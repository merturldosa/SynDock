using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Services;

public class KakaoAlimtalkService : IKakaoAlimtalkService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<KakaoAlimtalkService> _logger;
    private readonly string _senderKey;
    private readonly bool _enabled;

    private static class TemplateCode
    {
        public const string OrderConfirmed = "ORDER_CONFIRMED";
        public const string Shipped = "ORDER_SHIPPED";
        public const string Delivered = "ORDER_DELIVERED";
    }

    public KakaoAlimtalkService(HttpClient httpClient, IConfiguration configuration, ILogger<KakaoAlimtalkService> logger)
    {
        _httpClient = httpClient;
        _logger = logger;

        var section = configuration.GetSection("KakaoAlimtalk");
        _senderKey = section["SenderKey"] ?? "";
        _enabled = section.GetValue<bool>("Enabled");

        var apiKey = section["ApiKey"] ?? "";
        if (!string.IsNullOrEmpty(apiKey))
        {
            _httpClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", $"KakaoAK {apiKey}");
        }
    }

    public async Task<bool> SendOrderConfirmedAsync(string phoneNumber, string orderNumber, decimal totalAmount, CancellationToken ct = default)
    {
        var variables = new Dictionary<string, string>
        {
            ["#{orderNumber}"] = orderNumber,
            ["#{totalAmount}"] = totalAmount.ToString("N0"),
        };
        return await SendAsync(phoneNumber, TemplateCode.OrderConfirmed, variables, ct);
    }

    public async Task<bool> SendShippedAsync(string phoneNumber, string orderNumber, string? trackingCarrier, string? trackingNumber, CancellationToken ct = default)
    {
        var variables = new Dictionary<string, string>
        {
            ["#{orderNumber}"] = orderNumber,
            ["#{carrier}"] = trackingCarrier ?? "택배",
            ["#{trackingNumber}"] = trackingNumber ?? "-",
        };
        return await SendAsync(phoneNumber, TemplateCode.Shipped, variables, ct);
    }

    public async Task<bool> SendDeliveredAsync(string phoneNumber, string orderNumber, CancellationToken ct = default)
    {
        var variables = new Dictionary<string, string>
        {
            ["#{orderNumber}"] = orderNumber,
        };
        return await SendAsync(phoneNumber, TemplateCode.Delivered, variables, ct);
    }

    public async Task<bool> SendAsync(string phoneNumber, string templateCode, Dictionary<string, string> variables, CancellationToken ct = default)
    {
        if (!_enabled)
        {
            _logger.LogInformation("Kakao Alimtalk disabled: Template={Template}, Phone={Phone}", templateCode, MaskPhone(phoneNumber));
            return true;
        }

        if (string.IsNullOrEmpty(phoneNumber))
        {
            _logger.LogWarning("Alimtalk send failed: phone number missing (Template={Template})", templateCode);
            return false;
        }

        try
        {
            var normalizedPhone = NormalizePhone(phoneNumber);
            var templateContent = GetTemplateContent(templateCode, variables);

            var payload = new
            {
                senderkey = _senderKey,
                template_code = templateCode,
                receiver_num = normalizedPhone,
                template_object = new
                {
                    object_type = "text",
                    text = templateContent,
                    link = new { web_url = "", mobile_web_url = "" },
                }
            };

            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _httpClient.PostAsync("/v2/api/talk/memo/send", content, ct);

            if (response.IsSuccessStatusCode)
            {
                _logger.LogInformation("Alimtalk sent successfully: Template={Template}, Phone={Phone}", templateCode, MaskPhone(phoneNumber));
                return true;
            }

            var errorBody = await response.Content.ReadAsStringAsync(ct);
            _logger.LogWarning("Alimtalk send failed: Status={Status}, Body={Body}", response.StatusCode, errorBody);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Alimtalk send exception: Template={Template}", templateCode);
            return false;
        }
    }

    private static string GetTemplateContent(string templateCode, Dictionary<string, string> variables)
    {
        var template = templateCode switch
        {
            TemplateCode.OrderConfirmed =>
                "[주문 확인]\n\n주문번호: #{orderNumber}\n결제금액: #{totalAmount}원\n\n주문이 정상적으로 확인되었습니다.\n상품 준비 후 발송해 드리겠습니다.",
            TemplateCode.Shipped =>
                "[배송 시작]\n\n주문번호: #{orderNumber}\n택배사: #{carrier}\n운송장번호: #{trackingNumber}\n\n상품이 발송되었습니다.\n배송 완료까지 2~3일 소요됩니다.",
            TemplateCode.Delivered =>
                "[배송 완료]\n\n주문번호: #{orderNumber}\n\n상품이 배송 완료되었습니다.\n이용해 주셔서 감사합니다.",
            _ => ""
        };

        foreach (var (key, value) in variables)
        {
            template = template.Replace(key, value);
        }

        return template;
    }

    private static string NormalizePhone(string phone)
    {
        var digits = new string(phone.Where(char.IsDigit).ToArray());
        if (digits.StartsWith("82")) return digits;
        if (digits.StartsWith("0")) return "82" + digits[1..];
        return digits;
    }

    private static string MaskPhone(string phone)
    {
        if (string.IsNullOrEmpty(phone) || phone.Length < 4) return "****";
        return phone[..^4] + "****";
    }
}
