using System.Globalization;
using Microsoft.AspNetCore.Mvc;
using Shop.Application.Common.Interfaces;

namespace Shop.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class LocaleController : ControllerBase
{
    private static readonly Dictionary<string, string> CountryToCulture = new(StringComparer.OrdinalIgnoreCase)
    {
        ["KR"] = "ko",
        ["JP"] = "ja",
        ["CN"] = "zh-CN",
        ["TW"] = "zh-CN",
        ["HK"] = "zh-CN",
        ["VN"] = "vi",
    };

    private static readonly string[] SupportedCultures = ["ko", "en", "ja", "zh-CN", "vi"];

    private readonly IGeoIpService _geoIpService;

    public LocaleController(IGeoIpService geoIpService)
    {
        _geoIpService = geoIpService;
    }

    /// <summary>
    /// 현재 요청의 감지된 언어/국가 정보 반환 (프론트엔드 i18n 초기화용)
    /// </summary>
    [HttpGet("detect")]
    public IActionResult DetectLocale()
    {
        // 1. CDN header check
        var countryCode = Request.Headers["CF-IPCountry"].FirstOrDefault()
            ?? Request.Headers["X-Country-Code"].FirstOrDefault()
            ?? Request.Headers["X-Vercel-IP-Country"].FirstOrDefault()
            ?? Request.Headers["CloudFront-Viewer-Country"].FirstOrDefault();

        // 2. GeoIP lookup
        if (string.IsNullOrEmpty(countryCode))
        {
            countryCode = _geoIpService.GetCountryCode(HttpContext.Connection.RemoteIpAddress);
        }

        // 3. Determine culture
        var detectedCulture = !string.IsNullOrEmpty(countryCode)
            ? CountryToCulture.GetValueOrDefault(countryCode, "en")
            : null;

        // 4. Current active culture (from middleware pipeline)
        var currentCulture = CultureInfo.CurrentUICulture.Name;

        return Ok(new
        {
            detectedCountry = countryCode,
            detectedLanguage = detectedCulture,
            activeLanguage = currentCulture,
            supportedLanguages = SupportedCultures,
            source = !string.IsNullOrEmpty(Request.Headers["CF-IPCountry"].FirstOrDefault()) ? "cdn"
                : !string.IsNullOrEmpty(countryCode) ? "geoip"
                : "default"
        });
    }

    /// <summary>
    /// 지원 언어 목록 반환
    /// </summary>
    [HttpGet("languages")]
    public IActionResult GetSupportedLanguages()
    {
        return Ok(new[]
        {
            new { code = "ko", name = "한국어", nativeName = "한국어" },
            new { code = "en", name = "English", nativeName = "English" },
            new { code = "ja", name = "Japanese", nativeName = "日本語" },
            new { code = "zh-CN", name = "Chinese (Simplified)", nativeName = "中文(简体)" },
            new { code = "vi", name = "Vietnamese", nativeName = "Tiếng Việt" },
        });
    }
}
