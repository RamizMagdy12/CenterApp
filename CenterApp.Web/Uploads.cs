using CenterApp.Application.Abstractions;

namespace CenterApp.Web;

public static class Uploads
{
    public static UploadedFile? Read(IFormFile? file)
    {
        if (file == null) return null;
        return new UploadedFile(file.FileName, file.Length, file.OpenReadStream());
    }
}
