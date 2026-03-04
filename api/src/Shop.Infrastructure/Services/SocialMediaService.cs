using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Services;

public class SocialMediaService : ISocialMediaService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<SocialMediaService> _logger;
    private readonly string _instagramAccessToken;
    private readonly string _instagramAccountId;
    private readonly string _facebookPageId;
    private readonly string _facebookAccessToken;

    public SocialMediaService(HttpClient httpClient, IConfiguration configuration, ILogger<SocialMediaService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var section = configuration.GetSection("SocialMedia");
        _instagramAccessToken = section["Instagram:AccessToken"] ?? "";
        _instagramAccountId = section["Instagram:AccountId"] ?? "";
        _facebookPageId = section["Facebook:PageId"] ?? "";
        _facebookAccessToken = section["Facebook:AccessToken"] ?? "";
    }

    public Task<bool> IsConfiguredAsync(string platform, CancellationToken ct = default)
    {
        var configured = platform switch
        {
            "Instagram" => !string.IsNullOrEmpty(_instagramAccessToken) && !string.IsNullOrEmpty(_instagramAccountId),
            "Facebook" => !string.IsNullOrEmpty(_facebookPageId) && !string.IsNullOrEmpty(_facebookAccessToken),
            _ => false
        };
        return Task.FromResult(configured);
    }

    public async Task<SocialPostResult> PostToInstagramAsync(string caption, string imageUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_instagramAccessToken))
        {
            _logger.LogInformation("Instagram 미설정: 포스팅 스킵");
            return new SocialPostResult(false, null, null, "Instagram not configured");
        }

        try
        {
            // Step 1: Create media container
            var createUrl = $"https://graph.facebook.com/v19.0/{_instagramAccountId}/media";
            var createPayload = new Dictionary<string, string>
            {
                ["image_url"] = imageUrl,
                ["caption"] = caption,
                ["access_token"] = _instagramAccessToken
            };

            var createContent = new FormUrlEncodedContent(createPayload);
            var createResponse = await _httpClient.PostAsync(createUrl, createContent, ct);
            var createBody = await createResponse.Content.ReadAsStringAsync(ct);

            if (!createResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Instagram 미디어 생성 실패: {Body}", createBody);
                return new SocialPostResult(false, null, null, createBody);
            }

            var createResult = JsonSerializer.Deserialize<JsonElement>(createBody);
            var containerId = createResult.GetProperty("id").GetString();

            // Step 2: Publish media
            var publishUrl = $"https://graph.facebook.com/v19.0/{_instagramAccountId}/media_publish";
            var publishPayload = new Dictionary<string, string>
            {
                ["creation_id"] = containerId!,
                ["access_token"] = _instagramAccessToken
            };

            var publishContent = new FormUrlEncodedContent(publishPayload);
            var publishResponse = await _httpClient.PostAsync(publishUrl, publishContent, ct);
            var publishBody = await publishResponse.Content.ReadAsStringAsync(ct);

            if (!publishResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Instagram 발행 실패: {Body}", publishBody);
                return new SocialPostResult(false, null, null, publishBody);
            }

            var publishResult = JsonSerializer.Deserialize<JsonElement>(publishBody);
            var postId = publishResult.GetProperty("id").GetString();
            var postUrl = $"https://www.instagram.com/p/{postId}";

            _logger.LogInformation("Instagram 포스팅 성공: PostId={PostId}", postId);
            return new SocialPostResult(true, postId, postUrl, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Instagram 포스팅 예외");
            return new SocialPostResult(false, null, null, ex.Message);
        }
    }

    public async Task<SocialPostResult> PostToFacebookAsync(string caption, string? imageUrl, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_facebookAccessToken))
        {
            _logger.LogInformation("Facebook 미설정: 포스팅 스킵");
            return new SocialPostResult(false, null, null, "Facebook not configured");
        }

        try
        {
            string url;
            HttpContent content;

            if (!string.IsNullOrEmpty(imageUrl))
            {
                url = $"https://graph.facebook.com/v19.0/{_facebookPageId}/photos";
                var payload = new Dictionary<string, string>
                {
                    ["url"] = imageUrl,
                    ["message"] = caption,
                    ["access_token"] = _facebookAccessToken
                };
                content = new FormUrlEncodedContent(payload);
            }
            else
            {
                url = $"https://graph.facebook.com/v19.0/{_facebookPageId}/feed";
                var payload = new Dictionary<string, string>
                {
                    ["message"] = caption,
                    ["access_token"] = _facebookAccessToken
                };
                content = new FormUrlEncodedContent(payload);
            }

            var response = await _httpClient.PostAsync(url, content, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Facebook 포스팅 실패: {Body}", body);
                return new SocialPostResult(false, null, null, body);
            }

            var result = JsonSerializer.Deserialize<JsonElement>(body);
            var postId = result.GetProperty("id").GetString();
            var postUrl = $"https://www.facebook.com/{postId}";

            _logger.LogInformation("Facebook 포스팅 성공: PostId={PostId}", postId);
            return new SocialPostResult(true, postId, postUrl, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Facebook 포스팅 예외");
            return new SocialPostResult(false, null, null, ex.Message);
        }
    }
}
