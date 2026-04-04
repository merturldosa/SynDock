using Shop.Application.Platform.Commands;
using Shop.Domain.Entities;

namespace Shop.Infrastructure.Data;

public static class CatholiaSeedData
{
    public static TenantConfig GetConfig() => new()
    {
        Theme = new ThemeConfig
        {
            Primary = "#D4AF37",
            PrimaryLight = "#E8C766",
            Secondary = "#1B2A4A",
            SecondaryLight = "#2A3D66",
            Background = "#FAF8F5",
            LogoUrl = null,
            FaviconUrl = null,
            FontFamily = "Pretendard, sans-serif"
        },
        EnabledFeatures = ["liturgy", "saints", "baptismalName", "community", "aiChat", "collection", "wms", "crm", "erp", "scm", "mes"],
        ChatPersona = new ChatPersonaConfig
        {
            Name = "\uce74\ud1a8\ub9ac\uc544 AI \ub3c4\uc6b0\ubbf8",
            Greeting = "\uc548\ub155\ud558\uc138\uc694! \uac00\ud1a8\ub9ad \uc131\ubb3c\uacfc \uad00\ub828\ub41c \uad81\uae08\ud55c \uc810\uc744 \ubb3c\uc5b4\ubcf4\uc138\uc694.",
            SystemPrompt = "You are a helpful assistant for Catholia, a Catholic religious goods store. " +
                "You can help customers find rosaries, bibles, sacred art, liturgical items, prayer goods, and gifts. " +
                "You are knowledgeable about Catholic traditions, saints, liturgical seasons, and sacraments. " +
                "Always be respectful and warm. Answer in Korean unless the customer uses another language."
        },
        Shipping = new ShippingConfig
        {
            FreeShippingThreshold = 50000,
            DefaultShippingFee = 3000,
            EstimatedDeliveryDays = 3,
            ShippingPolicy = "\uc8fc\ubb38 \ud655\uc778 \ud6c4 3~7\uc77c \uc774\ub0b4 \ucd9c\uace0\ub429\ub2c8\ub2e4.",
            ReturnPolicy = "\uc0c1\ud488 \uc218\ub839 \ud6c4 7\uc77c \uc774\ub0b4 \ubc18\ud488 \uac00\ub2a5\ud569\ub2c8\ub2e4."
        },
        Seo = new SeoConfig
        {
            MetaTitle = "\uce74\ud1a8\ub9ac\uc544 - \uac00\ud1a8\ub9ad \uc131\ubb3c \uc804\ubb38 \uc1fc\ud551\ubab0",
            MetaDescription = "\ubb35\uc8fc, \uc131\uacbd, \uc131\ud654, \uc804\ub840\uc6a9\ud488 \ub4f1 \uac00\ud1a8\ub9ad \uc131\ubb3c\uc744 \ub9cc\ub098\ubcf4\uc138\uc694."
        },
        SocialLinks = new SocialLinksConfig
        {
            Instagram = "catholia_shop",
            KakaoChannel = "catholia"
        },
        Onboarding = new OnboardingConfig
        {
            IsCompleted = true,
            TemplateType = "catholia"
        },
        SeasonalThemes = new Dictionary<string, SeasonalThemeConfig>
        {
            ["advent"] = new()
            {
                Primary = "#7B2D8E",
                Secondary = "#4A1A5E",
                Background = "#F5F0F8"
            },
            ["lent"] = new()
            {
                Primary = "#8B5CF6",
                Secondary = "#6D28D9",
                Background = "#F5F3FF"
            },
            ["easter"] = new()
            {
                Primary = "#FFFFFF",
                Secondary = "#D4AF37",
                Background = "#FFFDF5"
            },
            ["christmas"] = new()
            {
                Primary = "#DC2626",
                Secondary = "#166534",
                Background = "#FEF2F2"
            }
        },
        PromoBanner = new PromoBannerConfig
        {
            Title = "\uc0c8 \uc2dc\uc98c \uc804\ub840\uc6a9\ud488 \uc785\uace0",
            Description = "\ub300\ub9bc\uc808 \ud2b9\ubcc4 \ucee8\ub809\uc158\uc744 \ub9cc\ub098\ubcf4\uc138\uc694",
            IsActive = true,
            BackgroundColor = "#1B2A4A"
        },
        Settings = new Dictionary<string, object>
        {
            ["companyName"] = "\uce74\ud1a8\ub9ac\uc544",
            ["companyAddress"] = "\uc11c\uc6b8\ud2b9\ubcc4\uc2dc \uc885\ub85c\uad6c \uba85\ub3d9\uae38 12",
            ["businessNumber"] = "123-45-67890",
            ["ceoName"] = "\uae40\uc694\uc169",
            ["contactPhone"] = "02-1234-5678",
            ["contactEmail"] = "contact@catholia.com",
            ["heroSubtitle"] = "\uc740\ucd1d \uac00\ub4dd\ud55c \uc131\ubb3c\uc744 \ub9cc\ub098\ubcf4\uc138\uc694",
            ["heroTagline"] = "CATHOLIA",
            ["heroDescription"] = "\uc815\uc131\uaecf \uc5c4\uc120\ud55c \uac00\ud1a8\ub9ad \uc131\ubb3c\ub85c\n\uc2e0\uc559\uc0dd\ud65c\uc744 \ub354\uc6b1 \ud48d\uc694\ub86d\uac8c"
        }
    };

