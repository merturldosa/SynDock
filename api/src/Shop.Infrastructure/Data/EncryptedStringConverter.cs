using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Data;

public class EncryptedStringConverter : ValueConverter<string?, string?>
{
    public EncryptedStringConverter(IEncryptionService encryptionService)
        : base(
            v => encryptionService.Encrypt(v),
            v => encryptionService.Decrypt(v))
    {
    }
}
