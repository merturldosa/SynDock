using System.Security.Cryptography;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Services;

public class TotpService : ITotpService
{
    private const int SecretLength = 20; // 160 bits
    private const int TimeStep = 30;     // 30 seconds
    private const int CodeDigits = 6;
    private const int TimeTolerance = 1; // allow +/- 1 time step

    private static readonly char[] Base32Chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567".ToCharArray();

    public string GenerateSecret()
    {
        var bytes = new byte[SecretLength];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Base32Encode(bytes);
    }

    public string GenerateQrCodeUri(string secret, string email, string issuer)
    {
        var encodedIssuer = Uri.EscapeDataString(issuer);
        var encodedEmail = Uri.EscapeDataString(email);
        return $"otpauth://totp/{encodedIssuer}:{encodedEmail}?secret={secret}&issuer={encodedIssuer}";
    }

    public bool ValidateCode(string secret, string code)
    {
        if (string.IsNullOrWhiteSpace(code) || code.Length != CodeDigits)
            return false;

        var secretBytes = Base32Decode(secret);
        var currentTimeStep = GetCurrentTimeStep();

        // Check current time step and +/- tolerance window
        for (long i = -TimeTolerance; i <= TimeTolerance; i++)
        {
            var computedCode = ComputeTotp(secretBytes, currentTimeStep + i);
            if (computedCode == code)
                return true;
        }

        return false;
    }

    public List<string> GenerateBackupCodes(int count = 8)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var codes = new List<string>(count);
        using var rng = RandomNumberGenerator.Create();

        for (int i = 0; i < count; i++)
        {
            var codeBytes = new byte[8];
            rng.GetBytes(codeBytes);
            var code = new char[8];
            for (int j = 0; j < 8; j++)
            {
                code[j] = chars[codeBytes[j] % chars.Length];
            }
            codes.Add(new string(code));
        }

        return codes;
    }

    private static long GetCurrentTimeStep()
    {
        var unixTimestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
        return unixTimestamp / TimeStep;
    }

    private static string ComputeTotp(byte[] secretBytes, long timeStep)
    {
        var timeBytes = BitConverter.GetBytes(timeStep);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(timeBytes);

        using var hmac = new HMACSHA1(secretBytes);
        var hash = hmac.ComputeHash(timeBytes);

        // Dynamic truncation (RFC 4226)
        var offset = hash[^1] & 0x0F;
        var binaryCode =
            ((hash[offset] & 0x7F) << 24) |
            ((hash[offset + 1] & 0xFF) << 16) |
            ((hash[offset + 2] & 0xFF) << 8) |
            (hash[offset + 3] & 0xFF);

        var otp = binaryCode % (int)Math.Pow(10, CodeDigits);
        return otp.ToString().PadLeft(CodeDigits, '0');
    }

    private static string Base32Encode(byte[] data)
    {
        var result = new List<char>();
        int buffer = 0;
        int bitsLeft = 0;

        foreach (var b in data)
        {
            buffer = (buffer << 8) | b;
            bitsLeft += 8;
            while (bitsLeft >= 5)
            {
                bitsLeft -= 5;
                result.Add(Base32Chars[(buffer >> bitsLeft) & 0x1F]);
            }
        }

        if (bitsLeft > 0)
        {
            buffer <<= (5 - bitsLeft);
            result.Add(Base32Chars[buffer & 0x1F]);
        }

        return new string(result.ToArray());
    }

    private static byte[] Base32Decode(string base32)
    {
        var cleanInput = base32.TrimEnd('=').ToUpperInvariant();
        var output = new List<byte>();
        int buffer = 0;
        int bitsLeft = 0;

        foreach (var c in cleanInput)
        {
            int val;
            if (c >= 'A' && c <= 'Z')
                val = c - 'A';
            else if (c >= '2' && c <= '7')
                val = c - '2' + 26;
            else
                throw new FormatException($"Invalid Base32 character: {c}");

            buffer = (buffer << 5) | val;
            bitsLeft += 5;

            if (bitsLeft >= 8)
            {
                bitsLeft -= 8;
                output.Add((byte)(buffer >> bitsLeft));
            }
        }

        return output.ToArray();
    }
}
