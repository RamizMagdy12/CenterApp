using System.ComponentModel.DataAnnotations;

namespace CenterApp.Web.Models;

public class LoginVm
{
    [Required] public string UserName { get; set; } = "";
    [Required] public string Password { get; set; } = "";
    public string? ReturnUrl { get; set; }
    public string? Error { get; set; }
}