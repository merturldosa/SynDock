using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;

namespace Shop.Infrastructure.Services;

public class WebPushService : IWebPushService
{
    private readonly IShopDbContext _db;
    private readonly ILogger<WebPushService> _logger;
    private readonly HttpClient _httpClient;
    private readonly string? _vapidSubject;
    private readonly string? _vapidPublicKey;
    private readonly string? _vapidPrivateKey;

    public WebPushService(
        IShopDbContext db,
        ILogger<WebPushService> logger,
        IHttpClientFactory httpClientFactory,
        IConfiguration configuration)
    {
        _db = db;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient("WebPush");
        _vapidSubject = configuration["WebPush:Subject"];
        _vapidPublicKey = configuration["WebPush:PublicKey"];
        _vapidPrivateKey = configuration["WebPush:PrivateKey"];
    }

    public async Task SendPushAsync(int userId, string title, string message, string? url = null, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_vapidPublicKey))
        {
            _logger.LogDebug("WebPush VAPID keys not configured, skipping push");
            return;
        }

        var subscriptions = await _db.PushSubscriptions
            .AsNoTracking()
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync(ct);

        foreach (var sub in subscriptions)
        {
            await SendToSubscription(sub, title, message, url, ct);
        }
    }

    public async Task SendPushToAllAsync(int tenantId, string title, string message, string? url = null, CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_vapidPublicKey))
        {
            _logger.LogDebug("WebPush VAPID keys not configured, skipping push");
            return;
        }

        var subscriptions = await _db.PushSubscriptions
            .AsNoTracking()
            .Where(s => s.TenantId == tenantId && s.IsActive)
            .ToListAsync(ct);

        foreach (var sub in subscriptions)
        {
            await SendToSubscription(sub, title, message, url, ct);
        }
    }

    private async Task SendToSubscription(Domain.Entities.PushSubscription sub, string title, string message, string? url, CancellationToken ct = default)
    {
        try
        {
            var payload = JsonSerializer.Serialize(new
            {
                title,
                body = message,
                icon = "/icons/icon-192.svg",
                badge = "/icons/icon-192.svg",
                url = url ?? "/",
                timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });

            var request = new HttpRequestMessage(HttpMethod.Post, sub.Endpoint)
            {
                Content = new StringContent(payload, Encoding.UTF8, "application/json")
            };

            request.Headers.TryAddWithoutValidation("TTL", "86400");

            if (!string.IsNullOrEmpty(_vapidSubject))
            {
                request.Headers.TryAddWithoutValidation("Urgency", "normal");
            }

            var response = await _httpClient.SendAsync(request, ct);

            if (response.StatusCode == System.Net.HttpStatusCode.Gone ||
                response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                _logger.LogInformation("Push subscription expired, marking inactive: {Endpoint}", sub.Endpoint[..Math.Min(50, sub.Endpoint.Length)]);
                var entity = await ((Microsoft.EntityFrameworkCore.DbContext)(object)_db).Set<Domain.Entities.PushSubscription>()
                    .FirstOrDefaultAsync(s => s.Id == sub.Id, ct);
                if (entity != null)
                {
                    entity.IsActive = false;
                    await _db.SaveChangesAsync(ct);
                }
            }
            else if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Push failed for subscription {Id}: {Status}", sub.Id, response.StatusCode);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send push to subscription {Id}", sub.Id);
        }
    }
}
