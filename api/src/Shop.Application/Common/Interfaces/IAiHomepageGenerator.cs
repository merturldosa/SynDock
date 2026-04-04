namespace Shop.Application.Common.Interfaces;

public interface IAiHomepageGenerator
{
    Task GenerateHomepageAsync(int tenantId, string companyName, string businessType, string? businessDescription, CancellationToken ct = default);
}

public record GeneratedSection(string Title, string Content, string? ImagePrompt);
