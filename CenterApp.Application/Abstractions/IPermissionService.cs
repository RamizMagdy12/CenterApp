using System.Security.Claims;
using CenterApp.Domain.Security;

namespace CenterApp.Application.Abstractions;

public interface IPermissionService
{
    bool Has(ClaimsPrincipal user, string screen, PermissionAction action);
    void InvalidateAll();
}
