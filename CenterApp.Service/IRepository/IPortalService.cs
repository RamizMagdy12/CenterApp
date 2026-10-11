using CenterApp.Entity.Center;
using CenterApp.Service.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CenterApp.Service.IRepository;



internal sealed record Enr(long Id, long GroupId, DateTime JoinedAt, DateTime? LeftAt);
internal sealed record Inv(long EnrollmentId, int Year, int Month, InvoiceStatus Status);
internal sealed record Gr(long? GroupId, DateTime ExpiresAt);
internal sealed record Ctx(List<Enr> E, List<Inv> I, List<Gr> G);
internal sealed record Verdict(bool Visible, bool Locked, string? Reason, DateTime? GraceUntil, string Groups);
public interface IPortalService
    {
        Task<StudentInfo?> StudentOfUserAsync(string userId);
        Task<List<PortalMaterialRow>> ListAsync(long studentId);
        Task<(PortalMaterialRow? Row, Material? Material)> GetAsync(long studentId, long materialId);
        Task<bool> CanViewAsync(long studentId, long materialId);
        Task<MaterialFile?> FileAsync(long fileId);
    }

