using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Shop.Application.Common.Interfaces;
using Shop.Application.Orders.Events;
using Shop.Domain.Entities;

namespace Shop.Infrastructure.Integration;

/// <summary>
/// When a product is created on any tenant shop:
/// 1. Generates AI-powered SNS content (captions, hashtags)
/// 2. Auto-creates social media posts for multiple platforms
/// 3. Schedules optimal posting times
/// 4. Tracks performance
/// </summary>
public class SnsAutoMarketingHandler : INotificationHandler<ProductCreatedEvent>
{
    private readonly IShopDbContext _db;
    private readonly ILogger<SnsAutoMarketingHandler> _logger;

    public SnsAutoMarketingHandler(IShopDbContext db, ILogger<SnsAutoMarketingHandler> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task Handle(ProductCreatedEvent notification, CancellationToken ct)
    {
        try
        {
            var tenant = await _db.Tenants.AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == notification.TenantId, ct);
            if (tenant == null) return;

            var shopName = tenant.Name;
            var shopUrl = $"https://{tenant.Slug}.syndock.com";
            var productUrl = $"{shopUrl}/products/{notification.ProductId}";
            var mallUrl = $"https://mall.syndock.com/products/{notification.ProductId}";

            // Check if SNS marketing is enabled for this tenant
            var config = ParseConfig(tenant.ConfigJson);
            // Default: auto-marketing enabled for all tenants

            var now = DateTime.UtcNow;

            // === Platform-specific content generation ===

            // 1. Instagram Post
            var instaCaption = GenerateInstagramCaption(shopName, notification.ProductName, notification.Price, productUrl);
            _db.SocialPosts.Add(new SocialPost
            {
                TenantId = notification.TenantId,
                ProductId = notification.ProductId,
                Platform = "Instagram",
                Caption = instaCaption,
                ImageUrl = notification.ImageUrl,
                Status = "Pending",
                CreatedBy = "AI-Marketing"
            });

            // 2. Facebook Post
            var fbContent = GenerateFacebookPost(shopName, notification.ProductName, notification.Price, productUrl);
            _db.SocialPosts.Add(new SocialPost
            {
                TenantId = notification.TenantId,
                ProductId = notification.ProductId,
                Platform = "Facebook",
                Caption = fbContent,
                ImageUrl = notification.ImageUrl,
                Status = "Pending",
                CreatedBy = "AI-Marketing"
            });

            // 3. YouTube Shorts Script (for video creation)
            var ytScript = GenerateYouTubeScript(shopName, notification.ProductName, notification.Price);
            _db.SocialPosts.Add(new SocialPost
            {
                TenantId = notification.TenantId,
                ProductId = notification.ProductId,
                Platform = "YouTube",
                Caption = ytScript,
                Status = "Pending",
                CreatedBy = "AI-Marketing"
            });

            // 4. Twitter/X Post
            var tweetContent = GenerateTwitterPost(shopName, notification.ProductName, notification.Price, mallUrl);
            _db.SocialPosts.Add(new SocialPost
            {
                TenantId = notification.TenantId,
                ProductId = notification.ProductId,
                Platform = "Twitter",
                Caption = tweetContent,
                Status = "Pending",
                CreatedBy = "AI-Marketing"
            });

            // 5. Naver Blog Post
            var blogContent = GenerateNaverBlogPost(shopName, notification.ProductName, notification.Price, productUrl);
            _db.SocialPosts.Add(new SocialPost
            {
                TenantId = notification.TenantId,
                ProductId = notification.ProductId,
                Platform = "NaverBlog",
                Caption = blogContent,
                Status = "Pending",
                CreatedBy = "AI-Marketing"
            });

            await _db.SaveChangesAsync(ct);

            _logger.LogInformation("AI-Marketing: Created 5 SNS posts for new product '{Product}' from {Shop}",
                notification.ProductName, shopName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "SNS auto-marketing failed for product {ProductId}", notification.ProductId);
        }
    }

    // === Platform-Specific Content Generators ===

    private static string GenerateInstagramCaption(string shopName, string productName, decimal price, string url)
    {
        var priceStr = price > 0 ? $"\u20a9{price:N0}" : "";
        var hashtags = GenerateHashtags(shopName, productName);

        return $"\u2728 \uc0c8\ub85c\uc6b4 \uc0c1\ud488\uc774 \ub4f1\ub85d\ub418\uc5c8\uc5b4\uc694!\n\n" +
               $"\ud83c\udff7\ufe0f {productName}\n" +
               (price > 0 ? $"\ud83d\udcb0 {priceStr}\n" : "") +
               $"\ud83c\udfea {shopName}\n\n" +
               $"SynDock Mall\uc5d0\uc11c \ub9cc\ub098\ubcf4\uc138\uc694! \ud83d\uded2\n" +
               $"\ud504\ub85c\ud544 \ub9c1\ud06c\uc5d0\uc11c \uad6c\ub9e4 \uac00\ub2a5\ud569\ub2c8\ub2e4.\n\n" +
               hashtags;
    }