    public static List<SeedCategoryDto> GetCategories() =>
    [
        new("\ubb35\uc8fc", "rosary", "\uac00\ud1a8\ub9ad \uc2e0\uc559\uc758 \uc0c1\uc9d5, \ub2e4\uc591\ud55c \ubb35\uc8fc", "\u271d\ufe0f", 1,
        [
            new("5\ub2e8\ubb35\uc8fc", "rosary-5", "\uc804\ud1b5 5\ub2e8 \ubb35\uc8fc", null, 1),
            new("\uc6d0\ud615\ubb35\uc8fc", "rosary-circle", "\ubcbd\uac78\uc774\uc6a9 \uc6d0\ud615 \ubb35\uc8fc", null, 2),
            new("\ubc18\uc9c0\ubb35\uc8fc", "rosary-ring", "\ud734\ub300\uc6a9 \ubc18\uc9c0\ud615 \ubb35\uc8fc", null, 3)
        ]),
        new("\uc131\uacbd/\uc11c\uc801", "bible", "\uc131\uacbd\uacfc \uc601\uc131 \uc11c\uc801", "\ud83d\udcd6", 2,
        [
            new("\uc131\uacbd", "bible-book", "\ud55c\uad6d\ucc9c\uc8fc\uad50 \uc8fc\uad50\ud68c\uc758 \uacf5\uc778 \uc131\uacbd", null, 1),
            new("\uc804\ub840\uc11c", "liturgy-book", "\ubbf8\uc0ac \uc804\ub840 \uad00\ub828 \uc11c\uc801", null, 2),
            new("\uc601\uc131\uc11c\uc801", "spiritual-book", "\uc131\uc778\uc804, \uc601\uc131 \uc548\ub0b4\uc11c", null, 3)
        ]),
        new("\uc131\ud654/\uc131\uc0c1", "sacred-art", "\uc131\ud654, \uc131\uc0c1, \uc2ed\uc790\uac00", "\ud83d\uddbc\ufe0f", 3,
        [
            new("\uc131\ud654", "painting", "\uc774\ucf58 \ubc0f \uc131\ud654 \uc791\ud488", null, 1),
            new("\uc131\uc0c1", "statue", "\uc131\ubaa8\uc0c1, \uc608\uc218\uc0c1 \ub4f1", null, 2),
            new("\uc2ed\uc790\uac00", "crucifix", "\ubcbd\uac78\uc774 \ubc0f \ud0c1\uc0c1\uc6a9 \uc2ed\uc790\uac00", null, 3)
        ]),
        new("\uc804\ub840\uc6a9\ud488", "liturgy-items", "\ubbf8\uc0ac\uc640 \uc804\ub840\uc5d0 \ud544\uc694\ud55c \uc6a9\ud488", "\u26ea", 4,
        [
            new("\uc81c\uc758/\ubcf5\uc7a5", "vestment", "\uc804\ub840\uc6a9 \uc81c\uc758\uc640 \ubcf5\uc7a5", null, 1),
            new("\uc804\ub840\uae30\uad6c", "liturgical-vessel", "\uc131\uc791, \uc131\ubc18 \ub4f1 \uc804\ub840 \uae30\uad6c", null, 2),
            new("\ucd2b\ub300", "candle", "\uc804\ub840\uc6a9 \ucd2b\ub300\uc640 \ucd08", null, 3)
        ]),
        new("\uae30\ub3c4\uc6a9\ud488", "prayer-items", "\uae30\ub3c4\uc640 \uc601\uc131\uc0dd\ud65c\uc744 \uc704\ud55c \uc6a9\ud488", "\ud83d\ude4f", 5,
        [
            new("\uc131\uc218/\uc131\uc720", "holy-water", "\uc131\uc218, \uc131\uc720 \ubc0f \uad00\ub828 \uc6a9\uae30", null, 1),
            new("\ud5a5/\ud5a5\ub85c", "incense", "\uc720\ud5a5, \ud5a5\ub85c \uc138\ud2b8", null, 2),
            new("\uae30\ub3c4\uc11c", "prayer-book", "\uc77c\uacfc \uae30\ub3c4, \uac1c\uc778 \uae30\ub3c4\uc11c", null, 3)
        ]),
        new("\uc120\ubb3c/\uae30\ub150\ud488", "gift", "\uc138\ub840, \ucd95\ubcf5 \uc120\ubb3c \ubc0f \uae30\ub150\ud488", "\ud83c\udf81", 6,
        [
            new("\uc138\ub840\uc120\ubb3c", "baptism-gift", "\uc138\ub168\uc131\uc0ac \uae30\ub150 \uc120\ubb3c", null, 1),
            new("\ucca8\uc601\uc131\uccb4", "first-communion", "\ucca8\uc601\uc131\uccb4 \uae30\ub150\ud488", null, 2),
            new("\ucd95\ubcf5\uc120\ubb3c", "blessing-gift", "\ucd95\ubcf5\uacfc \uac10\uc0ac\uc758 \uc120\ubb3c", null, 3)
        ])
    ];

