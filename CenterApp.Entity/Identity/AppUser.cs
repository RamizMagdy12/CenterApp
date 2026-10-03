// Identity/AppUser.cs
using Microsoft.AspNetCore.Identity;
namespace CenterApp.Entity.Identity;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = "";
}