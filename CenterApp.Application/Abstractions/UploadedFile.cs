namespace CenterApp.Application.Abstractions;

public sealed class UploadedFile : IDisposable
{
    private readonly Stream _content;

    public UploadedFile(string fileName, long length, Stream content)
    {
        FileName = fileName;
        Length = length;
        _content = content;
    }

    public string FileName { get; }
    public long Length { get; }

    public Task CopyToAsync(Stream target) => _content.CopyToAsync(target);

    public void Dispose() => _content.Dispose();
}
