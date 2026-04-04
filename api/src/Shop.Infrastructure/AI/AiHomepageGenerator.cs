using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;

namespace Shop.Infrastructure.AI;

public class AiHomepageGenerator : IAiHomepageGenerator
{
    private readonly IShopDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly IImageGenerator _imageGenerator;
    private readonly ILogger<AiHomepageGenerator> _logger;

    public AiHomepageGenerator(IShopDbContext db, HttpClient httpClient, IImageGenerator imageGenerator, ILogger<AiHomepageGenerator> logger)
    {
        _db = db;
        _httpClient = httpClient;
        _imageGenerator = imageGenerator;
        _logger = logger;
    }

    public async Task GenerateHomepageAsync(int tenantId, string companyName, string businessType, string? businessDescription, CancellationToken ct = default)
    {
        _logger.LogInformation("Generating AI homepage for tenant {TenantId}: {Company} ({Type})", tenantId, companyName, businessType);

        // Step 1: Generate text content using AI
        var sections = await GenerateTextContentAsync(companyName, businessType, businessDescription, ct);

        // Step 2: Generate images for each section
        var sectionImages = new Dictionary<string, string>();
        foreach (var section in sections.Where(s => s.ImagePrompt != null))
        {
            try
            {
                var result = await _imageGenerator.GenerateAsync(section.ImagePrompt!, ct: ct);
                if (!string.IsNullOrEmpty(result.Url))
                    sectionImages[section.Title] = result.Url;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to generate image for section: {Title}", section.Title);
            }
        }

        // Step 3: Store as Page entities
        var sortOrder = 1;
        foreach (var section in sections)
        {
            var imageUrl = sectionImages.GetValueOrDefault(section.Title);
            var htmlContent = BuildSectionHtml(section.Title, section.Content, imageUrl, sortOrder);

            var exists = await _db.Pages.AnyAsync(p => p.TenantId == tenantId && p.Slug == $"home-section-{sortOrder}", ct);
            if (!exists)
            {
                _db.Pages.Add(new Page
                {
                    TenantId = tenantId,
                    Title = section.Title,
                    Slug = $"home-section-{sortOrder}",
                    Content = htmlContent,
                    IsPublished = true,
                    SortOrder = sortOrder,
                    CreatedBy = "AI-Generator"
                });
            }
            sortOrder++;
        }

        // Store the full homepage config in tenant ConfigJson
        var tenant = await _db.Tenants.FirstOrDefaultAsync(t => t.Id == tenantId, ct);
        if (tenant != null)
        {
            var config = string.IsNullOrEmpty(tenant.ConfigJson)
                ? new Dictionary<string, object>()
                : JsonSerializer.Deserialize<Dictionary<string, object>>(tenant.ConfigJson) ?? new();

            config["homepage"] = new
            {
                generated = true,
                generatedAt = DateTime.UtcNow,
                sections = sections.Select((s, i) => new
                {
                    order = i + 1,
                    title = s.Title,
                    content = s.Content,
                    imageUrl = sectionImages.GetValueOrDefault(s.Title)
                })
            };

            // Also set hero section in config
            var heroSection = sections.FirstOrDefault();
            if (heroSection != null)
            {
                config["hero"] = new
                {
                    title = heroSection.Title,
                    subtitle = heroSection.Content.Length > 100 ? heroSection.Content[..100] + "..." : heroSection.Content,
                    imageUrl = sectionImages.GetValueOrDefault(heroSection.Title)
                };
            }

            tenant.ConfigJson = JsonSerializer.Serialize(config);
            tenant.UpdatedBy = "AI-Generator";
            tenant.UpdatedAt = DateTime.UtcNow;
        }

        await _db.SaveChangesAsync(ct);
        _logger.LogInformation("AI homepage generated for {Company}: {Count} sections", companyName, sections.Count);
    }

    private async Task<List<GeneratedSection>> GenerateTextContentAsync(
        string companyName, string businessType, string? description, CancellationToken ct)
    {
        // Try AI generation first
        var aiContent = await TryAiGenerateAsync(companyName, businessType, description, ct);
        if (aiContent != null && aiContent.Count > 0)
            return aiContent;

        // Fallback: template-based generation
        return GenerateTemplateContent(companyName, businessType, description);
    }

    private async Task<List<GeneratedSection>?> TryAiGenerateAsync(
        string companyName, string businessType, string? description, CancellationToken ct)
    {
        var apiKey = Environment.GetEnvironmentVariable("CLAUDE_API_KEY")
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (string.IsNullOrEmpty(apiKey)) return null;

        var isClaude = !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("CLAUDE_API_KEY"));
        var prompt = BuildHomepagePrompt(companyName, businessType, description);

        try
        {
            string responseText;

            if (isClaude)
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.anthropic.com/v1/messages");
                request.Headers.Add("x-api-key", apiKey);
                request.Headers.Add("anthropic-version", "2023-06-01");
                request.Content = new StringContent(JsonSerializer.Serialize(new
                {
                    model = "claude-sonnet-4-20250514",
                    max_tokens = 2048,
                    messages = new[] { new { role = "user", content = prompt } }
                }), System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode) return null;
                var json = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                responseText = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "";
            }
            else
            {
                var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions");
                request.Headers.Add("Authorization", $"Bearer {apiKey}");
                request.Content = new StringContent(JsonSerializer.Serialize(new
                {
                    model = "gpt-4o",
                    messages = new[] { new { role = "user", content = prompt } }
                }), System.Text.Encoding.UTF8, "application/json");

                var response = await _httpClient.SendAsync(request, ct);
                if (!response.IsSuccessStatusCode) return null;
                var json = await response.Content.ReadAsStringAsync(ct);
                using var doc = JsonDocument.Parse(json);
                responseText = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";
            }

