using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Services;

public class OAuthProviderService : IOAuthProviderService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;

    public OAuthProviderService(HttpClient httpClient, IConfiguration configuration)
    {
        _httpClient = httpClient;
        _configuration = configuration;
    }

    public async Task<OAuthProfile> ExchangeCodeForProfile(string provider, string code, string redirectUri, CancellationToken ct = default)
    {
        return provider.ToLowerInvariant() switch
        {
            "kakao" => await ExchangeKakao(code, redirectUri, ct),
            "google" => await ExchangeGoogle(code, redirectUri, ct),
            _ => throw new ArgumentException($"Unsupported OAuth provider: {provider}")
        };
    }

    private async Task<OAuthProfile> ExchangeKakao(string code, string redirectUri, CancellationToken ct)
    {
        var clientId = _configuration["OAuth:Kakao:ClientId"]!;
        var clientSecret = _configuration["OAuth:Kakao:ClientSecret"];

        // Exchange code for access token
        var tokenParams = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = clientId,
            ["redirect_uri"] = redirectUri,
            ["code"] = code
        };
        if (!string.IsNullOrEmpty(clientSecret))
            tokenParams["client_secret"] = clientSecret;

        var tokenResponse = await _httpClient.PostAsync(
            "https://kauth.kakao.com/oauth/token",
            new FormUrlEncodedContent(tokenParams), ct);
        tokenResponse.EnsureSuccessStatusCode();

        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
        var accessToken = tokenJson.GetProperty("access_token").GetString()!;

        // Get user profile
        var profileRequest = new HttpRequestMessage(HttpMethod.Get, "https://kapi.kakao.com/v2/user/me");
        profileRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var profileResponse = await _httpClient.SendAsync(profileRequest, ct);
        profileResponse.EnsureSuccessStatusCode();

        var profileJson = await profileResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
        var kakaoId = profileJson.GetProperty("id").GetInt64().ToString();
        var account = profileJson.GetProperty("kakao_account");

        var email = account.TryGetProperty("email", out var emailProp) ? emailProp.GetString() : null;
        var profile = account.TryGetProperty("profile", out var profileProp) ? profileProp : default;
        var nickname = profile.ValueKind != JsonValueKind.Undefined && profile.TryGetProperty("nickname", out var nickProp)
            ? nickProp.GetString() : null;
        var profileImage = profile.ValueKind != JsonValueKind.Undefined && profile.TryGetProperty("profile_image_url", out var imgProp)
            ? imgProp.GetString() : null;

        if (string.IsNullOrEmpty(email))
            throw new InvalidOperationException("No email registered with Kakao account");

        return new OAuthProfile("kakao", kakaoId, email, nickname ?? "Kakao User", profileImage);
    }

    private async Task<OAuthProfile> ExchangeGoogle(string code, string redirectUri, CancellationToken ct)
    {
        var clientId = _configuration["OAuth:Google:ClientId"]!;
        var clientSecret = _configuration["OAuth:Google:ClientSecret"]!;

        // Exchange code for access token
        var tokenParams = new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = clientId,
            ["client_secret"] = clientSecret,
            ["redirect_uri"] = redirectUri,
            ["code"] = code
        };

        var tokenResponse = await _httpClient.PostAsync(
            "https://oauth2.googleapis.com/token",
            new FormUrlEncodedContent(tokenParams), ct);
        tokenResponse.EnsureSuccessStatusCode();

        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
        var accessToken = tokenJson.GetProperty("access_token").GetString()!;

        // Get user profile
        var profileRequest = new HttpRequestMessage(HttpMethod.Get, "https://www.googleapis.com/oauth2/v2/userinfo");
        profileRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        var profileResponse = await _httpClient.SendAsync(profileRequest, ct);
        profileResponse.EnsureSuccessStatusCode();

        var profileJson = await profileResponse.Content.ReadFromJsonAsync<JsonElement>(ct);
        var googleId = profileJson.GetProperty("id").GetString()!;
        var email = profileJson.GetProperty("email").GetString()!;
        var name = profileJson.TryGetProperty("name", out var nameProp) ? nameProp.GetString() : null;
        var picture = profileJson.TryGetProperty("picture", out var picProp) ? picProp.GetString() : null;

        return new OAuthProfile("google", googleId, email, name ?? "Google 사용자", picture);
    }
}
