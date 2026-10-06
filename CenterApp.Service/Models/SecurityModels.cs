namespace CenterApp.Service.Models;

public class UserRow
{
    public string Id { get; set; } = "";
    public string UserName { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string Role { get; set; } = "";
    public bool IsActive { get; set; }
    public bool IsAdmin { get; set; }
}

public class UserVm
{
    public string? Id { get; set; }
    public string UserName { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? Email { get; set; }
    public string? PhoneNumber { get; set; }
    public string? Password { get; set; }
    public string? RoleName { get; set; }
    public bool IsActive { get; set; } = true;
}

public class RoleRow
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public int Users { get; set; }
    public bool IsAdmin { get; set; }
}

public class RolePermVm
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public bool View { get; set; }
    public bool Add { get; set; }
    public bool Edit { get; set; }
    public bool Delete { get; set; }
}

public class RoleVm
{
    public string? Id { get; set; }
    public string Name { get; set; } = "";
    public List<RolePermVm> Perms { get; set; } = new();
}