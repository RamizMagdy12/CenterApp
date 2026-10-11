using CenterApp.Entity.Center;
using CenterApp.Service.IRepository;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using PDFtoImage;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CenterApp.Service.Repository
{
    public class MaterialStorage : IMaterialStorage
    {
        private const long MaxVideo = 200L * 1024 * 1024, MaxDoc = 25L * 1024 * 1024;
        private const int MaxPages = 200;
        private readonly string _root;
        private readonly string? _office;

        public MaterialStorage(IWebHostEnvironment env, IConfiguration cfg)
        {
            _root = Path.Combine(env.ContentRootPath, "App_Data", "materials");   // برّه wwwroot: مفيش رابط مباشر
            _office = cfg["Material:LibreOfficePath"];
        }

        private string Folder(long materialId) => Path.Combine(_root, materialId.ToString());
        public string FullPath(MaterialFile f) => Path.Combine(Folder(f.MaterialId), f.StoredName);
        private string PagePath(MaterialFile f, int n)
            => Path.Combine(Folder(f.MaterialId), $"{Path.GetFileNameWithoutExtension(f.StoredName)}_p{n}.jpg");

        public async Task<(string? Error, MaterialFile? File)> SaveAsync(long materialId, IFormFile file)
        {
            if (file == null || file.Length == 0) return ("مفيش ملف", null);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            MaterialFileKind kind; string contentType;
            switch (ext)
            {
                case ".mp4": kind = MaterialFileKind.Video; contentType = "video/mp4"; break;
                case ".webm": kind = MaterialFileKind.Video; contentType = "video/webm"; break;
                case ".pdf": case ".doc": case ".docx": kind = MaterialFileKind.Pdf; contentType = "application/pdf"; break;
                case ".jpg": case ".jpeg": kind = MaterialFileKind.Image; contentType = "image/jpeg"; break;
                case ".png": kind = MaterialFileKind.Image; contentType = "image/png"; break;
                default: return ("نوع الملف غير مسموح (mp4 / webm / pdf / doc / docx / jpg / png)", null);
            }

            if (file.Length > (kind == MaterialFileKind.Video ? MaxVideo : MaxDoc))
                return (kind == MaterialFileKind.Video ? "الفيديو أكبر من 200 ميجا" : "الملف أكبر من 25 ميجا", null);

            var folder = Folder(materialId);
            Directory.CreateDirectory(folder);
            var id = Guid.NewGuid().ToString("N");
            var raw = Path.Combine(folder, id + ext);

            await using (var fs = new FileStream(raw, FileMode.Create)) await file.CopyToAsync(fs);

            (string?, MaterialFile?) Fail(string m) { Cleanup(folder, id); return (m, null); }

            try
            {
                if (!Sniff(raw, ext)) return Fail("محتوى الملف مش مطابق لنوعه");

                var stored = id + ext;
                var pages = 1;

                if (ext is ".doc" or ".docx")
                {
                    if (!await ConvertToPdfAsync(raw, folder)) return Fail("تعذر تحويل ملف Word إلى PDF، ارفعه PDF");
                    File.Delete(raw);
                    raw = Path.Combine(folder, id + ".pdf");
                    stored = id + ".pdf";
                }

                if (kind == MaterialFileKind.Pdf)
                {
                    var (err, count) = RenderPdf(raw, folder, id);
                    if (err != null) return Fail(err);
                    pages = count;
                }

                return (null, new MaterialFile
                {
                    MaterialId = materialId,
                    FileName = Path.GetFileName(file.FileName),
                    StoredName = stored,
                    Kind = kind,
                    ContentType = contentType,
                    SizeBytes = file.Length,
                    PageCount = pages
                });
            }
            catch { return Fail("حصل خطأ أثناء معالجة الملف"); }
        }

        private static bool Sniff(string path, string ext)
        {
            var b = new byte[12];
            int n;
            using (var s = File.OpenRead(path)) n = s.Read(b, 0, 12);
            if (n < 4) return false;
            return ext switch
            {
                ".pdf" => b[0] == 0x25 && b[1] == 0x50 && b[2] == 0x44 && b[3] == 0x46,
                ".jpg" or ".jpeg" => b[0] == 0xFF && b[1] == 0xD8 && b[2] == 0xFF,
                ".png" => b[0] == 0x89 && b[1] == 0x50 && b[2] == 0x4E && b[3] == 0x47,
                ".mp4" => n >= 8 && b[4] == 0x66 && b[5] == 0x74 && b[6] == 0x79 && b[7] == 0x70,
                ".webm" => b[0] == 0x1A && b[1] == 0x45 && b[2] == 0xDF && b[3] == 0xA3,
                ".docx" => b[0] == 0x50 && b[1] == 0x4B,
                ".doc" => b[0] == 0xD0 && b[1] == 0xCF,
                _ => false
            };
        }

        // Word → PDF بـ LibreOffice (لازم يكون متسطّب على السيرفر ومسجّل مساره في الإعدادات)
        private async Task<bool> ConvertToPdfAsync(string input, string outDir)
        {
            if (string.IsNullOrWhiteSpace(_office) || !File.Exists(_office)) return false;

            var psi = new ProcessStartInfo(_office) { UseShellExecute = false, CreateNoWindow = true };
            foreach (var a in new[] { "--headless", "--convert-to", "pdf", "--outdir", outDir, input }) psi.ArgumentList.Add(a);

            using var p = Process.Start(psi);
            if (p == null) return false;
            var done = await Task.Run(() => p.WaitForExit(90_000));
            if (!done) { try { p.Kill(true); } catch { } return false; }
            return p.ExitCode == 0 && File.Exists(Path.Combine(outDir, Path.GetFileNameWithoutExtension(input) + ".pdf"));
        }

        // كل صفحة PDF بتتحول لصورة JPEG على السيرفر
        private static (string? Error, int Pages) RenderPdf(string pdfPath, string folder, string id)
        {
            int count;
            try { using var s = File.OpenRead(pdfPath); count = Conversion.GetPageCount(s); }
            catch { return ("ملف PDF تالف أو عليه باسورد", 0); }

            if (count < 1) return ("ملف PDF فاضي", 0);
            if (count > MaxPages) return ($"ملف PDF أكتر من {MaxPages} صفحة، قسّمه", 0);

            for (var i = 0; i < count; i++)
            {
                using var s = File.OpenRead(pdfPath);
                using var bmp = Conversion.ToImage(s, page: i, options: new RenderOptions(Dpi: 110));
                using var flat = new SKBitmap(bmp.Width, bmp.Height);
                using (var c = new SKCanvas(flat)) { c.Clear(SKColors.White); c.DrawBitmap(bmp, 0, 0); }
                using var img = SKImage.FromBitmap(flat);
                using var data = img.Encode(SKEncodedImageFormat.Jpeg, 80);
                File.WriteAllBytes(Path.Combine(folder, $"{id}_p{i + 1}.jpg"), data.ToArray());
            }
            return (null, count);
        }

        public byte[] ReadPage(MaterialFile f, int page, string watermark)
        {
            var path = f.Kind == MaterialFileKind.Pdf ? PagePath(f, page) : FullPath(f);
            if (page < 1 || page > Math.Max(1, f.PageCount) || !File.Exists(path)) throw new FileNotFoundException();
            return Watermark(File.ReadAllBytes(path), watermark);
        }

        // علامة مائية متكررة ومائلة محروقة في الصورة (كود الطالب + التاريخ)
        private static byte[] Watermark(byte[] bytes, string text)
        {
            using var bmp = SKBitmap.Decode(bytes);
            using (var canvas = new SKCanvas(bmp))
            using (var paint = new SKPaint { Color = new SKColor(80, 80, 80, 55), IsAntialias = true })
            using (var font = new SKFont { Size = Math.Max(16f, bmp.Width / 30f) })
            {
                var w = font.MeasureText(text, paint);
                var step = (int)(font.Size * 3.5f);
                canvas.RotateDegrees(-28, bmp.Width / 2f, bmp.Height / 2f);
                for (var y = -bmp.Height; y < bmp.Height * 2; y += step)
                    for (var x = -bmp.Width; x < bmp.Width * 2; x += (int)w + step)
                        canvas.DrawText(text, x, y, font, paint);
            }
            using var img = SKImage.FromBitmap(bmp);
            using var data = img.Encode(SKEncodedImageFormat.Jpeg, 82);
            return data.ToArray();
        }

        public void DeleteFile(MaterialFile f) => Cleanup(Folder(f.MaterialId), Path.GetFileNameWithoutExtension(f.StoredName));

        public void DeleteMaterial(long materialId)
        {
            var f = Folder(materialId);
            if (Directory.Exists(f)) try { Directory.Delete(f, true); } catch { }
        }

        private static void Cleanup(string folder, string id)
        {
            if (!Directory.Exists(folder)) return;
            foreach (var p in Directory.GetFiles(folder, id + "*")) try { File.Delete(p); } catch { }
        }
    }
}
