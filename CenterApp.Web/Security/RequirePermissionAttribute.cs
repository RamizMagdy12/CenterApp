// Security/RequirePermissionAttribute.cs
using CenterApp.Entity.Security;
using Microsoft.AspNetCore.Mvc;

namespace CenterApp.Web.Security;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public class RequirePermissionAttribute : TypeFilterAttribute
{
    public RequirePermissionAttribute(string screen, PermissionAction action) : base(typeof(PermissionFilter))
    {
        Arguments = new object[] { screen, action };
    }
}