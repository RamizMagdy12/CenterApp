using System.Security.Claims;
using CenterApp.Entity.Security;
using CenterApp.Service.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace CenterApp.Service.Services;

public interface IPermissionService
{
    bool Has(ClaimsPrincipal user, string screen, PermissionAction action);
    void InvalidateAll();
}

public class PermissionService : IPermissionService
{
    private static int _version;
    private readonly AppDbContext _db;
    private readonly IMemoryCache _cache;

    public PermissionService(AppDbContext db, IMemoryCache cache) { _db = db; _cache = cache; }

    public bool Has(ClaimsPrincipal user, string screen, PermissionAction action)
    {
        if (user?.Identity?.IsAuthenticated != true) return false;
        if (user.IsInRole(RoleNames.Admin)) return true;

        foreach (var role in user.FindAll(ClaimTypes.Role).Select(c => c.Value))
        {
            var map = _cache.GetOrCreate($"perm:{_version}:{role}", e =>
            {
                e.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10);
                return Load(role);
            })!;

            if (map.TryGetValue(screen, out var flags) && flags[(int)action]) return true;
        }
        return false;
    }

    private Dictionary<string, bool[]> Load(string roleName)
    {
        var rows = (from p in _db.RolePermissions.AsNoTracking()
                    join r in _db.Roles.AsNoTracking() on p.RoleId equals r.Id
                    where r.Name == roleName
                    select p).ToList();

        return rows.ToDictionary(p => p.ScreenCode, p => new[] { p.CanView, p.CanAdd, p.CanEdit, p.CanDelete });
    }

    public void InvalidateAll() => Interlocked.Increment(ref _version);
}