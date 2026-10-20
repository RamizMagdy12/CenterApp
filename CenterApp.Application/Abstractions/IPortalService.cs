using CenterApp.Domain.Center;
using CenterApp.Application.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CenterApp.Application.Abstractions;

public interface IPortalService
    {
        Task<StudentInfo?> StudentOfUserAsync(string userId);
        Task<List<PortalMaterialRow>> ListAsync(long studentId);
        Task<(PortalMaterialRow? Row, Material? Material)> GetAsync(long studentId, long materialId);
        Task<bool> CanViewAsync(long studentId, long materialId);
        Task<MaterialFile?> FileAsync(long fileId);
        Task<List<PortalHomeworkRow>> ListHomeworkAsync(long studentId);
        Task<PortalHomeworkVm?> GetHomeworkAsync(long studentId, long homeworkId);
        Task<bool> CanDownloadHomeworkAsync(long studentId, long homeworkId);
        Task<HomeworkFile?> HomeworkFileAsync(long fileId);
    }

