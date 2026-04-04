namespace Shop.Application.Common.Interfaces;

public record SocialPostResult(bool Success, string? PostId, string? PostUrl, string? ErrorMessage);

public interface ISocialMediaService
{
    Task<SocialPostResult> PostToInstagramAsync(string caption, string imageUrl, CancellationToken ct = default);
    Task<SocialPostResult> PostToFacebookAsync(string caption, string? imageUrl, CancellationToken ct = default);
    Task<SocialPostResult> PostToYoutubeAsync(string title, string description, string? thumbnailUrl, CancellationToken ct = default);
    Task<SocialPostResult> PostToTwitterAsync(string text, string? imageUrl, CancellationToken ct = default);
    Task<bool> IsConfiguredAsync(string platform, CancellationToken ct = default);
}
