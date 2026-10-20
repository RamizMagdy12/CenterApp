// Identity/AppUser.cs
using Microsoft.AspNetCore.Identity;
namespace CenterApp.Domain.Identity;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = "";
    public bool IsActive { get; set; } = true;
}