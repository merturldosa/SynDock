namespace Shop.Application.Common.Interfaces;

public interface IFileStorageService
{
    Task<string> UploadAsync(Stream stream, string fileName, string folder, CancellationToken cancellationToken = default);
    Task DeleteAsync(string filePath, CancellationToken cancellationToken = default);
}