            return ParseAiSections(responseText);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "AI homepage generation failed, using template");
            return null;
        }
    }

    private static string BuildHomepagePrompt(string companyName, string businessType, string? description) =>
        $"다음 회사의 홈페이지 소개 콘텐츠를 한국어로 작성해주세요.\n\n" +
        $"회사명: {companyName}\n업종: {businessType}\n" +
        (description != null ? $"설명: {description}\n" : "") +
        "\n정확히 5개 섹션을 ```json``` 블록 안에 작성해주세요:\n" +
        "```json\n[\n" +
        "  {\"title\": \"히어로 제목 (10자 이내)\", \"content\": \"회사 핵심 가치 소개 (100자)\", \"imagePrompt\": \"영문 이미지 프롬프트\"},\n" +
        "  {\"title\": \"회사 소개\", \"content\": \"회사 설명 (200자)\", \"imagePrompt\": \"영문 이미지 프롬프트\"},\n" +
        "  {\"title\": \"주요 제품/서비스\", \"content\": \"제품 설명 (200자)\", \"imagePrompt\": \"영문 이미지 프롬프트\"},\n" +
        "  {\"title\": \"차별점\", \"content\": \"경쟁 우위 (150자)\", \"imagePrompt\": \"영문 이미지 프롬프트\"},\n" +
        "  {\"title\": \"연락처\", \"content\": \"연락 안내 (100자)\", \"imagePrompt\": null}\n" +
        "]\n```\n\nimagePrompt는 DALL-E 이미지 생성용 영문 프롬프트입니다. 비즈니스에 적합한 전문적인 이미지를 묘사해주세요.";

    private static List<GeneratedSection>? ParseAiSections(string text)
    {
        try
        {
            var jsonMatch = Regex.Match(text, @"```json\s*([\s\S]*?)\s*```");
            var jsonStr = jsonMatch.Success ? jsonMatch.Groups[1].Value : text.Trim();
            if (!jsonStr.StartsWith("[")) jsonStr = jsonStr[jsonStr.IndexOf('[')..];
            if (!jsonStr.EndsWith("]")) jsonStr = jsonStr[..(jsonStr.LastIndexOf(']') + 1)];

            var items = JsonSerializer.Deserialize<List<JsonElement>>(jsonStr);
            if (items == null) return null;

            return items.Select(item => new GeneratedSection(
                item.GetProperty("title").GetString() ?? "",
                item.GetProperty("content").GetString() ?? "",
                item.TryGetProperty("imagePrompt", out var ip) && ip.ValueKind == JsonValueKind.String ? ip.GetString() : null
            )).ToList();
        }
        catch { return null; }
    }

    private static List<GeneratedSection> GenerateTemplateContent(string companyName, string businessType, string? description)
    {
        var (industry, products, values) = businessType switch
        {
            "Food" => ("식품", "신선하고 건강한 식품", "안전한 먹거리"),
            "Fashion" => ("패션", "트렌디한 패션 아이템", "세련된 스타일"),
            "Beauty" => ("뷰티", "프리미엄 뷰티 제품", "아름다움과 자신감"),
            "Religious" => ("종교용품", "정성이 담긴 종교 용품", "신앙의 동반자"),
            "Manufacturing" => ("제조", "고품질 제조 제품", "신뢰의 품질"),
            "Electronics" => ("전자제품", "최신 전자 제품", "혁신적 기술"),
            _ => ("소매", "다양한 상품", "고객 만족"),
        };

        return new List<GeneratedSection>
        {
            new($"{companyName}", description ?? $"{companyName}과 함께하는 특별한 {industry} 경험. {values}을 최우선으로 생각합니다.", $"Professional {businessType} company hero banner, modern clean design, blue tones"),
            new("회사 소개", $"{companyName}은 {industry} 분야의 전문 기업입니다. 오랜 경험과 노하우를 바탕으로 고객에게 최고의 {products}을 제공합니다. {values}을 핵심 가치로 삼고 끊임없이 발전하고 있습니다.", $"Modern {businessType} office interior, professional workspace, warm lighting"),
            new("주요 제품", $"엄선된 {products}을 제공합니다. 품질 관리부터 배송까지 모든 과정을 철저히 관리하여 고객 만족을 실현합니다. SynDock AI 기반 수요 예측으로 항상 최적의 재고를 유지합니다.", $"Beautiful {businessType} product showcase, premium quality, studio photography"),
            new("왜 저희를 선택해야 하나요?", $"AI 기반 맞춤 추천, 빠른 배송, 안전한 결제, 그리고 진심이 담긴 고객 서비스. {companyName}은 단순한 쇼핑이 아닌 특별한 경험을 선사합니다.", $"Happy customers using {businessType} products, satisfaction, modern lifestyle"),
            new("문의하기", $"{companyName}에 궁금한 점이 있으시면 언제든 연락해주세요. 빠르고 친절하게 안내해 드리겠습니다.", null),
        };
    }

    private static string BuildSectionHtml(string title, string content, string? imageUrl, int order)
    {
        var imgHtml = imageUrl != null
            ? $"<img src=\"{imageUrl}\" alt=\"{title}\" style=\"width:100%;max-height:400px;object-fit:cover;border-radius:12px;margin-bottom:16px\" />"
            : "";
        return $"<section data-order=\"{order}\">{imgHtml}<h2>{title}</h2><p>{content}</p></section>";
    }
}
