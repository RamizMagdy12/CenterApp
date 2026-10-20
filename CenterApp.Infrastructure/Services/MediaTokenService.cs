using CenterApp.Application.Abstractions;
using Microsoft.AspNetCore.DataProtection;

namespace CenterApp.Infrastructure.Services;
public class MediaTokenService : IMediaTokenService
{
    private readonly ITimeLimitedDataProtector _p;
    public MediaTokenService(IDataProtectionProvider provider)
        => _p = provider.CreateProtector("MaterialMedia.v1").ToTimeLimitedDataProtector();

    public string Create(string userId, long fileId) => _p.Protect($"{userId}|{fileId}", TimeSpan.FromHours(3));

    public bool Validate(string token, string userId, long fileId)
    {
        try { return _p.Unprotect(token) == $"{userId}|{fileId}"; }
        catch { return false; }
    }
}