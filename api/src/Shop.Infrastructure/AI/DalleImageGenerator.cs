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

public class DalleImageGenerator : IImageGenerator
{
    private readonly HttpClient _httpClient;
    private readonly string _fallbackApiKey;
    private readonly string _fallbackModel;
    private readonly IShopDbContext _db;
    private readonly ITenantContext _tenantContext;
    private readonly ILogger<DalleImageGenerator> _logger;

    public DalleImageGenerator(HttpClient httpClient, IConfiguration configuration,
        IShopDbContext db, ITenantContext tenantContext, ILogger<DalleImageGenerator> logger)
    {
        _httpClient = httpClient;
        _fallbackApiKey = configuration["AI:OpenAI:ApiKey"] ?? "";
        _fallbackModel = configuration["AI:OpenAI:ImageModel"] ?? "dall-e-3";
        _db = db;
        _tenantContext = tenantContext;
        _logger = logger;

        _httpClient.BaseAddress = new Uri("https://api.openai.com");
    }

    public async Task<GeneratedImage> GenerateAsync(string prompt, string size = "1024x1024", CancellationToken ct = default)
    {
        var (apiKey, model) = await GetApiKeyFromTenant(ct);

        if (string.IsNullOrEmpty(apiKey))
            return new GeneratedImage("", "API key not configured — 관리자 설정에서 OpenAI API 키를 입력해 주세요.");

        try
        {
            _httpClient.DefaultRequestHeaders.Remove("Authorization");
            _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

            var requestBody = new { model, prompt, n = 1, size, quality = "standard" };

            var response = await _httpClient.PostAsJsonAsync("/v1/images/generations", requestBody, ct);
            var responseJson = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("DALL-E API error: {StatusCode} {Response}", response.StatusCode, responseJson);
                return new GeneratedImage("", $"이미지 생성 실패: {response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<DalleApiResponse>(responseJson);
            var data = result?.Data?.FirstOrDefault();

            return new GeneratedImage(data?.Url ?? "", data?.RevisedPrompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DALL-E API call failed");
            return new GeneratedImage("", $"이미지 생성 서비스 오류: {ex.Message}");
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
                    var key = ai["openAiApiKey"]?.GetValue<string>();
                    var model = ai["dalleModel"]?.GetValue<string>() ?? _fallbackModel;
                    if (!string.IsNullOrEmpty(key)) return (key, model);
                }
            }
        }
        catch (Exception ex) { _logger.LogWarning(ex, "Failed to read tenant AI config"); }

        return (_fallbackApiKey, _fallbackModel);
    }

    private record DalleApiResponse([property: JsonPropertyName("data")] List<DalleImageData>? Data);
    private record DalleImageData([property: JsonPropertyName("url")] string? Url, [property: JsonPropertyName("revised_prompt")] string? RevisedPrompt);
}
