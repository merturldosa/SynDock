using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Entities;

namespace Shop.Infrastructure.Services;

public class ShopMigrationService : IShopMigrationService
{
    private readonly IShopDbContext _db;
    private readonly HttpClient _httpClient;
    private readonly ILogger<ShopMigrationService> _logger;

    public ShopMigrationService(IShopDbContext db, HttpClient httpClient, ILogger<ShopMigrationService> logger)
    {
        _db = db;
        _httpClient = httpClient;
        _logger = logger;
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("SynDock-Migrator/1.0");
        _httpClient.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task<MigrationJobDto> StartMigrationAsync(int? tenantId, int? applicationId, string sourceUrl, string sourceType, string createdBy, CancellationToken ct = default)
    {
        var job = new MigrationJob
        {
            TenantId = tenantId,
            ApplicationId = applicationId,
            SourceUrl = sourceUrl.TrimEnd('/'),
            SourceType = DetectSourceType(sourceUrl, sourceType),
            Status = "Crawling",
            StartedAt = DateTime.UtcNow,
            CreatedBy = createdBy
        };
        _db.MigrationJobs.Add(job);
        await _db.SaveChangesAsync(ct);

        // Run crawling in background
        _ = Task.Run(async () =>
        {
            try
            {
                var preview = await CrawlWebsiteAsync(job.SourceUrl, ct);
                job.TotalCategoriesFound = preview.Categories.Count;
                job.TotalProductsFound = preview.Products.Count;
                job.TotalImagesFound = preview.TotalImages;
                job.CrawlResultJson = JsonSerializer.Serialize(preview);
                job.Status = "Extracting";
                job.ProgressPercent = 50;
                await _db.SaveChangesAsync(ct);

                // Auto-import if tenant is assigned
                if (tenantId.HasValue && tenantId.Value > 0)
                {
                    await ImportToTenantAsync(job, tenantId.Value, preview, ct);
                }
                else
                {
                    job.Status = "Completed";
                    job.ProgressPercent = 100;
                    job.CompletedAt = DateTime.UtcNow;
                }
                await _db.SaveChangesAsync(ct);
            }
            catch (Exception ex)
            {
                job.Status = "Failed";
                job.ErrorMessage = ex.Message;
                job.CompletedAt = DateTime.UtcNow;
                await _db.SaveChangesAsync(CancellationToken.None);
                _logger.LogError(ex, "Migration failed for {Url}", job.SourceUrl);
            }
        }, ct);

        return ToDto(job);
    }

    public async Task<MigrationJobDto?> GetJobAsync(int jobId, CancellationToken ct = default)
    {
        var job = await _db.MigrationJobs.AsNoTracking().FirstOrDefaultAsync(j => j.Id == jobId, ct);
        return job == null ? null : ToDto(job);
    }

    public async Task<List<MigrationJobDto>> GetJobsAsync(int? tenantId = null, CancellationToken ct = default)
    {
        var query = _db.MigrationJobs.AsNoTracking().AsQueryable();
        if (tenantId.HasValue) query = query.Where(j => j.TenantId == tenantId.Value);
        return await query.OrderByDescending(j => j.CreatedAt).Select(j => ToDto(j)).ToListAsync(ct);
    }

    public async Task<MigrationPreviewDto> PreviewMigrationAsync(string sourceUrl, CancellationToken ct = default)
    {
        return await CrawlWebsiteAsync(sourceUrl.TrimEnd('/'), ct);
    }

    public async Task ImportCrawlResultsAsync(int jobId, int tenantId, CancellationToken ct = default)
    {
        var job = await _db.MigrationJobs.FirstOrDefaultAsync(j => j.Id == jobId, ct)
            ?? throw new InvalidOperationException("Job not found");

        if (string.IsNullOrEmpty(job.CrawlResultJson))
            throw new InvalidOperationException("No crawl results to import");

        var preview = JsonSerializer.Deserialize<MigrationPreviewDto>(job.CrawlResultJson)!;
        job.TenantId = tenantId;
        await ImportToTenantAsync(job, tenantId, preview, ct);
        await _db.SaveChangesAsync(ct);
    }

    private async Task<MigrationPreviewDto> CrawlWebsiteAsync(string baseUrl, CancellationToken ct)
    {
        _logger.LogInformation("AI-powered crawl starting for {Url}", baseUrl);

        // Step 1: Fetch main page HTML
        string html;
        try
        {
            html = await _httpClient.GetStringAsync(baseUrl, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to fetch {Url}", baseUrl);
            return new MigrationPreviewDto(baseUrl, "Website", "Unknown Shop", new(), new(), 0, null);
        }

        var sourceType = DetectSourceType(baseUrl, "Website");

        // Step 2: Try SPA-specific extraction (Next.js, React, Vue)
        var spaResult = await TrySpaExtractionAsync(baseUrl, html, sourceType, ct);
        if (spaResult != null && spaResult.Products.Count > 0)
        {
            _logger.LogInformation("SPA extraction found {Products} products from {Url}", spaResult.Products.Count, baseUrl);
            return spaResult;
        }

        // Step 3: Truncate HTML for AI analysis
        var cleanHtml = TruncateHtml(html, 30000);

        // Step 4: AI Analysis
        var aiResult = await AnalyzeWithAiAsync(baseUrl, sourceType, cleanHtml, ct);
        if (aiResult != null && aiResult.Products.Count > 0)
        {
            _logger.LogInformation("AI extracted {Products} products, {Categories} categories from {Url}",
                aiResult.Products.Count, aiResult.Categories.Count, baseUrl);
            return aiResult;
        }

        // Step 5: Fallback to basic extraction
        _logger.LogWarning("AI unavailable or no products found, falling back to basic extraction for {Url}", baseUrl);
        return FallbackBasicExtraction(baseUrl, html, sourceType);
    }

    /// <summary>
    /// Extract products from SPA frameworks (Next.js __NEXT_DATA__, API endpoints, etc.)
    /// </summary>
    private async Task<MigrationPreviewDto?> TrySpaExtractionAsync(string baseUrl, string html, string sourceType, CancellationToken ct)
    {
        var categories = new List<CrawledCategory>();
        var products = new List<CrawledProduct>();
        var totalImages = 0;
        string? shopName = null;

        // Extract <title>
        var titleMatch = Regex.Match(html, @"<title[^>]*>([^<]+)</title>", RegexOptions.IgnoreCase);
        shopName = titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : new Uri(baseUrl).Host;

        // === Strategy 1: Next.js __NEXT_DATA__ ===
        var nextDataMatch = Regex.Match(html, @"<script\s+id=""__NEXT_DATA__""[^>]*>([\s\S]*?)</script>", RegexOptions.IgnoreCase);
        if (nextDataMatch.Success)
        {
            _logger.LogInformation("Found Next.js __NEXT_DATA__ for {Url}", baseUrl);
            try
            {
                using var doc = JsonDocument.Parse(nextDataMatch.Groups[1].Value);
                ExtractFromJsonRecursive(doc.RootElement, baseUrl, products, categories, ref totalImages);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to parse __NEXT_DATA__");
            }
        }

        // === Strategy 2: Try common API endpoints ===
        if (products.Count == 0)
        {
            var apiPaths = new[]
            {
                "/api/products", "/api/product", "/api/items",
                "/api/goods", "/api/shop/products",
                "/_next/data", "/products.json",
            };

            foreach (var apiPath in apiPaths)
            {
                try
                {
                    var apiUrl = baseUrl.TrimEnd('/') + apiPath;
                    var response = await _httpClient.GetAsync(apiUrl, ct);
                    if (!response.IsSuccessStatusCode) continue;

                    var content = await response.Content.ReadAsStringAsync(ct);
                    if (string.IsNullOrEmpty(content) || !content.TrimStart().StartsWith("{") && !content.TrimStart().StartsWith("["))
                        continue;

                    using var doc = JsonDocument.Parse(content);
                    ExtractFromJsonRecursive(doc.RootElement, baseUrl, products, categories, ref totalImages);

                    if (products.Count > 0)
                    {
                        _logger.LogInformation("Found {Count} products from API endpoint {Path}", products.Count, apiPath);
                        break;
                    }
                }
                catch { /* Skip failed API endpoints */ }
            }
        }

        // === Strategy 3: Scan for product-like JSON in HTML ===
        if (products.Count == 0)
        {
            var jsonBlocks = Regex.Matches(html, @"<script[^>]*>\s*(\{[\s\S]*?""(?:products?|items?|goods)""\s*:[\s\S]*?\})\s*</script>", RegexOptions.IgnoreCase);
            foreach (Match block in jsonBlocks)
            {
                try
                {
                    using var doc = JsonDocument.Parse(block.Groups[1].Value);
                    ExtractFromJsonRecursive(doc.RootElement, baseUrl, products, categories, ref totalImages);
                    if (products.Count > 0) break;
                }
                catch { }
            }
        }

        // === Strategy 4: Crawl sub-pages for product listings ===
        if (products.Count == 0)
        {
            var productLinks = Regex.Matches(html,
                @"href=""(/(?:product|item|goods|shop|detail)[^""]*?)""",
                RegexOptions.IgnoreCase);

            var visited = new HashSet<string>();
            foreach (Match link in productLinks)
            {
                if (products.Count >= 20) break;
                var path = link.Groups[1].Value;
                if (!visited.Add(path)) continue;

                try
                {
                    var pageUrl = baseUrl.TrimEnd('/') + path;
                    var pageHtml = await _httpClient.GetStringAsync(pageUrl, ct);

                    // Check for __NEXT_DATA__ on sub-page
                    var subNextData = Regex.Match(pageHtml, @"<script\s+id=""__NEXT_DATA__""[^>]*>([\s\S]*?)</script>", RegexOptions.IgnoreCase);
                    if (subNextData.Success)
                    {
                        using var doc = JsonDocument.Parse(subNextData.Groups[1].Value);
                        ExtractFromJsonRecursive(doc.RootElement, baseUrl, products, categories, ref totalImages);
                    }

                    await Task.Delay(300, ct); // Rate limit
                }
                catch { }
            }
        }

        if (products.Count == 0) return null;

        if (categories.Count == 0)
            categories.Add(new CrawledCategory("전체상품", null, products.Count));

        return new MigrationPreviewDto(baseUrl, sourceType, shopName ?? "Shop", categories, products, totalImages, null);
    }

    /// <summary>
    /// Recursively search JSON for product-like data structures
    /// </summary>
    private static void ExtractFromJsonRecursive(JsonElement element, string baseUrl, List<CrawledProduct> products, List<CrawledCategory> categories, ref int totalImages, int depth = 0)
    {
        if (depth > 10 || products.Count >= 50) return;

        if (element.ValueKind == JsonValueKind.Object)
        {
            // Check if this object looks like a product
            var hasName = element.TryGetProperty("name", out var nameEl) || element.TryGetProperty("title", out nameEl) || element.TryGetProperty("productName", out nameEl);
            var hasPrice = element.TryGetProperty("price", out var priceEl) || element.TryGetProperty("salePrice", out priceEl) || element.TryGetProperty("cost", out priceEl);

            if (hasName && hasPrice && nameEl.ValueKind == JsonValueKind.String)
            {
                var name = nameEl.GetString() ?? "";
                if (name.Length >= 2 && name.Length <= 200)
                {
                    decimal price = 0;
                    if (priceEl.ValueKind == JsonValueKind.Number) price = priceEl.GetDecimal();
                    else if (priceEl.ValueKind == JsonValueKind.String)
                        decimal.TryParse(Regex.Replace(priceEl.GetString() ?? "", @"[^\d.]", ""), out price);

                    string? imageUrl = null;
                    if (element.TryGetProperty("image", out var imgEl) || element.TryGetProperty("imageUrl", out imgEl) || element.TryGetProperty("thumbnail", out imgEl) || element.TryGetProperty("img", out imgEl))
                    {
                        imageUrl = imgEl.ValueKind == JsonValueKind.String ? imgEl.GetString() : null;
                        if (imageUrl != null && !imageUrl.StartsWith("http"))
                            imageUrl = ResolveUrl(baseUrl, imageUrl);
                        if (imageUrl != null) totalImages++;
                    }

                    string? desc = null;
                    if (element.TryGetProperty("description", out var descEl) || element.TryGetProperty("desc", out descEl))
                        desc = descEl.ValueKind == JsonValueKind.String ? descEl.GetString() : null;

                    string? category = null;
                    if (element.TryGetProperty("category", out var catEl) || element.TryGetProperty("categoryName", out catEl))
                        category = catEl.ValueKind == JsonValueKind.String ? catEl.GetString() : null;

                    // Avoid duplicates
                    if (!products.Any(p => p.Name == name))
                    {
                        products.Add(new CrawledProduct(name, desc, price, null, category ?? "전체상품", imageUrl, null, null));

                        if (category != null && !categories.Any(c => c.Name == category))
                            categories.Add(new CrawledCategory(category, null, 0));
                    }
                    return; // Don't recurse into product objects
                }
            }

            // Recurse into object properties
            foreach (var prop in element.EnumerateObject())
            {
                ExtractFromJsonRecursive(prop.Value, baseUrl, products, categories, ref totalImages, depth + 1);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                ExtractFromJsonRecursive(item, baseUrl, products, categories, ref totalImages, depth + 1);
            }
        }
    }

    /// <summary>
    /// AI-powered shop analysis using Claude API.
    /// Sends page HTML to Claude and gets structured product/category data back.
    /// Handles errors automatically with retry and self-correction.
    /// </summary>
    private async Task<MigrationPreviewDto?> AnalyzeWithAiAsync(string baseUrl, string sourceType, string html, CancellationToken ct)
    {
        // Try Claude API first, then OpenAI as fallback
        var aiEndpoints = new[]
        {
            ("https://api.anthropic.com/v1/messages", "CLAUDE_API_KEY", "claude"),
            ("https://api.openai.com/v1/chat/completions", "OPENAI_API_KEY", "openai"),
        };

        foreach (var (endpoint, envKey, provider) in aiEndpoints)
        {
            var apiKey = Environment.GetEnvironmentVariable(envKey) ?? "";
            if (string.IsNullOrEmpty(apiKey)) continue;

            for (var attempt = 1; attempt <= 2; attempt++)
            {
                try
                {
                    var result = provider == "claude"
                        ? await CallClaudeAsync(endpoint, apiKey, baseUrl, sourceType, html, attempt, ct)
                        : await CallOpenAiAsync(endpoint, apiKey, baseUrl, sourceType, html, attempt, ct);

                    if (result != null) return result;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "AI analysis attempt {Attempt} failed with {Provider} for {Url}", attempt, provider, baseUrl);
                    if (attempt < 2) await Task.Delay(1000, ct); // Wait before retry
                }
            }
        }

        return null; // All AI providers failed
    }

    private async Task<MigrationPreviewDto?> CallClaudeAsync(string endpoint, string apiKey, string baseUrl, string sourceType, string html, int attempt, CancellationToken ct)
    {
        var systemPrompt = attempt == 1
            ? GetExtractionPrompt(sourceType)
            : GetCorrectionPrompt(sourceType); // Second attempt uses error-correction prompt

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("x-api-key", apiKey);
        request.Headers.Add("anthropic-version", "2023-06-01");
        request.Content = new StringContent(JsonSerializer.Serialize(new
        {
            model = "claude-sonnet-4-20250514",
            max_tokens = 4096,
            system = systemPrompt,
            messages = new[]
            {
                new { role = "user", content = $"다음 쇼핑몰 HTML을 분석하여 상품과 카테고리를 추출하세요.\n\nURL: {baseUrl}\n플랫폼: {sourceType}\n\n<html>\n{html}\n</html>" }
            }
        }), System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return null;

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseJson);
        var text = doc.RootElement.GetProperty("content")[0].GetProperty("text").GetString() ?? "";

        return ParseAiResponse(baseUrl, sourceType, text);
    }

    private async Task<MigrationPreviewDto?> CallOpenAiAsync(string endpoint, string apiKey, string baseUrl, string sourceType, string html, int attempt, CancellationToken ct)
    {
        var systemPrompt = attempt == 1
            ? GetExtractionPrompt(sourceType)
            : GetCorrectionPrompt(sourceType);

        var request = new HttpRequestMessage(HttpMethod.Post, endpoint);
        request.Headers.Add("Authorization", $"Bearer {apiKey}");
        request.Content = new StringContent(JsonSerializer.Serialize(new
        {
            model = "gpt-4o",
            max_tokens = 4096,
            messages = new object[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = $"다음 쇼핑몰 HTML을 분석하여 상품과 카테고리를 추출하세요.\n\nURL: {baseUrl}\n플랫폼: {sourceType}\n\n<html>\n{html}\n</html>" }
            }
        }), System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.SendAsync(request, ct);
        if (!response.IsSuccessStatusCode) return null;

        var responseJson = await response.Content.ReadAsStringAsync(ct);
        using var doc = JsonDocument.Parse(responseJson);
        var text = doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? "";

        return ParseAiResponse(baseUrl, sourceType, text);
    }

    private static string GetExtractionPrompt(string sourceType) =>
        "당신은 쇼핑몰 데이터 추출 전문가입니다. HTML을 분석하여 상품과 카테고리 정보를 정확하게 추출합니다.\n\n" +
        $"플랫폼 유형: {sourceType}\n\n" +
        "반드시 아래 JSON 형식으로만 응답하세요 (다른 텍스트 없이):\n" +
        "```json\n" +
        "{\n" +
        "  \"shopName\": \"쇼핑몰 이름\",\n" +
        "  \"logoUrl\": \"로고 이미지 URL 또는 null\",\n" +
        "  \"categories\": [{\"name\": \"카테고리명\", \"parentName\": null, \"productCount\": 0}],\n" +
        "  \"products\": [{\n" +
        "    \"name\": \"상품명\", \"description\": \"상품 설명 (200자 이내)\",\n" +
        "    \"price\": 10000, \"salePrice\": null,\n" +
        "    \"categoryName\": \"소속 카테고리명\",\n" +
        "    \"imageUrl\": \"상품 이미지 URL\", \"sku\": null,\n" +
        "    \"attributes\": {\"색상\": \"블랙\"}\n" +
        "  }]\n}\n```\n\n" +
        "규칙:\n" +
        "1. 가격은 반드시 숫자로 (원 단위, 콤마 제거). 가격을 찾을 수 없으면 0\n" +
        "2. 이미지 URL은 절대 경로로 변환\n" +
        "3. 카테고리는 네비게이션 메뉴에서 추출\n" +
        "4. 상품은 최대 50개까지 추출\n" +
        "5. HTML에 상품이 없으면 빈 배열 반환\n" +
        "6. 설명에서 HTML 태그 제거\n" +
        $"7. {sourceType} 플랫폼의 특수한 HTML 구조를 고려하여 분석";

    private static string GetCorrectionPrompt(string sourceType) =>
        "이전 분석에서 오류가 발생했습니다. 더 신중하게 다시 분석해주세요.\n\n" +
        $"플랫폼: {sourceType}\n\n" +
        "주의사항:\n" +
        "1. JSON 형식이 정확해야 합니다 (구문 오류 없이)\n" +
        "2. 가격에 문자가 포함되면 안 됩니다 (숫자만)\n" +
        "3. URL이 상대 경로면 기본 도메인을 붙여주세요\n" +
        "4. 상품을 찾을 수 없으면 빈 배열을 반환하세요\n" +
        "5. 반드시 ```json ... ``` 블록 안에만 JSON을 작성하세요\n\n" +
        GetExtractionPrompt(sourceType);

    private MigrationPreviewDto? ParseAiResponse(string baseUrl, string sourceType, string aiText)
    {
        try
        {
            // Extract JSON from response (may be wrapped in ```json ... ```)
            var jsonMatch = Regex.Match(aiText, @"```json\s*([\s\S]*?)\s*```", RegexOptions.IgnoreCase);
            var jsonStr = jsonMatch.Success ? jsonMatch.Groups[1].Value : aiText.Trim();

            // Try to fix common JSON issues
            jsonStr = jsonStr.Trim();
            if (!jsonStr.StartsWith("{")) jsonStr = jsonStr[jsonStr.IndexOf('{')..];
            if (!jsonStr.EndsWith("}")) jsonStr = jsonStr[..(jsonStr.LastIndexOf('}') + 1)];

            using var doc = JsonDocument.Parse(jsonStr);
            var root = doc.RootElement;

            var shopName = root.TryGetProperty("shopName", out var sn) ? sn.GetString() ?? "Shop" : "Shop";
            var logoUrl = root.TryGetProperty("logoUrl", out var lu) ? lu.GetString() : null;

            var categories = new List<CrawledCategory>();
            if (root.TryGetProperty("categories", out var cats))
            {
                foreach (var cat in cats.EnumerateArray())
                {
                    var name = cat.TryGetProperty("name", out var cn) ? cn.GetString() ?? "" : "";
                    var parent = cat.TryGetProperty("parentName", out var pn) ? pn.GetString() : null;
                    var count = cat.TryGetProperty("productCount", out var pc) ? pc.TryGetInt32(out var pcv) ? pcv : 0 : 0;
                    if (!string.IsNullOrEmpty(name))
                        categories.Add(new CrawledCategory(name, parent, count));
                }
            }

            var products = new List<CrawledProduct>();
            var totalImages = 0;
            if (root.TryGetProperty("products", out var prods))
            {
                foreach (var prod in prods.EnumerateArray())
                {
                    try
                    {
                        var name = prod.TryGetProperty("name", out var pn) ? pn.GetString() ?? "" : "";
                        if (string.IsNullOrEmpty(name)) continue;

                        var desc = prod.TryGetProperty("description", out var pd) ? pd.GetString() : null;
                        var price = ParsePrice(prod, "price");
                        var salePrice = ParseNullablePrice(prod, "salePrice");
                        var catName = prod.TryGetProperty("categoryName", out var pcn) ? pcn.GetString() ?? "전체상품" : "전체상품";
                        var imgUrl = prod.TryGetProperty("imageUrl", out var piu) ? piu.GetString() : null;
                        var sku = prod.TryGetProperty("sku", out var ps) ? ps.GetString() : null;

                        // Resolve relative image URLs
                        if (imgUrl != null && !imgUrl.StartsWith("http"))
                            imgUrl = ResolveUrl(baseUrl, imgUrl);

                        Dictionary<string, string>? attrs = null;
                        if (prod.TryGetProperty("attributes", out var pa) && pa.ValueKind == JsonValueKind.Object)
                        {
                            attrs = new Dictionary<string, string>();
                            foreach (var attr in pa.EnumerateObject())
                                attrs[attr.Name] = attr.Value.GetString() ?? "";
                        }

                        products.Add(new CrawledProduct(name, desc, price, salePrice, catName, imgUrl, sku, attrs));
                        if (imgUrl != null) totalImages++;
                    }
                    catch { /* Skip malformed product entries */ }
                }
            }

            if (categories.Count == 0)
                categories.Add(new CrawledCategory("전체상품", null, products.Count));

            _logger.LogInformation("AI parsed: {Shop}, {Products} products, {Categories} categories", shopName, products.Count, categories.Count);
            return new MigrationPreviewDto(baseUrl, sourceType, shopName, categories, products, totalImages, logoUrl);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to parse AI response for {Url}", baseUrl);
            return null;
        }
    }

    private static decimal ParsePrice(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var val)) return 0;
        if (val.ValueKind == JsonValueKind.Number) return val.GetDecimal();
        if (val.ValueKind == JsonValueKind.String)
        {
            var str = Regex.Replace(val.GetString() ?? "", @"[^\d.]", "");
            return decimal.TryParse(str, out var p) ? p : 0;
        }
        return 0;
    }

    private static decimal? ParseNullablePrice(JsonElement el, string prop)
    {
        if (!el.TryGetProperty(prop, out var val)) return null;
        if (val.ValueKind == JsonValueKind.Null) return null;
        var p = ParsePrice(el, prop);
        return p > 0 ? p : null;
    }

    private static string TruncateHtml(string html, int maxLength)
    {
        // Remove scripts, styles, comments to save space
        var clean = Regex.Replace(html, @"<script[^>]*>[\s\S]*?</script>", "", RegexOptions.IgnoreCase);
        clean = Regex.Replace(clean, @"<style[^>]*>[\s\S]*?</style>", "", RegexOptions.IgnoreCase);
        clean = Regex.Replace(clean, @"<!--[\s\S]*?-->", "");
        clean = Regex.Replace(clean, @"\s+", " ");
        return clean.Length > maxLength ? clean[..maxLength] : clean;
    }

    /// <summary>Basic fallback extraction when AI is unavailable</summary>
    private MigrationPreviewDto FallbackBasicExtraction(string baseUrl, string html, string sourceType)
    {
        var categories = new List<CrawledCategory>();
        var products = new List<CrawledProduct>();
        var totalImages = 0;

        var titleMatch = Regex.Match(html, @"<title[^>]*>([^<]+)</title>", RegexOptions.IgnoreCase);
        var shopName = titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : new Uri(baseUrl).Host;

        // Extract JSON-LD structured data (works across all platforms)
        var jsonLdMatches = Regex.Matches(html, @"<script[^>]*type=""application/ld\+json""[^>]*>([\s\S]*?)</script>", RegexOptions.IgnoreCase);
        foreach (Match m in jsonLdMatches)
        {
            try
            {
                using var doc = JsonDocument.Parse(m.Groups[1].Value.Trim());
                var root = doc.RootElement;
                if (root.TryGetProperty("@type", out var t) && t.GetString() == "Product")
                {
                    var name = root.TryGetProperty("name", out var n) ? n.GetString() ?? "" : "";
                    var desc = root.TryGetProperty("description", out var d) ? d.GetString() : null;
                    decimal price = 0;
                    if (root.TryGetProperty("offers", out var offers) && offers.TryGetProperty("price", out var p))
                        decimal.TryParse(p.ToString(), out price);
                    var img = root.TryGetProperty("image", out var i) ? (i.ValueKind == JsonValueKind.String ? i.GetString() : null) : null;

                    if (!string.IsNullOrEmpty(name))
                    {
                        products.Add(new CrawledProduct(name, desc, price, null, "전체상품", img, null, null));
                        if (img != null) totalImages++;
                    }
                }
            }
            catch { }
        }

        if (categories.Count == 0)
            categories.Add(new CrawledCategory("전체상품", null, products.Count));

        return new MigrationPreviewDto(baseUrl, sourceType, shopName, categories, products, totalImages, null);
    }

    private async Task ImportToTenantAsync(MigrationJob job, int tenantId, MigrationPreviewDto preview, CancellationToken ct)
    {
        job.Status = "Importing";
        job.ProgressPercent = 60;
        await _db.SaveChangesAsync(ct);

        var importLog = new List<string>();
        var categoryMap = new Dictionary<string, int>();

        // Import categories
        foreach (var cat in preview.Categories)
        {
            try
            {
                var slug = Slugify(cat.Name);
                var existing = await _db.Categories.FirstOrDefaultAsync(c => c.TenantId == tenantId && c.Slug == slug, ct);
                if (existing != null)
                {
                    categoryMap[cat.Name] = existing.Id;
                    continue;
                }

                var category = new Category
                {
                    TenantId = tenantId,
                    Name = cat.Name,
                    Slug = slug,
                    IsActive = true,
                    SortOrder = categoryMap.Count + 1,
                    CreatedBy = "migration"
                };
                _db.Categories.Add(category);
                await _db.SaveChangesAsync(ct);
                categoryMap[cat.Name] = category.Id;
                job.CategoriesImported++;
                importLog.Add($"Category: {cat.Name}");
            }
            catch (Exception ex)
            {
                job.FailedItems++;
                importLog.Add($"Category FAILED: {cat.Name} - {ex.Message}");
            }
        }

        // Import products
        var total = preview.Products.Count;
        for (var i = 0; i < total; i++)
        {
            var prod = preview.Products[i];
            try
            {
                var slug = Slugify(prod.Name);
                var existing = await _db.Products.AnyAsync(p => p.TenantId == tenantId && p.Slug == slug, ct);
                if (existing) { job.ProductsImported++; continue; }

                var categoryId = categoryMap.GetValueOrDefault(prod.CategoryName, categoryMap.Values.FirstOrDefault());

                var product = new Product
                {
                    TenantId = tenantId,
                    Name = prod.Name,
                    Slug = slug,
                    Description = prod.Description,
                    Price = prod.Price > 0 ? prod.Price : 10000,
                    SalePrice = prod.SalePrice,
                    PriceType = "Fixed",
                    CategoryId = categoryId,
                    IsActive = true,
                    IsNew = true,
                    CreatedBy = "migration"
                };
                _db.Products.Add(product);
                await _db.SaveChangesAsync(ct);

                // Download and save product image
                if (!string.IsNullOrEmpty(prod.ImageUrl))
                {
                    try
                    {
                        var imageData = await _httpClient.GetByteArrayAsync(prod.ImageUrl, ct);
                        var ext = Path.GetExtension(new Uri(prod.ImageUrl).AbsolutePath);
                        if (string.IsNullOrEmpty(ext)) ext = ".jpg";
                        var fileName = $"migrated-{product.Id}{ext}";

                        // Save to local uploads
                        var uploadDir = Path.Combine(AppContext.BaseDirectory, "wwwroot", "uploads", "products");
                        Directory.CreateDirectory(uploadDir);
                        var filePath = Path.Combine(uploadDir, fileName);
                        await File.WriteAllBytesAsync(filePath, imageData, ct);

                        _db.ProductImages.Add(new ProductImage
                        {
                            TenantId = tenantId,
                            ProductId = product.Id,
                            Url = $"/uploads/products/{fileName}",
                            SortOrder = 1,
                            CreatedBy = "migration"
                        });
                        job.ImagesImported++;
                    }
                    catch { /* Image download failure is non-critical */ }
                }

                job.ProductsImported++;
                job.ProgressPercent = 60 + (decimal)(i + 1) / total * 40;
                if (i % 5 == 0) await _db.SaveChangesAsync(ct);

                importLog.Add($"Product: {prod.Name} (₩{prod.Price:N0})");
            }
            catch (Exception ex)
            {
                job.FailedItems++;
                importLog.Add($"Product FAILED: {prod.Name} - {ex.Message}");
            }
        }

        job.Status = "Completed";
        job.ProgressPercent = 100;
        job.CompletedAt = DateTime.UtcNow;
        job.ImportLogJson = JsonSerializer.Serialize(importLog);
        await _db.SaveChangesAsync(ct);

        _logger.LogInformation("Migration completed for tenant {TenantId}: {Products} products, {Categories} categories, {Images} images imported from {Url}",
            tenantId, job.ProductsImported, job.CategoriesImported, job.ImagesImported, job.SourceUrl);
    }

    private static string DetectSourceType(string url, string fallback)
    {
        var host = new Uri(url).Host.ToLower();
        if (host.Contains("cafe24")) return "Cafe24";
        if (host.Contains("makeshop")) return "MakeShop";
        if (host.Contains("shopify") || host.Contains("myshopify")) return "Shopify";
        if (host.Contains("godo") || host.Contains("godomall")) return "GodoMall";
        if (host.Contains("sixshop")) return "SixShop";
        return fallback;
    }

    private static string Slugify(string text)
    {
        var slug = text.ToLower().Trim();
        slug = Regex.Replace(slug, @"[^\w가-힣\s-]", "");
        slug = Regex.Replace(slug, @"[\s]+", "-");
        slug = Regex.Replace(slug, @"-+", "-").Trim('-');
        if (slug.Length > 50) slug = slug[..50];
        if (string.IsNullOrEmpty(slug)) slug = $"item-{Guid.NewGuid().ToString()[..8]}";
        return slug;
    }

    private static string ResolveUrl(string baseUrl, string relative)
    {
        if (relative.StartsWith("http")) return relative;
        if (relative.StartsWith("//")) return "https:" + relative;
        var uri = new Uri(new Uri(baseUrl), relative);
        return uri.ToString();
    }

    private static MigrationJobDto ToDto(MigrationJob j) => new(
        j.Id, j.SourceUrl, j.SourceType, j.Status,
        j.TotalProductsFound, j.TotalCategoriesFound, j.TotalImagesFound,
        j.ProductsImported, j.CategoriesImported, j.ImagesImported,
        j.FailedItems, j.ProgressPercent,
        j.StartedAt, j.CompletedAt, j.ErrorMessage);
}
