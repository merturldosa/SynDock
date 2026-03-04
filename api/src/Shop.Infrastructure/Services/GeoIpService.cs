using System.Net;
using MaxMind.GeoIP2;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Services;

public class GeoIpService : IGeoIpService, IDisposable
{
    private readonly DatabaseReader? _reader;
    private readonly ILogger<GeoIpService> _logger;

    public GeoIpService(ILogger<GeoIpService> logger)
    {
        _logger = logger;

        var dbPath = FindDatabasePath();
        if (dbPath != null)
        {
            try
            {
                _reader = new DatabaseReader(dbPath);
                _logger.LogInformation("GeoIP database loaded from {Path}", dbPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to load GeoIP database from {Path}", dbPath);
            }
        }
        else
        {
            _logger.LogInformation("GeoIP database not found. IP-based language detection will rely on CDN headers.");
        }
    }

    public string? GetCountryCode(IPAddress? ipAddress)
    {
        if (ipAddress == null || _reader == null)
            return null;

        // Skip private/loopback IPs
        if (IPAddress.IsLoopback(ipAddress) || IsPrivateIp(ipAddress))
            return null;

        try
        {
            if (_reader.TryCountry(ipAddress, out var response))
                return response?.Country?.IsoCode;
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "GeoIP lookup failed for {IP}", ipAddress);
        }

        return null;
    }

    public void Dispose()
    {
        _reader?.Dispose();
    }

    private static string? FindDatabasePath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "GeoLite2-Country.mmdb"),
            Path.Combine(AppContext.BaseDirectory, "Data", "GeoLite2-Country.mmdb"),
            Path.Combine(Directory.GetCurrentDirectory(), "GeoLite2-Country.mmdb"),
            "/usr/share/GeoIP/GeoLite2-Country.mmdb" // Linux standard path
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private static bool IsPrivateIp(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();
        return bytes.Length == 4 && bytes[0] switch
        {
            10 => true,
            172 => bytes[1] >= 16 && bytes[1] <= 31,
            192 => bytes[1] == 168,
            _ => false
        };
    }
}
