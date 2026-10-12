using CenterApp.Entity.Center;
using CenterApp.Service.IRepository;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace CenterApp.Service.Repository;

public class HomeworkStorage : IHomeworkStorage
{
    private const long MaxDoc = 25L * 1024 * 1024;
    private readonly string _root;

    public HomeworkStorage(IWebHostEnvironment env)
        => _root = Path.Combine(env.ContentRootPath, "App_Data", "homework");

    private string Folder(long homeworkId) => Path.Combine(_root, homeworkId.ToString());
    public string FullPath(HomeworkFile f) => Path.Combine(Folder(f.HomeworkId), f.StoredName);

    public async Task<(string? Error, HomeworkFile? File)> SaveAsync(long homeworkId, IFormFile file)
    {
        if (file == null || file.Length == 0) return ("مفيش ملف", null);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        HomeworkFileKind kind;
        string contentType;
        switch (ext)
        {
            case ".pdf":
                kind = HomeworkFileKind.Pdf;
                contentType = "application/pdf";
                break;
            case ".doc":
                kind = HomeworkFileKind.Word;
                contentType = "application/msword";
                break;
            case ".docx":
                kind = HomeworkFileKind.Word;
                contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                break;
            default:
                return ("نوع الملف غير مسموح (pdf / doc / docx)", null);
        }

        if (file.Length > MaxDoc) return ("الملف أكبر من 25 ميجا", null);

        var folder = Folder(homeworkId);
        Directory.CreateDirectory(folder);
        var id = Guid.NewGuid().ToString("N");
        var raw = Path.Combine(folder, id + ext);

        await using (var fs = new FileStream(raw, FileMode.Create)) await file.CopyToAsync(fs);

        if (!Sniff(raw, ext))
        {
            Cleanup(folder, id);
            return ("محتوى الملف مش مطابق لنوعه", null);
        }

        return (null, new HomeworkFile
        {
            HomeworkId = homeworkId,
            FileName = Path.GetFileName(file.FileName),
            StoredName = id + ext,
            Kind = kind,
            ContentType = contentType,
            SizeBytes = file.Length
        });
    }

    private static bool Sniff(string path, string ext)
    {
        var b = new byte[8];
        int n;
        using (var s = File.OpenRead(path)) n = s.Read(b, 0, 8);
        if (n < 4) return false;
        return ext switch
        {
            ".pdf" => b[0] == 0x25 && b[1] == 0x50 && b[2] == 0x44 && b[3] == 0x46,
            ".docx" => b[0] == 0x50 && b[1] == 0x4B,
            ".doc" => b[0] == 0xD0 && b[1] == 0xCF,
            _ => false
        };
    }

    public void DeleteFile(HomeworkFile f) => Cleanup(Folder(f.HomeworkId), Path.GetFileNameWithoutExtension(f.StoredName));

    public void DeleteHomework(long homeworkId)
    {
        var f = Folder(homeworkId);
        if (Directory.Exists(f)) try { Directory.Delete(f, true); } catch { }
    }

    private static void Cleanup(string folder, string id)
    {
        if (!Directory.Exists(folder)) return;
        foreach (var p in Directory.GetFiles(folder, id + "*")) try { File.Delete(p); } catch { }
    }
}