    public static List<SeedProductDto> GetProducts() =>
    [
        // === 묵주 (rosary) ===
        new(
            Name: "\uc62c\ub9ac\ube0c\ub098\ubb34 5\ub2e8 \ubb35\uc8fc",
            Slug: "olive-wood-rosary-5",
            Description: "\uc131\uc9c0 \uc608\ub8e8\uc0b4\ub818\uc758 \uc62c\ub9ac\ube0c\ub098\ubb34\ub85c \uc81c\uc791\ub41c \uc804\ud1b5 5\ub2e8 \ubb35\uc8fc\uc785\ub2c8\ub2e4. \ub530\ub73b\ud55c \ub098\ubb34 \uc9c8\uac10\uc774 \uae30\ub3c4\uc5d0 \uc9d1\uc911\uc744 \ub3c4\uc640\uc90d\ub2c8\ub2e4.",
            Price: 35000,
            SalePrice: null,
            CategorySlug: "rosary-5",
            IsFeatured: true,
            IsNew: true),
        new(
            Name: "\uc740 \uc7a5\ubbf8 \ubb35\uc8fc",
            Slug: "silver-rose-rosary",
            Description: "\uc740\uc73c\ub85c \uc815\uad50\ud558\uac8c \uc81c\uc791\ub41c \uc7a5\ubbf8 \ubaa8\uc591 \ubb35\uc8fc\uc785\ub2c8\ub2e4. \uc138\ub828\ub41c \ub514\uc790\uc778\uc774 \ud2b9\ubcc4\ud55c \uc120\ubb3c\ub85c\ub3c4 \uc88b\uc2b5\ub2c8\ub2e4.",
            Price: 89000,
            SalePrice: null,
            CategorySlug: "rosary-5",
            IsFeatured: false,
            IsNew: true),
        new(
            Name: "\uc6d0\ud615 \ubcbd\uac78\uc774 \ubb35\uc8fc",
            Slug: "wall-circle-rosary",
            Description: "\uac00\uc815\uc758 \uae30\ub3c4 \uacf5\uac04\uc744 \uc544\ub984\ub2f5\uac8c \uafb8\uba70\uc8fc\ub294 \uc6d0\ud615 \ubcbd\uac78\uc774 \ubb35\uc8fc\uc785\ub2c8\ub2e4.",
            Price: 28000,
            SalePrice: null,
            CategorySlug: "rosary-circle",
            IsFeatured: false,
            IsNew: false),

        // === 성경/서적 (bible) ===
        new(
            Name: "\ud55c\uad6d\ucc9c\uc8fc\uad50 \uc8fc\uad50\ud68c\uc758 \uc131\uacbd",
            Slug: "korean-catholic-bible",
            Description: "\ud55c\uad6d\ucc9c\uc8fc\uad50 \uc8fc\uad50\ud68c\uc758 \uacf5\uc778 \ubc88\uc5ed \uc131\uacbd\uc785\ub2c8\ub2e4. \ud070 \uae00\uc528\uc640 \uc8fc\uc11d\uc73c\ub85c \uc77d\uae30 \ud3b8\ud569\ub2c8\ub2e4.",
            Price: 25000,
            SalePrice: null,
            CategorySlug: "bible-book",
            IsFeatured: true,
            IsNew: true),
        new(
            Name: "\ub9e4\uc77c\ubbf8\uc0ac",
            Slug: "daily-mass-book",
            Description: "\ub9e4\uc77c\uc758 \ubbf8\uc0ac \ub3c5\uc11c\uc640 \uae30\ub3c4\ubb38\uc774 \ub2f4\uae34 \uc6d4\uac04 \uc804\ub840\uc11c\uc785\ub2c8\ub2e4.",
            Price: 15000,
            SalePrice: null,
            CategorySlug: "liturgy-book",
            IsFeatured: false,
            IsNew: true),
        new(
            Name: "\uc131\uc778\uc804",
            Slug: "lives-of-saints",
            Description: "\uad50\ud68c\uac00 \uacf5\uacbd\ud558\ub294 \uc131\uc778\ub4e4\uc758 \uc0b6\uacfc \uc601\uc131\uc744 \ub2f4\uc740 \uc11c\uc801\uc785\ub2c8\ub2e4.",
            Price: 22000,
            SalePrice: null,
            CategorySlug: "spiritual-book",
            IsFeatured: false,
            IsNew: false),

        // === 성화/성상 (sacred-art) ===
        new(
            Name: "\uc131\ubaa8\ub9c8\ub9ac\uc544 \uc774\ucf58 \uc131\ud654",
            Slug: "virgin-mary-icon",
            Description: "\uc804\ud1b5 \uc774\ucf58 \uae30\ubc95\uc73c\ub85c \uadf8\ub824\uc9c4 \uc131\ubaa8\ub9c8\ub9ac\uc544 \uc131\ud654\uc785\ub2c8\ub2e4. \uae08\ubc15 \uc7a5\uc2dd\uc774 \uacbd\uac74\ud55c \uc544\ub984\ub2e4\uc6c0\uc744 \ub354\ud569\ub2c8\ub2e4.",
            Price: 120000,
            SalePrice: null,
            CategorySlug: "painting",
            IsFeatured: true,
            IsNew: true),
        new(
            Name: "\uc608\uc218 \uc131\uc2ec \uc131\uc0c1",
            Slug: "sacred-heart-statue",
            Description: "\uc608\uc218\ub2d8\uc758 \uc131\uc2ec\uc744 \ud45c\ud604\ud55c \uc815\uad50\ud55c \uc131\uc0c1\uc785\ub2c8\ub2e4. \uace0\uae09 \ub808\uc9c4 \uc18c\uc7ac\ub85c \uc138\ubc00\ud558\uac8c \uc81c\uc791\ub418\uc5c8\uc2b5\ub2c8\ub2e4.",
            Price: 180000,
            SalePrice: null,
            CategorySlug: "statue",
            IsFeatured: false,
            IsNew: true),
        new(
            Name: "\ub098\ubb34 \uc2ed\uc790\uac00 \ub300\ud615",
            Slug: "large-wooden-crucifix",
            Description: "\uc6d0\ubaa9\uc758 \uc790\uc5f0\uc2a4\ub7ec\uc6b4 \uacb0\uc744 \uc0b4\ub9b0 \ub300\ud615 \ubcbd\uac78\uc774 \uc2ed\uc790\uac00\uc785\ub2c8\ub2e4.",
            Price: 65000,
            SalePrice: null,
            CategorySlug: "crucifix",
            IsFeatured: false,
            IsNew: false),

        // === 전례용품 (liturgy-items) ===
        new(
            Name: "\ud770\uc0c9 \uc81c\uc758 \uc138\ud2b8",
            Slug: "white-vestment-set",
            Description: "\ubd80\ud65c\uc808\uacfc \uc131\ub2e8\uc5d0 \uc0ac\uc6a9\ud558\ub294 \ud770\uc0c9 \uc81c\uc758 \uc138\ud2b8\uc785\ub2c8\ub2e4. \uc2ed\uc790\uac00 \uc790\uc218 \uc7a5\uc2dd\uc774 \ud3ec\ud568\ub418\uc5b4 \uc788\uc2b5\ub2c8\ub2e4.",
            Price: 350000,
            SalePrice: null,
            CategorySlug: "vestment",
            IsFeatured: true,
            IsNew: true),
        new(
            Name: "\uc131\uc791 \uae08\ub3c4\uae08",
            Slug: "gold-plated-chalice",
            Description: "\ubbf8\uc0ac \uc804\ub840\uc5d0 \uc0ac\uc6a9\ud558\ub294 \uae08\ub3c4\uae08 \uc131\uc791\uc785\ub2c8\ub2e4. \uc2e0\uc911\ud55c \uc7a5\uc778 \uc815\uc2e0\uc73c\ub85c \uc81c\uc791\ub418\uc5c8\uc2b5\ub2c8\ub2e4.",
            Price: 280000,
            SalePrice: null,
            CategorySlug: "liturgical-vessel",
            IsFeatured: false,
            IsNew: true),
        new(
            Name: "\ucc9c\uc5f0\ubc00\ub78d \ucd08",
            Slug: "natural-beeswax-candle",
            Description: "\ucc9c\uc5f0 \ubc00\ub78d\uc73c\ub85c \ub9cc\ub4e0 \uc804\ub840\uc6a9 \ucd08\uc785\ub2c8\ub2e4. \uc740\uc740\ud55c \uaf43\ud5a5\uae30\uac00 \uae30\ub3c4 \ubd84\uc704\uae30\ub97c \ub354\ud574\uc90d\ub2c8\ub2e4.",
            Price: 12000,
            SalePrice: null,
            CategorySlug: "candle",
            IsFeatured: false,
            IsNew: false),

        // === 기도용품 (prayer-items) ===
        new(
            Name: "\ub8e8\ub974\ub4dc \uc131\uc218",
            Slug: "lourdes-holy-water",
            Description: "\ud504\ub791\uc2a4 \ub8e8\ub974\ub4dc \uc131\uc9c0\uc5d0\uc11c \uac00\uc838\uc628 \uc131\uc218\uc785\ub2c8\ub2e4. \uc131\ubaa8\ub9c8\ub9ac\uc544 \ubaa8\uc591 \ubcd1\uc5d0 \ub2f4\uaca8 \uc788\uc2b5\ub2c8\ub2e4.",
            Price: 8000,
            SalePrice: null,
            CategorySlug: "holy-water",
            IsFeatured: true,
            IsNew: true),
        new(
            Name: "\uc720\ud5a5 \uc138\ud2b8",
            Slug: "frankincense-set",
            Description: "\uc804\ub840\uc6a9 \ucc9c\uc5f0 \uc720\ud5a5\uacfc \ud5a5\ub85c \uc138\ud2b8\uc785\ub2c8\ub2e4. \uae4a\uc740 \ud5a5\uc774 \uae30\ub3c4\uc758 \uc2dc\uac04\uc744 \uac70\ub8e9\ud558\uac8c \ud574\uc90d\ub2c8\ub2e4.",
            Price: 25000,
            SalePrice: null,
            CategorySlug: "incense",
            IsFeatured: false,
            IsNew: true),
        new(
            Name: "\uac00\ud1a8\ub9ad \uae30\ub3c4\uc11c",
            Slug: "catholic-prayer-book",
            Description: "\uc77c\uacfc \uae30\ub3c4, \ubb35\uc8fc\uae30\ub3c4, \uc131\uccb4\uc870\ubc30 \uae30\ub3c4 \ub4f1\uc774 \ub2f4\uae34 \uc885\ud569 \uae30\ub3c4\uc11c\uc785\ub2c8\ub2e4.",
            Price: 18000,
            SalePrice: null,
            CategorySlug: "prayer-book",
            IsFeatured: false,
            IsNew: false),

        // === 선물/기념품 (gift) ===
        new(
            Name: "\uc138\ub840 \uae30\ub150 \uc2ed\uc790\uac00 \uc138\ud2b8",
            Slug: "baptism-cross-gift-set",
            Description: "\uc138\ub168\uc131\uc0ac \uae30\ub150\uc73c\ub85c \uc88b\uc740 \uc2ed\uc790\uac00\uc640 \ubb35\uc8fc \uc120\ubb3c \uc138\ud2b8\uc785\ub2c8\ub2e4. \uace0\uae09 \ud3ec\uc7a5\uc774 \ud3ec\ud568\ub418\uc5b4 \uc788\uc2b5\ub2c8\ub2e4.",
            Price: 45000,
            SalePrice: null,
            CategorySlug: "baptism-gift",
            IsFeatured: true,
            IsNew: true),
        new(
            Name: "\ucca8\uc601\uc131\uccb4 \uae30\ub150 \ubb35\uc8fc\ud568",
            Slug: "first-communion-rosary-box",
            Description: "\ucca8\uc601\uc131\uccb4\ub97c \uae30\ub150\ud558\ub294 \ud2b9\ubcc4\ud55c \ubb35\uc8fc\ud568\uc785\ub2c8\ub2e4. \uc774\ub984\uacfc \ub0a0\uc9dc \uac01\uc778 \uc11c\ube44\uc2a4\uac00 \uac00\ub2a5\ud569\ub2c8\ub2e4.",
            Price: 55000,
            SalePrice: null,
            CategorySlug: "first-communion",
            IsFeatured: false,
            IsNew: true),
        new(
            Name: "\ucd95\ubcf5\uc758 \uba54\ub2ec \uc138\ud2b8",
            Slug: "blessing-medal-set",
            Description: "\uc131 \ubca0\ub124\ub515\ud1a0 \uba54\ub2ec\uacfc \uae30\uc801\uc758 \uba54\ub2ec\uc774 \ud568\uaed8 \ub2f4\uae34 \ucd95\ubcf5 \uc138\ud2b8\uc785\ub2c8\ub2e4.",
            Price: 32000,
            SalePrice: null,
            CategorySlug: "blessing-gift",
            IsFeatured: false,
            IsNew: false)
    ];
}
