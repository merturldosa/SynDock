namespace Shop.Application.Common.DTOs;

public record AuthResponse(string AccessToken, string RefreshToken, UserDto User);

public record UserDto(int Id, string Username, string Email, string Name, string? Phone, string Role, string? CustomFieldsJson);

public record UserProfileDto(int Id, string Username, string Email, string Name, string? Phone, string Role, string? CustomFieldsJson, DateTime? LastLoginAt, bool EmailVerified);

// 2FA DTOs
public record LoginResponse(
    bool RequiresTwoFactor,
    string? TwoFactorToken,
    AuthResponse? Auth,
    bool MustChangePassword = false);

public record TwoFactorSetupResponse(
    string QrCodeUri,
    string Secret,
    List<string> BackupCodes);

public record TwoFactorVerifySetupResponse(bool Enabled);
