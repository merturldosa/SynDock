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
}

public class ThemeConfig
{
    public string Primary { get; set; } = "#3B82F6";
    public string Secondary { get; set; } = "#1E40AF";
    public string Background { get; set; } = "#FFFFFF";
    public string? LogoUrl { get; set; }
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
