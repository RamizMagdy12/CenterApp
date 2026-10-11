using CenterApp.Service.Models;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CenterApp.Service.IRepository
{
    public interface IMaterialService
    {
        Task<long?> TeacherIdOfUserAsync(string userId);
        Task<List<MaterialRow>> ListAsync(long? teacherId);
        Task<MaterialEditVm?> GetAsync(long id, long? teacherId);
        Task<OpResult> SaveAsync(MaterialSaveDto dto, long? teacherId);
        Task<OpResult> UploadAsync(long materialId, IFormFile file, long? teacherId);
        Task<OpResult> DeleteFileAsync(long fileId, long? teacherId);
        Task<OpResult> PublishAsync(long id, long? teacherId);
        Task<OpResult> DeleteAsync(long id, long? teacherId);
        Task<bool> CanViewAsync(long materialId, long? teacherId);
    }
}
