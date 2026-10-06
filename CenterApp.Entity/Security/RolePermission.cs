// Security/RolePermission.cs
namespace CenterApp.Entity.Security;

public class RolePermission
{
    public long Id { get; set; }
    public string RoleId { get; set; } = null!;
    public string ScreenCode { get; set; } = null!;
    public bool CanView { get; set; }
    public bool CanAdd { get; set; }
    public bool CanEdit { get; set; }
    public bool CanDelete { get; set; }
}