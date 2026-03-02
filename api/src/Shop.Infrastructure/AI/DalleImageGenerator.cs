using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.AI;

public class DalleImageGenerator : IImageGenerator
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;
    private readonly string _model;
    private readonly ILogger<DalleImageGenerator> _logger;

    public DalleImageGenerator(HttpClient httpClient, IConfiguration configuration, ILogger<DalleImageGenerator> logger)
    {
        _httpClient = httpClient;
        _apiKey = configuration["AI:OpenAI:ApiKey"] ?? "";
        _model = configuration["AI:OpenAI:ImageModel"] ?? "dall-e-3";
        _logger = logger;

        _httpClient.BaseAddress = new Uri("https://api.openai.com");
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    public async Task<GeneratedImage> GenerateAsync(string prompt, string size = "1024x1024", CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_apiKey))
        {
            return new GeneratedImage("", "API key not configured — 이미지 생성 서비스가 설정되지 않았습니다.");
        }

        try
        {
            var requestBody = new
            {
                model = _model,
                prompt,
                n = 1,
                size,
                quality = "standard"
            };

            var response = await _httpClient.PostAsJsonAsync("/v1/images/generations", requestBody, ct);
            var responseJson = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("DALL-E API error: {StatusCode} {Response}", response.StatusCode, responseJson);
                return new GeneratedImage("", $"이미지 생성 실패: {response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<DalleApiResponse>(responseJson);
            var data = result?.Data?.FirstOrDefault();

            return new GeneratedImage(
                data?.Url ?? "",
                data?.RevisedPrompt);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "DALL-E API call failed");
            return new GeneratedImage("", $"이미지 생성 서비스 오류: {ex.Message}");
        }
    }

    private record DalleApiResponse(
        [property: JsonPropertyName("data")] List<DalleImageData>? Data);

    private record DalleImageData(
        [property: JsonPropertyName("url")] string? Url,
        [property: JsonPropertyName("revised_prompt")] string? RevisedPrompt);
}
