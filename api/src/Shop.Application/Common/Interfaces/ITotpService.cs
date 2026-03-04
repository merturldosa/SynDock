namespace Shop.Application.Common.Interfaces;

public interface ITotpService
{
    string GenerateSecret();
    string GenerateQrCodeUri(string secret, string email, string issuer);
    bool ValidateCode(string secret, string code);
    List<string> GenerateBackupCodes(int count = 8);
}
