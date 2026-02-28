using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.AI;

public class ClaudeAIChatProvider : IAIChatProvider
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<ClaudeAIChatProvider> _logger;

    public ClaudeAIChatProvider(HttpClient httpClient, IConfiguration configuration, ILogger<ClaudeAIChatProvider> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["AI:Claude:ApiKey"] ?? "";
        _model = configuration["AI:Claude:Model"] ?? "claude-sonnet-4-20250514";
        _logger = logger;

        _httpClient.BaseAddress = new Uri("https://api.anthropic.com");
        _httpClient.DefaultRequestHeaders.Add("x-api-key", _apiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", "2023-06-01");
    }

    public async Task<ChatResponse> ChatAsync(
        IReadOnlyList<ChatMessage> messages,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return new ChatResponse("AI 서비스가 설정되지 않았습니다. 관리자에게 문의해 주세요.", "API key not configured");
        }

        try
        {
            var requestBody = new
            {
                model = _model,
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

    private record ClaudeApiResponse(
        [property: JsonPropertyName("content")] List<ContentBlock>? Content);

    private record ContentBlock(
        [property: JsonPropertyName("type")] string Type,
        [property: JsonPropertyName("text")] string? Text);
}
