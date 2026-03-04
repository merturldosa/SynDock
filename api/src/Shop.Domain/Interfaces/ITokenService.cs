using Shop.Domain.Entities;

namespace Shop.Domain.Interfaces;

public interface ITokenService
{
    string GenerateAccessToken(User user, Tenant tenant);
    string GenerateRefreshToken();
    int? ValidateAccessToken(string token);
    string GenerateTwoFactorToken(User user, Tenant tenant);
    int? ValidateTwoFactorToken(string token);
}
