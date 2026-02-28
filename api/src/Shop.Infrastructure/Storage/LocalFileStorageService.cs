using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Storage;

public class LocalFileStorageService : IFileStorageService
{
    private readonly string _basePath;
    private readonly string _baseUrl;

    public LocalFileStorageService(string basePath, string baseUrl)
    {
        _basePath = basePath;
        _baseUrl = baseUrl.TrimEnd('/');
    }

    public async Task<string> UploadAsync(Stream stream, string fileName, string folder, CancellationToken cancellationToken = default)
    {
        var ext = Path.GetExtension(fileName).ToLowerInvariant();
        var safeName = $"{Guid.NewGuid():N}{ext}";
        var folderPath = Path.Combine(_basePath, folder);

        Directory.CreateDirectory(folderPath);

        var filePath = Path.Combine(folderPath, safeName);
        using (var fs = new FileStream(filePath, FileMode.Create))
        {
            await stream.CopyToAsync(fs, cancellationToken);
        }

        return $"{_baseUrl}/uploads/{folder}/{safeName}";
    }

    public Task DeleteAsync(string filePath, CancellationToken cancellationToken = default)
    {
        // Extract relative path from URL
        var relativePath = filePath.Replace($"{_baseUrl}/uploads/", "");
        var fullPath = Path.Combine(_basePath, relativePath);

        if (File.Exists(fullPath))
            File.Delete(fullPath);

        return Task.CompletedTask;
    }
}
