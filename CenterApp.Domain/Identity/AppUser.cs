// Identity/AppUser.cs
using CenterApp.Domain.Center;
using Microsoft.AspNetCore.Identity;
namespace CenterApp.Domain.Identity;

public class AppUser : IdentityUser
{
    public string FullName { get; set; } = "";
    public string? FullNameEn { get; set; }
    public string LocalFullName => Bilingual.Show(FullName, FullNameEn);
    public bool IsActive { get; set; } = true;
}