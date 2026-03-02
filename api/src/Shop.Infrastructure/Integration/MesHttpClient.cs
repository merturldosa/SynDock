using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Domain.Interfaces;

namespace Shop.Infrastructure.Integration;

public class MesHttpClient : IMesClient
{
    private readonly HttpClient _httpClient;
    private readonly IDistributedCache _cache;
    private readonly IConfiguration _configuration;
    private readonly ILogger<MesHttpClient> _logger;
    private readonly bool _enabled;

    private const string TokenCacheKey = "mes:jwt:accessToken";
    private static readonly TimeSpan TokenCacheDuration = TimeSpan.FromMinutes(50);
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public MesHttpClient(
        HttpClient httpClient,
        IDistributedCache cache,
        ITenantContext tenantContext,
        IConfiguration configuration,
        ILogger<MesHttpClient> logger)
    {
        _httpClient = httpClient;
        _cache = cache;
        _configuration = configuration;
        _logger = logger;

        _enabled = configuration.GetValue<bool>("Mes:Enabled");
        if (_enabled)
        {
            var baseUrl = configuration["Mes:BaseUrl"] ?? "http://localhost:8080";
            var timeout = configuration.GetValue<int>("Mes:TimeoutSeconds", 10);

            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(timeout);
        }
    }

    public async Task<bool> IsAvailableAsync(CancellationToken ct = default)
    {
        if (!_enabled) return false;

        try
        {
            var response = await _httpClient.GetAsync("/api/health", ct);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<MesInventoryItem>> GetInventoryAsync(CancellationToken ct = default)
    {
        if (!_enabled) return [];

        try
        {
            var response = await SendAuthenticatedAsync(HttpMethod.Get, "/api/inventory", null, ct);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("MES inventory request failed: {StatusCode}", response.StatusCode);
                return [];
            }

            var result = await response.Content.ReadFromJsonAsync<MesApiResponse<List<MesInventoryResponse>>>(JsonOptions, ct);
            if (result?.Data is null) return [];

            return result.Data.Select(r => new MesInventoryItem(
                r.ProductCode ?? "",
                r.ProductName,
                r.AvailableQuantity,
                (int)(r.ReservedQuantity ?? 0),
                r.WarehouseCode,
                r.WarehouseName,
                r.LastTransactionDate
            )).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get MES inventory");
            return [];
        }
    }

    public async Task<MesSalesOrderResult> CreateSalesOrderAsync(MesSalesOrderRequest request, CancellationToken ct = default)
    {
        if (!_enabled)
            return new MesSalesOrderResult(false, null, "MES integration is disabled");

        try
        {
            // Create order (status: DRAFT)
            var response = await SendAuthenticatedAsync(HttpMethod.Post, "/api/sales-orders", request, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("MES order creation failed: {StatusCode} {Body}", response.StatusCode, body);
                return new MesSalesOrderResult(false, null, $"HTTP {response.StatusCode}");
            }

            var result = JsonSerializer.Deserialize<MesApiResponse<MesOrderCreated>>(body, JsonOptions);
            var orderId = result?.Data?.SalesOrderId;
            if (orderId is null)
                return new MesSalesOrderResult(false, null, "MES returned no order ID");

            // Confirm order (DRAFT → CONFIRMED)
            var confirmResponse = await SendAuthenticatedAsync(HttpMethod.Post, $"/api/sales-orders/{orderId}/confirm", null, ct);
            if (!confirmResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("MES order confirm failed for {OrderId}: {StatusCode}",
                    orderId, confirmResponse.StatusCode);
                // Order was created but not confirmed — still return success with warning
                return new MesSalesOrderResult(true, orderId.ToString(), "주문 생성됨, 확정 실패 (수동 확정 필요)");
            }

            return new MesSalesOrderResult(true, orderId.ToString(), null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create MES sales order: {OrderNo}", request.OrderNo);
            return new MesSalesOrderResult(false, null, ex.Message);
        }
    }

    public async Task<MesSyncStatus> GetSyncStatusAsync(CancellationToken ct = default)
    {
        if (!_enabled)
            return new MesSyncStatus(false, null, 0, "MES integration is disabled");

        var isConnected = await IsAvailableAsync(ct);
        var lastSyncStr = await _cache.GetStringAsync("mes:lastSync", ct);
        var lastSync = lastSyncStr is not null ? DateTime.Parse(lastSyncStr) : (DateTime?)null;
        var syncedCountStr = await _cache.GetStringAsync("mes:syncedCount", ct);
        var syncedCount = syncedCountStr is not null ? int.Parse(syncedCountStr) : 0;

        return new MesSyncStatus(isConnected, lastSync, syncedCount, isConnected ? null : "MES 서버에 연결할 수 없습니다");
    }

    // ── JWT Authentication ──────────────────────────────────

    private async Task<HttpResponseMessage> SendAuthenticatedAsync(
        HttpMethod method, string url, object? body, CancellationToken ct)
    {
        var token = await EnsureAuthenticatedAsync(ct);
        var response = await SendWithTokenAsync(method, url, body, token, ct);

        // 401 → refresh token and retry once
        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            _logger.LogInformation("MES token expired, refreshing...");
            await _cache.RemoveAsync(TokenCacheKey, ct);
            token = await EnsureAuthenticatedAsync(ct);
            response = await SendWithTokenAsync(method, url, body, token, ct);
        }

        return response;
    }

    private async Task<HttpResponseMessage> SendWithTokenAsync(
        HttpMethod method, string url, object? body, string token, CancellationToken ct)
    {
        using var request = new HttpRequestMessage(method, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Add("X-Tenant-ID", _configuration["Mes:TenantId"] ?? "smartdocking");

        if (body is not null)
            request.Content = JsonContent.Create(body, options: JsonOptions);

        return await _httpClient.SendAsync(request, ct);
    }

    private async Task<string> EnsureAuthenticatedAsync(CancellationToken ct)
    {
        var cached = await _cache.GetStringAsync(TokenCacheKey, ct);
        if (!string.IsNullOrEmpty(cached))
            return cached;

        var loginRequest = new
        {
            tenantId = _configuration["Mes:TenantId"] ?? "smartdocking",
            username = _configuration["Mes:Username"] ?? "admin",
            password = _configuration["Mes:Password"] ?? "admin123"
        };

        var response = await _httpClient.PostAsJsonAsync("/api/auth/login", loginRequest, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogError("MES login failed: {StatusCode} {Body}", response.StatusCode, body);
            throw new InvalidOperationException($"MES login failed: HTTP {response.StatusCode}");
        }

        var result = JsonSerializer.Deserialize<MesApiResponse<MesLoginData>>(body, JsonOptions);
        var token = result?.Data?.AccessToken
            ?? throw new InvalidOperationException("MES login response missing accessToken");

        await _cache.SetStringAsync(TokenCacheKey, token,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TokenCacheDuration }, ct);

        _logger.LogInformation("MES JWT token obtained and cached");
        return token;
    }

    // ── MES Response DTOs ───────────────────────────────────

    private record MesApiResponse<T>(
        [property: JsonPropertyName("data")] T? Data,
        [property: JsonPropertyName("success")] bool Success,
        [property: JsonPropertyName("message")] string? Message,
        [property: JsonPropertyName("timestamp")] string? Timestamp);

    private record MesLoginData(
        [property: JsonPropertyName("accessToken")] string? AccessToken,
        [property: JsonPropertyName("refreshToken")] string? RefreshToken,
        [property: JsonPropertyName("tokenType")] string? TokenType,
        [property: JsonPropertyName("expiresIn")] int? ExpiresIn);

    private record MesInventoryResponse(
        [property: JsonPropertyName("productId")] long? ProductId,
        [property: JsonPropertyName("productCode")] string? ProductCode,
        [property: JsonPropertyName("productName")] string? ProductName,
        [property: JsonPropertyName("availableQuantity")] decimal AvailableQuantity,
        [property: JsonPropertyName("reservedQuantity")] decimal? ReservedQuantity,
        [property: JsonPropertyName("warehouseCode")] string? WarehouseCode,
        [property: JsonPropertyName("warehouseName")] string? WarehouseName,
        [property: JsonPropertyName("lastTransactionDate")] DateTime? LastTransactionDate);

    private record MesOrderCreated(
        [property: JsonPropertyName("salesOrderId")] long? SalesOrderId,
        [property: JsonPropertyName("orderNo")] string? OrderNo,
        [property: JsonPropertyName("status")] string? Status);
}
