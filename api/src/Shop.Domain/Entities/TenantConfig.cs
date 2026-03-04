namespace Shop.Domain.Entities;

public class TenantConfig
{
    public ThemeConfig? Theme { get; set; }
    public string[] EnabledFeatures { get; set; } = [];
    public Dictionary<string, object> Settings { get; set; } = new();
    public Dictionary<string, SeasonalThemeConfig>? SeasonalThemes { get; set; }
    public string[]? ReactionTypes { get; set; }
    public ChatPersonaConfig? ChatPersona { get; set; }
    public PromoBannerConfig? PromoBanner { get; set; }

    // Shipping & Delivery
    public ShippingConfig? Shipping { get; set; }

    // SEO & Social
    public SeoConfig? Seo { get; set; }
    public SocialLinksConfig? SocialLinks { get; set; }

    // Onboarding
    public OnboardingConfig? Onboarding { get; set; }

    // Auto Coupon
    public AutoCouponConfig? AutoCoupon { get; set; }
}

public class ThemeConfig
{
    public string Primary { get; set; } = "#3B82F6";
    public string PrimaryLight { get; set; } = "#DBEAFE";
    public string Secondary { get; set; } = "#1E40AF";
    public string SecondaryLight { get; set; } = "#F0F4F8";
    public string Background { get; set; } = "#FFFFFF";
    public string? LogoUrl { get; set; }
    public string? FaviconUrl { get; set; }
    public string FontFamily { get; set; } = "Pretendard, sans-serif";
}

public class SeasonalThemeConfig
{
    public string? Primary { get; set; }
    public string? Secondary { get; set; }
    public string? Background { get; set; }
}

public class ChatPersonaConfig
{
    public string Name { get; set; } = "AI 쇼핑 어시스턴트";
    public string? Greeting { get; set; }
    public string? SystemPrompt { get; set; }
}

public class PromoBannerConfig
{
    public string? Title { get; set; }
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public string? LinkUrl { get; set; }
    public string? BackgroundColor { get; set; }
    public bool IsActive { get; set; }
}

public class ShippingConfig
{
    public decimal FreeShippingThreshold { get; set; } = 50000;
    public decimal DefaultShippingFee { get; set; } = 3000;
    public string? ShippingPolicy { get; set; }
    public string? ReturnPolicy { get; set; }
    public int EstimatedDeliveryDays { get; set; } = 3;
    public string[]? AvailableCarriers { get; set; }
}

public class SeoConfig
{
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
    public string? OgImageUrl { get; set; }
    public string? GoogleAnalyticsId { get; set; }
    public string? NaverAnalyticsId { get; set; }
}

public class SocialLinksConfig
{
    public string? Instagram { get; set; }
    public string? Facebook { get; set; }
    public string? Youtube { get; set; }
    public string? Blog { get; set; }
    public string? KakaoChannel { get; set; }
}

public class OnboardingConfig
{
    public bool IsCompleted { get; set; }
    public string? TemplateType { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string[] CompletedSteps { get; set; } = [];
}

public class AutoCouponConfig
{
    public bool WelcomeCouponEnabled { get; set; }
    public string WelcomeCouponCode { get; set; } = "WELCOME";
    public bool BirthdayCouponEnabled { get; set; }
    public string BirthdayCouponCode { get; set; } = "BIRTHDAY";
}
