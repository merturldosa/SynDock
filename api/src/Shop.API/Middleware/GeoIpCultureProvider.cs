using Microsoft.AspNetCore.Localization;
using Shop.Application.Common.Interfaces;

namespace Shop.API.Middleware;

/// <summary>
/// IP 기반 다국어 자동감지 Culture Provider.
/// 우선순위: CDN 헤더 (CF-IPCountry 등) → MaxMind GeoIP2 DB → null (다음 provider로)
/// </summary>
public class GeoIpCultureProvider : IRequestCultureProvider
{
    private static readonly Dictionary<string, string> CountryToCulture = new(StringComparer.OrdinalIgnoreCase)
    {
        ["KR"] = "ko",     // 한국
        ["JP"] = "ja",     // 일본
        ["CN"] = "zh-CN",  // 중국
        ["TW"] = "zh-CN",  // 대만
        ["HK"] = "zh-CN",  // 홍콩
        ["VN"] = "vi",     // 베트남
    };

    public Task<ProviderCultureResult?> DetermineProviderCultureResult(HttpContext httpContext)
    {
        // 1. Check CDN/Proxy country headers (production environment)
        var countryCode = httpContext.Request.Headers["CF-IPCountry"].FirstOrDefault()           // Cloudflare
            ?? httpContext.Request.Headers["X-Country-Code"].FirstOrDefault()                    // Custom proxy
            ?? httpContext.Request.Headers["X-Vercel-IP-Country"].FirstOrDefault()               // Vercel
            ?? httpContext.Request.Headers["CloudFront-Viewer-Country"].FirstOrDefault();        // AWS CloudFront

        // 2. Use MaxMind GeoIP2 if CDN header not available
        if (string.IsNullOrEmpty(countryCode))
        {
            var geoIpService = httpContext.RequestServices.GetService<IGeoIpService>();
            if (geoIpService != null)
            {
                countryCode = geoIpService.GetCountryCode(httpContext.Connection.RemoteIpAddress);
            }
        }

        if (string.IsNullOrEmpty(countryCode))
            return Task.FromResult<ProviderCultureResult?>(null);

        // 3. Map country code → culture (default: English)
        var culture = CountryToCulture.GetValueOrDefault(countryCode, "en");

        return Task.FromResult<ProviderCultureResult?>(
            new ProviderCultureResult(culture, culture));
    }
}
