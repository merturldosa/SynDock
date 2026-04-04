using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Interfaces;

namespace Shop.Infrastructure.AI;

public class ClaudeAIChatProvider : IAIChatProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _fallbackApiKey;
    private readonly string _fallbackModel;
    private readonly IShopDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<ClaudeAIChatProvider> _logger;

    public ClaudeAIChatProvider(HttpClient httpClient, IConfiguration configuration,
        IShopDbContext db, ITenantContext tenantContext, ILogger<ClaudeAIChatProvider> logger)
    {
        _httpClient = httpClient;
        _fallbackApiKey = configuration["AI:Claude:ApiKey"] ?? "";
        _fallbackModel = configuration["AI:Claude:Model"] ?? "claude-sonnet-4-20250514";
        _db = db;
        _tenantContext = tenantContext;
        _logger = logger;

        _httpClient.BaseAddress = new Uri("https://api.anthropic.com");
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async Task<ChatResponse> ChatAsync(
        IReadOnlyList<AiChatMessage> messages,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default)
    {
        var (apiKey, model) = await GetApiKeyFromTenant(cancellationToken);

        if (string.IsNullOrEmpty(apiKey))
            return new ChatResponse("AI 서비스가 설정되지 않았습니다. 관리자 설정에서 Claude API 키를 입력해 주세요.", "API key not configured");

        try
        {
            _httpClient.DefaultRequestHeaders.Remove("x-api-key");
            _httpClient.DefaultRequestHeaders.Add("x-api-key", apiKey);

            var requestBody = new
            {
                model,
                max_tokens = 1024,
                system = systemPrompt ?? "You are a helpful shopping assistant. Answer questions about products, orders, and general shopping inquiries in Korean.",
                messages = messages.Select(m => new { role = m.Role, content = m.Content }).ToArray()
            };

            var response = await _httpClient.PostAsJsonAsync("/v1/messages", requestBody, cancellationToken);
            var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Claude API error: {StatusCode} {Response}", response.StatusCode, responseJson);
                return new ChatResponse("AI 응답에 실패했습니다. 잠시 후 다시 시도해 주세요.", responseJson);
            }

            var result = JsonSerializer.Deserialize<ClaudeApiResponse>(responseJson);
            var content = result?.Content?.FirstOrDefault()?.Text ?? "응답을 받지 못했습니다.";

            return new ChatResponse(content, null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Claude API call failed");
            return new ChatResponse("AI 서비스에 일시적인 오류가 발생했습니다.", ex.Message);
        }
    }

    private async Task<(string apiKey, string model)> GetApiKeyFromTenant(CancellationToken ct)
    {
        try
        {
            var tenant = await _db.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == _tenantContext.TenantId, ct);

            if (tenant?.ConfigJson is not null)
            {
                var config = JsonNode.Parse(tenant.ConfigJson)?.AsObject();
                var ai = config?["aiIntegration"]?.AsObject();
                if (ai is not null)
                {
                    var key = ai["claudeApiKey"]?.GetValue<string>();
                    var model = ai["claudeModel"]?.GetValue<string>() ?? _fallbackModel;
                    if (!string.IsNullOrEmpty(key)) return (key, model);
                }
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to read tenant AI config"); }

        return (_fallbackApiKey, _fallbackModel);
    }

    private record ClaudeApiResponse([property: JsonPropertyName("content")] List<ContentBlock>? Content);
    private record ContentBlock([property: JsonPropertyName("type")] string Type, [property: JsonPropertyName("text")] string? Text);
}