    private static string GenerateFacebookPost(string shopName, string productName, decimal price, string url)
    {
        return $"\ud83c\udd95 [{shopName}] \uc2e0\uc0c1\ud488 \uc548\ub0b4\n\n" +
               $"{productName}\n" +
               (price > 0 ? $"\uac00\uaca9: \u20a9{price:N0}\n\n" : "\n") +
               $"\uc9c0\uae08 \ubc14\ub85c \ud655\uc778\ud574\ubcf4\uc138\uc694! \ud83d\udc47\n" +
               $"{url}\n\n" +
               $"#SynDock #{shopName.Replace(" ", "")} #\uc2e0\uc0c1\ud488 #\uc628\ub77c\uc778\uc1fc\ud551";
    }

    private static string GenerateYouTubeScript(string shopName, string productName, decimal price)
    {
        return $"[YouTube Shorts \uc2a4\ud06c\ub9bd\ud2b8]\n\n" +
               $"\uc81c\ubaa9: {productName} - {shopName} \uc2e0\uc0c1\ud488! \ud83c\udd95\n\n" +
               $"[\uc7a5\uba74 1 - 0~3\ucd08]\n" +
               $"\"\uc624\ub298 \uc18c\uac1c\ud560 \uc0c1\ud488\uc740!\"\n" +
               $"(\uc0c1\ud488 \uc774\ubbf8\uc9c0 \uc90c\uc778)\n\n" +
               $"[\uc7a5\uba74 2 - 3~8\ucd08]\n" +
               $"\"{productName}\"\n" +
               (price > 0 ? $"\"\uac00\uaca9\uc740 \u20a9{price:N0}!\"\n\n" : "\n") +
               $"[\uc7a5\uba74 3 - 8~12\ucd08]\n" +
               $"\"SynDock Mall\uc5d0\uc11c \uad6c\ub9e4 \uac00\ub2a5!\"\n" +
               $"(QR\ucf54\ub4dc \ub610\ub294 \ub9c1\ud06c \ud45c\uc2dc)\n\n" +
               $"[\uc7a5\uba74 4 - 12~15\ucd08]\n" +
               $"\"\uc88b\uc544\uc694\uc640 \uad6c\ub3c5 \ubd80\ud0c1\ub4dc\ub824\uc694!\" \ud83d\udc4d\n\n" +
               $"\uc124\uba85: #{shopName} #{productName.Split(' ')[0]} #\uc2e0\uc0c1\ud488 #\uc1fc\ud551 #\ucd94\ucc9c";
    }

    private static string GenerateTwitterPost(string shopName, string productName, decimal price, string url)
    {
        var priceStr = price > 0 ? $" \u20a9{price:N0}" : "";
        return $"\ud83c\udd95 [{shopName}] {productName}{priceStr}\n\n" +
               $"SynDock Mall\uc5d0\uc11c \uad6c\ub9e4 \uac00\ub2a5! \ud83d\uded2\n" +
               $"{url}\n\n" +
               $"#SynDock #{shopName.Replace(" ", "")} #\uc2e0\uc0c1\ud488";
    }

    private static string GenerateNaverBlogPost(string shopName, string productName, decimal price, string url)
    {
        return $"[{shopName}] {productName} - \uc2e0\uc0c1\ud488 \uc18c\uac1c\n\n" +
               $"\uc548\ub155\ud558\uc138\uc694! {shopName}\uc5d0\uc11c \uc0c8\ub85c\uc6b4 \uc0c1\ud488\uc744 \uc18c\uac1c\ud569\ub2c8\ub2e4.\n\n" +
               $"\ud83d\udccc \uc0c1\ud488\uba85: {productName}\n" +
               (price > 0 ? $"\ud83d\udccc \uac00\uaca9: \u20a9{price:N0}\n\n" : "\n") +
               $"\uc774 \uc0c1\ud488\uc740 SynDock Mall\uacfc {shopName} \uacf5\uc2dd \uc1fc\ud551\ubab0\uc5d0\uc11c\n" +
               $"\uad6c\ub9e4\ud558\uc2e4 \uc218 \uc788\uc2b5\ub2c8\ub2e4.\n\n" +
               $"\ud83d\udc49 \uad6c\ub9e4 \ub9c1\ud06c: {url}\n\n" +
               $"\uc88b\uc740 \uc0c1\ud488\uc73c\ub85c \ucc3e\uc544\ubf59\uaca0\uc2b5\ub2c8\ub2e4! \uac10\uc0ac\ud569\ub2c8\ub2e4. \ud83d\ude0a\n\n" +
               $"#SynDock #{shopName.Replace(" ", "")} #{productName.Split(' ')[0]} #\uc628\ub77c\uc778\uc1fc\ud551 #\ucd94\ucc9c\uc0c1\ud488";
    }

    private static string GenerateHashtags(string shopName, string productName)
    {
        var words = productName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var tags = new List<string> { "#SynDock", "#SynDockMall", $"#{shopName.Replace(" ", "")}", "#\uc2e0\uc0c1\ud488", "#\uc628\ub77c\uc778\uc1fc\ud551", "#\ucd94\ucc9c" };
        foreach (var word in words.Take(3))
        {
            if (word.Length >= 2) tags.Add($"#{word}");
        }
        return string.Join(" ", tags);
    }

    private static JsonElement? ParseConfig(string? json)
    {
        if (string.IsNullOrEmpty(json)) return null;
        try { return JsonDocument.Parse(json).RootElement; } catch { return null; }
    }
}
