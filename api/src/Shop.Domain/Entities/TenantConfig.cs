namespace Shop.Domain.Entities;

public class TenantConfig
{
    public ThemeConfig? Theme { get; set; }
    public string[] EnabledFeatures { get; set; } = [];
    public Dictionary<string, object> Settings { get; set; } = new();
}

public class ThemeConfig
{
    public string Primary { get; set; } = "#3B82F6";
    public string Secondary { get; set; } = "#1E40AF";
    public string Background { get; set; } = "#FFFFFF";
    public string? LogoUrl { get; set; }
    public string FontFamily { get; set; } = "Pretendard, sans-serif";
}
