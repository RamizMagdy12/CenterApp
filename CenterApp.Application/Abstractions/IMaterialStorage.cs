using CenterApp.Domain.Center;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CenterApp.Application.Abstractions
{
    public interface IMaterialStorage
    {
        Task<(string? Error, MaterialFile? File)> SaveAsync(long materialId, UploadedFile? file);
        string FullPath(MaterialFile f);
        byte[] ReadPage(MaterialFile f, int page, string watermark);
        void DeleteFile(MaterialFile f);
        void DeleteMaterial(long materialId);
    }
}
