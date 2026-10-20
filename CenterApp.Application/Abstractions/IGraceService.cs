using CenterApp.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CenterApp.Application.Abstractions
{
    public interface IGraceService
    {
        Task<List<GraceRow>> ListAsync();
        Task<GraceFormVm> FormAsync(long? studentId, long? groupId);
        Task<OpResult> GrantAsync(long studentId, long? groupId, int days, string? reason, string? reasonEn = null);
        Task<OpResult> RevokeAsync(long id);
    }
}
