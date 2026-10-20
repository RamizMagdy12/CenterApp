using CenterApp.Domain.Center;
using CenterApp.Domain.Security;
using CenterApp.Application.Abstractions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace CenterApp.Web.Controllers;

[Authorize]
public class MediaController : Controller
{
    private readonly IMediaTokenService _tokens;
    private readonly IPortalService _portal;
    private readonly IMaterialService _mat;
    private readonly IMaterialStorage _storage;
    private readonly IPermissionService _perm;
    private readonly IConfiguration _cfg;

    public MediaController(IMediaTokenService tokens, IPortalService portal, IMaterialService mat,
                           IMaterialStorage storage, IPermissionService perm, IConfiguration cfg)
    { _tokens = tokens; _portal = portal; _mat = mat; _storage = storage; _perm = perm; _cfg = cfg; }

    private async Task<(MaterialFile? File, string Wm)> CheckAsync(long id, string t, string dest)
    {
        // 1) لازم الطلب يكون جاي من عنصر <video> / <img> (مش فتح رابط مباشر)
        var got = Request.Headers["Sec-Fetch-Dest"].ToString();
        var strict = _cfg.GetValue("Material:StrictFetchHeaders", true);
        if (got.Length > 0 ? !got.Equals(dest, StringComparison.OrdinalIgnoreCase) : strict) return (null, "");

        // 2) توكن مؤقت مربوط بنفس المستخدم ونفس الملف
        var uid = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";
        if (string.IsNullOrEmpty(t) || !_tokens.Validate(t, uid, id)) return (null, "");

        var file = await _portal.FileAsync(id);
        if (file == null) return (null, "");

        // 3) صلاحية الوصول للماتريال
        string wm;
        if (User.IsInRole(RoleNames.Student))
        {
            var st = await _portal.StudentOfUserAsync(uid);
            if (st == null || !await _portal.CanViewAsync(st.Id, file.MaterialId)) return (null, "");   // الدفع / السماح
            wm = $"{st.Code}  {DateTime.Now:yyyy-MM-dd}";
        }
        else
        {
            if (!_perm.Has(User, Screens.Materials, PermissionAction.View)) return (null, "");
            long? scope = User.IsInRole(RoleNames.Admin) ? null : await _mat.TeacherIdOfUserAsync(uid);
            if (!await _mat.CanViewAsync(file.MaterialId, scope)) return (null, "");
            wm = $"{User.Identity?.Name}  {DateTime.Now:yyyy-MM-dd}";
        }
        return (file, wm);
    }

    private void NoStore()
    {
        Response.Headers["Cache-Control"] = "private, no-store";
        Response.Headers["X-Content-Type-Options"] = "nosniff";
    }

    [HttpGet]
    public async Task<IActionResult> Video(long id, string t)
    {
        var (f, _) = await CheckAsync(id, t, "video");
        if (f == null || f.Kind != MaterialFileKind.Video) return StatusCode(403);
        NoStore();
        return PhysicalFile(_storage.FullPath(f), f.ContentType, enableRangeProcessing: true);   // Seek شغال
    }

    [HttpGet]
    public async Task<IActionResult> Page(long id, int n, string t)
    {
        var (f, wm) = await CheckAsync(id, t, "image");
        if (f == null || f.Kind == MaterialFileKind.Video) return StatusCode(403);
        NoStore();
        try { return File(_storage.ReadPage(f, n, wm), "image/jpeg"); }
        catch (FileNotFoundException) { return NotFound(); }
    }
}