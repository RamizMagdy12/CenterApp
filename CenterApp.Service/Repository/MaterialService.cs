using CenterApp.Entity.Center;
using CenterApp.Service.IRepository;
using CenterApp.Service.Models;
using CenterApp.Service.Unitofwork;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CenterApp.Service.Repository
{
    public class MaterialService : IMaterialService
    {
        private readonly IUnitOfWork _u;
        private readonly IMaterialStorage _storage;
        public MaterialService(IUnitOfWork u, IMaterialStorage storage) { _u = u; _storage = storage; }

        private static string Size(long b) => b >= 1048576 ? $"{b / 1048576.0:0.#} MB" : $"{Math.Max(1, b / 1024)} KB";

        public async Task<long?> TeacherIdOfUserAsync(string userId)
            => await _u.Teacher.GetAll(t => t.UserId == userId).Select(t => (long?)t.Id).FirstOrDefaultAsync();

        public async Task<List<MaterialRow>> ListAsync(long? teacherId)
        {
            var list = await _u.Material
                .GetAll(m => teacherId == null || m.TeacherId == teacherId, "Groups.Group,Files,Teacher")
                .OrderByDescending(m => m.Id).ToListAsync();

            return list.Select(m => new MaterialRow
            {
                Id = m.Id,
                Title = m.Title,
                Teacher = m.Teacher?.Name ?? "",
                Status = m.Status,
                PublishedAt = m.PublishedAt,
                Groups = string.Join("، ", m.Groups.Select(g => g.Group.Name)),
                FilesCount = m.Files.Count
            }).ToList();
        }

        public async Task<MaterialEditVm?> GetAsync(long id, long? teacherId)
        {
            Material? m = null;
            if (id != 0)
            {
                m = await _u.Material.GetAll(x => x.Id == id && (teacherId == null || x.TeacherId == teacherId), "Groups,Files").FirstOrDefaultAsync();
                if (m == null) return null;
            }

            var selected = m?.Groups.Select(g => g.GroupId).ToHashSet() ?? new HashSet<long>();
            var groups = await _u.Group.GetAll(g => g.IsActive && (teacherId == null || g.TeacherId == teacherId))
                .OrderBy(g => g.Name).Select(g => new { g.Id, g.Name }).ToListAsync();

            return new MaterialEditVm
            {
                Id = m?.Id ?? 0,
                Title = m?.Title ?? "",
                Description = m?.Description,
                Status = m?.Status ?? MaterialStatus.Draft,
                Groups = groups.Select(g => new GroupOption { Id = g.Id, Name = g.Name, Selected = selected.Contains(g.Id) }).ToList(),
                Files = m?.Files.OrderBy(f => f.Id).Select(f => new MaterialFileVm
                {
                    Id = f.Id,
                    Name = f.FileName,
                    Kind = f.Kind,
                    PageCount = f.PageCount,
                    Size = Size(f.SizeBytes)
                }).ToList() ?? new()
            };
        }

        public async Task<OpResult> SaveAsync(MaterialSaveDto dto, long? teacherId)
        {
            if (string.IsNullOrWhiteSpace(dto.Title)) return OpResult.Fail("عنوان الماتريال مطلوب");
            var ids = (dto.GroupIds ?? new()).Distinct().ToList();
            if (ids.Count == 0) return OpResult.Fail("اختار مجموعة واحدة على الأقل");

            var allowed = await _u.Group.GetAll(g => ids.Contains(g.Id) && (teacherId == null || g.TeacherId == teacherId))
                .Select(g => g.Id).ToListAsync();
            if (allowed.Count != ids.Count) return OpResult.Fail("فيه مجموعة مش تابعة لك");

            Material? m;
            if (dto.Id == 0)
            {
                m = new Material { TeacherId = teacherId };
                _u.Material.Add(m);
            }
            else
            {
                m = await _u.Material.GetAll(x => x.Id == dto.Id && (teacherId == null || x.TeacherId == teacherId), "Groups", tracking: true)
                    .FirstOrDefaultAsync();
                if (m == null) return OpResult.Fail("الماتريال غير موجود");
            }

            m.Title = dto.Title.Trim();
            m.Description = dto.Description?.Trim();

            foreach (var g in m.Groups.Where(g => !ids.Contains(g.GroupId)).ToList()) _u.MaterialGroup.Remove(g);
            foreach (var gid in ids.Where(i => !m.Groups.Any(g => g.GroupId == i))) m.Groups.Add(new MaterialGroup { GroupId = gid });

            await _u.SaveAsync();
            return new OpResult(true, "تم الحفظ", m.Id);
        }

        public async Task<OpResult> UploadAsync(long materialId, IFormFile file, long? teacherId)
        {
            var ok = await CanViewAsync(materialId, teacherId);
            if (!ok) return OpResult.Fail("الماتريال غير موجود");

            var (err, f) = await _storage.SaveAsync(materialId, file);
            if (err != null || f == null) return OpResult.Fail(err ?? "فشل الرفع");

            _u.MaterialFile.Add(f);
            await _u.SaveAsync();
            return new OpResult(true, "تم رفع الملف", f.Id, f.FileName);
        }

        public async Task<OpResult> DeleteFileAsync(long fileId, long? teacherId)
        {
            var f = await _u.MaterialFile
                .GetAll(x => x.Id == fileId && (teacherId == null || x.Material.TeacherId == teacherId), "Material", tracking: true)
                .FirstOrDefaultAsync();
            if (f == null) return OpResult.Fail("الملف غير موجود");

            if (f.Material.Status == MaterialStatus.Published &&
                !await _u.MaterialFile.GetAll(x => x.MaterialId == f.MaterialId && x.Id != f.Id).AnyAsync())
                return OpResult.Fail("الماتريال المنشور لازم يفضل فيه ملف واحد على الأقل");

            _storage.DeleteFile(f);
            _u.MaterialFile.Remove(f);
            await _u.SaveAsync();
            return OpResult.Success("تم حذف الملف");
        }

        public async Task<OpResult> PublishAsync(long id, long? teacherId)
        {
            var m = await _u.Material.GetAll(x => x.Id == id && (teacherId == null || x.TeacherId == teacherId), "Groups,Files", tracking: true)
                .FirstOrDefaultAsync();
            if (m == null) return OpResult.Fail("الماتريال غير موجود");
            if (m.Files.Count == 0) return OpResult.Fail("ارفع ملف واحد على الأقل قبل النشر");
            if (m.Groups.Count == 0) return OpResult.Fail("اختار مجموعة واحدة على الأقل");

            if (m.Status != MaterialStatus.Published)
            {
                m.Status = MaterialStatus.Published;
                m.PublishedAt ??= DateTime.Now;      // شهر النشر هو اللي بيتحدد بيه القفل بالدفع
            }
            await _u.SaveAsync();
            return OpResult.Success("تم النشر");
        }

        public async Task<OpResult> DeleteAsync(long id, long? teacherId)
        {
            var m = await _u.Material.GetAll(x => x.Id == id && (teacherId == null || x.TeacherId == teacherId), "Groups,Files", tracking: true)
                .FirstOrDefaultAsync();
            if (m == null) return OpResult.Fail("الماتريال غير موجود");

            foreach (var g in m.Groups.ToList()) _u.MaterialGroup.Remove(g);
            foreach (var f in m.Files.ToList()) _u.MaterialFile.Remove(f);
            _u.Material.Remove(m);
            await _u.SaveAsync();

            _storage.DeleteMaterial(id);
            return OpResult.Success("تم الحذف");
        }

        public async Task<bool> CanViewAsync(long materialId, long? teacherId)
            => await _u.Material.GetAll(x => x.Id == materialId && (teacherId == null || x.TeacherId == teacherId)).AnyAsync();
    }
}
