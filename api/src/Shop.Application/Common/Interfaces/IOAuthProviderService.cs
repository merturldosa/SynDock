namespace Shop.Application.Common.Interfaces;

public interface IOAuthProviderService
{
    Task<OAuthProfile> ExchangeCodeForProfile(string provider, string code, string redirectUri, CancellationToken ct = default);
}

public record OAuthProfile(string Provider, string ProviderId, string Email, string Name, string? ProfileImageUrl);
