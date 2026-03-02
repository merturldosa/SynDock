namespace Shop.Application.Common.Interfaces;

public record GeneratedImage(string Url, string? RevisedPrompt);

public interface IImageGenerator
{
    Task<GeneratedImage> GenerateAsync(string prompt, string size = "1024x1024", CancellationToken ct = default);
}
