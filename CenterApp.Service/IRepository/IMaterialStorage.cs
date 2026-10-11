using CenterApp.Entity.Center;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CenterApp.Service.IRepository
{
    public interface IMaterialStorage
    {
        Task<(string? Error, MaterialFile? File)> SaveAsync(long materialId, IFormFile file);
        string FullPath(MaterialFile f);
        byte[] ReadPage(MaterialFile f, int page, string watermark);
        void DeleteFile(MaterialFile f);
        void DeleteMaterial(long materialId);
    }
}
