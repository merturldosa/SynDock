using System.Net;

namespace Shop.Application.Common.Interfaces;

public interface IGeoIpService
{
    string? GetCountryCode(IPAddress? ipAddress);
}
