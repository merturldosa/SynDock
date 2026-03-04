using Shop.Application.Platform.Commands;
using Shop.Domain.Entities;

namespace Shop.Infrastructure.Data;

public static class MoHyunSeedData
{
    public static TenantConfig GetConfig() => new()
    {
        Theme = new ThemeConfig
        {
            Primary = "#8B4513",
            PrimaryLight = "#A0522D",
            Secondary = "#2D5016",
            SecondaryLight = "#3D6B22",
            Background = "#FFF8F0",
            LogoUrl = null,
            FaviconUrl = null,
            FontFamily = "'Noto Serif KR', serif"
        },
        EnabledFeatures = ["aiChat", "collection"],
        ChatPersona = new ChatPersonaConfig
        {
            Name = "\uBAA8\uD604 AI \uB3C4\uC6B0\uBBF8",
            Greeting = "\uC548\uB155\uD558\uC138\uC694! \uC21C\uCC3D \uC804\uD1B5 \uC7A5\uB958\uC5D0 \uB300\uD574 \uAD81\uAE08\uD55C \uC810\uC744 \uBB3C\uC5B4\uBCF4\uC138\uC694.",
            SystemPrompt = "You are a helpful assistant for MoHyun, a Korean traditional fermented sauce (jang) shop from Sunchang. " +
                           "You specialize in doenjang (soybean paste), gochujang (red pepper paste), ganjang (soy sauce), " +
                           "and jirihwan (traditional health pills). Answer questions about products, fermentation processes, " +
                           "traditional recipes, storage tips, and health benefits. Respond primarily in Korean unless the user writes in another language."
        },
        Shipping = new ShippingConfig
        {
            FreeShippingThreshold = 30000,
            DefaultShippingFee = 3500,
            EstimatedDeliveryDays = 2,
            ShippingPolicy = "\uC8FC\uBB38 \uD655\uC778 \uD6C4 1~3\uC77C \uC774\uB0B4 \uC2E0\uC120\uD558\uAC8C \uCD9C\uACE0\uB429\uB2C8\uB2E4.",
            ReturnPolicy = "\uC2E0\uC120\uC2DD\uD488 \uD2B9\uC131\uC0C1 \uB2E8\uC21C \uBCC0\uC2EC \uBC18\uD488\uC774 \uC5B4\uB835\uC2B5\uB2C8\uB2E4. \uD488\uC9C8 \uBB38\uC81C \uC2DC 100% \uAD50\uD658/\uD658\uBD88 \uBCF4\uC7A5.",
            AvailableCarriers = ["CJ\uB300\uD55C\uD1B5\uC6B4", "\uD55C\uC9C4\uD0DD\uBC30"]
        },
        Seo = new SeoConfig
        {
            MetaTitle = "\uBAA8\uD604 - \uC21C\uCC3D \uC804\uD1B5 \uC7A5\uB958 \uC804\uBB38 \uC1FC\uD551\uBAAC",
            MetaDescription = "100\uB144 \uC804\uD1B5 \uC21C\uCC3D \uB41C\uC7A5, \uACE0\uCD94\uC7A5, \uAC04\uC7A5, \uC9C0\uB9AC\uD658\uC744 \uB9CC\uB098\uBCF4\uC138\uC694."
        },
        SocialLinks = new SocialLinksConfig
        {
            Instagram = "mohyun_jang",
            Youtube = "mohyun",
            Blog = "https://blog.mohyun.com",
            KakaoChannel = "mohyun"
        },
        Onboarding = new OnboardingConfig
        {
            IsCompleted = true,
            TemplateType = "mohyun"
        },
        PromoBanner = new PromoBannerConfig
        {
            Title = "\uC21C\uCC3D \uC804\uD1B5 \uC7A5\uB958 \uC2DC\uC98C",
            Description = "\uC62C\uD574 \uD56B \uBA54\uC8FC\uB85C \uB2F4\uADFC \uB41C\uC7A5\uC744 \uB9CC\uB098\uBCF4\uC138\uC694",
            IsActive = true,
            BackgroundColor = "#2D5016"
        },
        Settings = new Dictionary<string, object>
        {
            ["companyName"] = "\uBAA8\uD604",
            ["companyAddress"] = "\uC804\uB77C\uBD81\uB3C4 \uC21C\uCC3D\uAD70 \uC21C\uCC3D\uC74D \uC7A5\uB958\uB85C 123",
            ["businessNumber"] = "456-78-90123",
            ["ceoName"] = "\uBC15\uC21C\uCC3D",
            ["contactPhone"] = "063-650-1234",
            ["contactEmail"] = "contact@mohyun.com",
            ["heroSubtitle"] = "100\uB144 \uC804\uD1B5\uC758 \uB9DB, \uC21C\uCC3D \uC7A5\uB958",
            ["heroTagline"] = "\uBAA8\uD604 MoHyun",
            ["heroDescription"] = "\uC21C\uCC3D\uC758 \uB9D1\uC740 \uBB3C\uACFC \uAE68\uB057\uD55C \uACF5\uAE30\uB85C\n\uC815\uC131\uAECB \uBE5A\uC740 \uC804\uD1B5 \uC7A5\uB958\uC758 \uCC38\uB9DB"
        }
    };

    public static List<SeedCategoryDto> GetCategories() =>
    [
        // 1. \uB41C\uC7A5 (Doenjang - Soybean Paste)
        new SeedCategoryDto(
            Name: "\uB41C\uC7A5",
            Slug: "doenjang",
            Description: "\uC21C\uCC3D \uC804\uD1B5 \uBC29\uC2DD\uC73C\uB85C \uBC1C\uD6A8\uC2DC\uD0A8 \uB41C\uC7A5",
            Icon: "\uD83E\uDED8",
            SortOrder: 1,
            Children:
            [
                new SeedCategoryDto("\uC7AC\uB798\uB41C\uC7A5", "traditional-doenjang", "\uC804\uD1B5 \uBC29\uC2DD \uC7AC\uB798 \uB41C\uC7A5", null, 1),
                new SeedCategoryDto("\uC300\uC7A5", "ssamjang", "\uC374\uC5D0 \uC2F8 \uBA39\uB294 \uC300\uC7A5", null, 2),
                new SeedCategoryDto("\uCCAD\uAD6D\uC7A5", "cheonggukjang", "\uBE60\uB978 \uBC1C\uD6A8 \uCCAD\uAD6D\uC7A5", null, 3)
            ]),

        // 2. \uACE0\uCD94\uC7A5 (Gochujang - Red Pepper Paste)
        new SeedCategoryDto(
            Name: "\uACE0\uCD94\uC7A5",
            Slug: "gochujang",
            Description: "\uC21C\uCC3D \uC804\uD1B5 \uACE0\uCD94\uC7A5",
            Icon: "\uD83C\uDF36\uFE0F",
            SortOrder: 2,
            Children:
            [
                new SeedCategoryDto("\uC804\uD1B5\uACE0\uCD94\uC7A5", "traditional-gochujang", "\uC804\uD1B5 \uBC29\uC2DD \uACE0\uCD94\uC7A5", null, 1),
                new SeedCategoryDto("\uCC39\uC300\uACE0\uCD94\uC7A5", "glutinous-gochujang", "\uCC39\uC300\uB85C \uB2F4\uADFC \uACE0\uCD94\uC7A5", null, 2),
                new SeedCategoryDto("\uB9E4\uC6B4\uACE0\uCD94\uC7A5", "hot-gochujang", "\uB9E4\uCF64\uD55C \uACE0\uCD94\uC7A5", null, 3)
            ]),

        // 3. \uAC04\uC7A5 (Ganjang - Soy Sauce)
        new SeedCategoryDto(
            Name: "\uAC04\uC7A5",
            Slug: "ganjang",
            Description: "\uC21C\uCC3D \uC804\uD1B5 \uAC04\uC7A5",
            Icon: "\uD83E\uDD62",
            SortOrder: 3,
            Children:
            [
                new SeedCategoryDto("\uC870\uC120\uAC04\uC7A5", "joseon-ganjang", "\uC804\uD1B5 \uC870\uC120\uAC04\uC7A5", null, 1),
                new SeedCategoryDto("\uC591\uC870\uAC04\uC7A5", "brewed-ganjang", "\uC591\uC870 \uBC29\uC2DD \uAC04\uC7A5", null, 2),
                new SeedCategoryDto("\uB9DB\uAC04\uC7A5", "mat-ganjang", "\uB9CC\uB2A5 \uB9DB\uAC04\uC7A5", null, 3)
            ]),

        // 4. \uC9C0\uB9AC\uD658 (Jirihwan - Traditional Health Pills)
        new SeedCategoryDto(
            Name: "\uC9C0\uB9AC\uD658",
            Slug: "jirihwan",
            Description: "\uC21C\uCC3D \uC804\uD1B5 \uAC74\uAC15 \uD658",
            Icon: "\uD83D\uDC8A",
            SortOrder: 4),

        // 5. \uC120\uBB3C\uC138\uD2B8 (Gift Sets)
        new SeedCategoryDto(
            Name: "\uC120\uBB3C\uC138\uD2B8",
            Slug: "gift-set",
            Description: "\uC21C\uCC3D \uC7A5\uB958 \uC120\uBB3C \uC138\uD2B8",
            Icon: "\uD83C\uDF81",
            SortOrder: 5,
            Children:
            [
                new SeedCategoryDto("\uD504\uB9AC\uBBF8\uC5C4\uC138\uD2B8", "premium-set", "\uD504\uB9AC\uBBF8\uC5C4 \uC7A5\uB958 \uC138\uD2B8", null, 1),
                new SeedCategoryDto("\uBA85\uC808\uC138\uD2B8", "holiday-set", "\uBA85\uC808 \uC120\uBB3C\uC6A9 \uC138\uD2B8", null, 2),
                new SeedCategoryDto("\uC18C\uD3EC\uC7A5\uC138\uD2B8", "small-set", "\uC18C\uD3EC\uC7A5 \uB9DB\uBCF4\uAE30 \uC138\uD2B8", null, 3)
            ]),

        // 6. \uC2DD\uCD08/\uAE30\uD0C0 (Vinegar & Others)
        new SeedCategoryDto(
            Name: "\uC2DD\uCD08/\uAE30\uD0C0",
            Slug: "vinegar-etc",
            Description: "\uC804\uD1B5 \uC2DD\uCD08 \uBC0F \uAE30\uD0C0 \uBC1C\uD6A8 \uC2DD\uD488",
            Icon: "\uD83C\uDF76",
            SortOrder: 6,
            Children:
            [
                new SeedCategoryDto("\uC804\uD1B5\uC2DD\uCD08", "traditional-vinegar", "\uC804\uD1B5 \uBC29\uC2DD \uC2DD\uCD08", null, 1),
                new SeedCategoryDto("\uC7A5\uC544\uCC0C", "jangajji", "\uC804\uD1B5 \uC7A5\uC544\uCC0C", null, 2),
                new SeedCategoryDto("\uACE0\uCD67\uAC00\uB8E8", "gochugaru", "\uC21C\uCC3D \uACE0\uCD67\uAC00\uB8E8", null, 3)
            ])
    ];

    public static List<SeedProductDto> GetProducts() =>
    [
        // === \uB41C\uC7A5 (Doenjang) ===
        new SeedProductDto(
            Name: "\uC21C\uCC3D \uC804\uD1B5 \uC7AC\uB798 \uB41C\uC7A5 1kg",
            Slug: "sunchang-traditional-doenjang-1kg",
            Description: "\uC21C\uCC3D\uC758 \uB9D1\uC740 \uACF5\uAE30\uC640 \uAE68\uB057\uD55C \uBB3C\uB85C \uBE5A\uC740 \uC804\uD1B5 \uC7AC\uB798 \uB41C\uC7A5\uC785\uB2C8\uB2E4. 1\uB144 \uC774\uC0C1 \uC790\uC5F0 \uBC1C\uD6A8\uC2DC\uCF1C \uAE4A\uC740 \uAC10\uCE60\uB9DB\uC744 \uB2F4\uC558\uC2B5\uB2C8\uB2E4.",
            Price: 15000,
            SalePrice: null,
            CategorySlug: "traditional-doenjang",
            IsFeatured: true,
            IsNew: true),

        new SeedProductDto(
            Name: "\uC21C\uCC3D \uC300\uC7A5 500g",
            Slug: "sunchang-ssamjang-500g",
            Description: "\uC2E0\uC120\uD55C \uCC44\uC18C\uC640 \uD568\uAED8 \uC374\uC5D0 \uC2F8 \uBA39\uAE30 \uC88B\uC740 \uACE0\uC18C\uD55C \uC300\uC7A5\uC785\uB2C8\uB2E4. \uB41C\uC7A5\uACFC \uACE0\uCD94\uC7A5\uC758 \uC870\uD654\uB85C\uC6B4 \uBE14\uB80C\uB529.",
            Price: 8000,
            SalePrice: 6500,
            CategorySlug: "ssamjang",
            IsFeatured: false,
            IsNew: true),

        new SeedProductDto(
            Name: "\uCCAD\uAD6D\uC7A5 \uBD84\uB9D0 300g",
            Slug: "cheonggukjang-powder-300g",
            Description: "\uBE60\uB978 \uBC1C\uD6A8\uB85C \uAD6C\uC218\uD55C \uD5A5\uC774 \uC0B4\uC544\uC788\uB294 \uCCAD\uAD6D\uC7A5 \uBD84\uB9D0\uC785\uB2C8\uB2E4. \uCC0C\uAC1C\uB098 \uC694\uB9AC\uC5D0 \uAC04\uD3B8\uD558\uAC8C \uC0AC\uC6A9\uD558\uC138\uC694.",
            Price: 12000,
            SalePrice: null,
            CategorySlug: "cheonggukjang",
            IsFeatured: false,
            IsNew: false),

        // === \uACE0\uCD94\uC7A5 (Gochujang) ===
        new SeedProductDto(
            Name: "\uC21C\uCC3D \uC804\uD1B5 \uACE0\uCD94\uC7A5 1kg",
            Slug: "sunchang-traditional-gochujang-1kg",
            Description: "\uD587\uACE0\uCD94\uC640 \uCC39\uC300\uB85C \uC815\uC131\uAECB \uB2F4\uADFC \uC21C\uCC3D \uC804\uD1B5 \uACE0\uCD94\uC7A5\uC785\uB2C8\uB2E4. \uB2EC\uCF64\uD558\uBA74\uC11C\uB3C4 \uC54C\uC2F8\uD55C \uB9DB\uC774 \uC77C\uD488\uC785\uB2C8\uB2E4.",
            Price: 18000,
            SalePrice: null,
            CategorySlug: "traditional-gochujang",
            IsFeatured: true,
            IsNew: true),

        new SeedProductDto(
            Name: "\uCC39\uC300 \uACE0\uCD94\uC7A5 500g",
            Slug: "glutinous-gochujang-500g",
            Description: "\uCC39\uC300\uC758 \uCC30\uAE30\uAC00 \uB354\uD574\uC9C4 \uBD80\uB4DC\uB7EC\uC6B4 \uACE0\uCD94\uC7A5\uC785\uB2C8\uB2E4. \uBE44\uBE54\uBC25\uC774\uB098 \uB5A1\uBCF6\uC774\uC5D0 \uC798 \uC5B4\uC6B8\uB9BD\uB2C8\uB2E4.",
            Price: 12000,
            SalePrice: 9900,
            CategorySlug: "glutinous-gochujang",
            IsFeatured: false,
            IsNew: true),

        new SeedProductDto(
            Name: "\uB9E4\uC6B4 \uACE0\uCD94\uC7A5 500g",
            Slug: "hot-gochujang-500g",
            Description: "\uCCAD\uC591\uACE0\uCD94\uB97C \uB4EC\uBF51 \uB123\uC5B4 \uD654\uB044\uD55C \uB9DB\uC744 \uB0B4\uB294 \uB9E4\uC6B4 \uACE0\uCD94\uC7A5\uC785\uB2C8\uB2E4. \uB9E4\uC6B4 \uC74C\uC2DD \uC560\uD638\uAC00\uC5D0\uAC8C \uCD94\uCC9C\uD569\uB2C8\uB2E4.",
            Price: 10000,
            SalePrice: null,
            CategorySlug: "hot-gochujang",
            IsFeatured: false,
            IsNew: false),

        // === \uAC04\uC7A5 (Ganjang) ===
        new SeedProductDto(
            Name: "3\uB144 \uC219\uC131 \uC870\uC120\uAC04\uC7A5 500ml",
            Slug: "3year-joseon-ganjang-500ml",
            Description: "3\uB144 \uC774\uC0C1 \uC625\uD56D\uC544\uB9AC\uC5D0\uC11C \uC790\uC5F0 \uC219\uC131\uC2DC\uD0A8 \uAE4A\uC740 \uD48D\uBBF8\uC758 \uC870\uC120\uAC04\uC7A5\uC785\uB2C8\uB2E4. \uB098\uBB3C\uC774\uB098 \uBCF6\uC74C\uC5D0 \uC81C\uACA9\uC785\uB2C8\uB2E4.",
            Price: 22000,
            SalePrice: null,
            CategorySlug: "joseon-ganjang",
            IsFeatured: true,
            IsNew: true),

        new SeedProductDto(
            Name: "\uC591\uC870\uAC04\uC7A5 1L",
            Slug: "brewed-ganjang-1l",
            Description: "\uC790\uC5F0 \uC591\uC870 \uBC29\uC2DD\uC73C\uB85C \uBE5A\uC740 \uAD48\uD615 \uC7A1\uD78C \uAC04\uC7A5\uC785\uB2C8\uB2E4. \uC77C\uC0C1 \uC694\uB9AC\uC5D0 \uB9CC\uB2A5\uC73C\uB85C \uC0AC\uC6A9\uD558\uC138\uC694.",
            Price: 15000,
            SalePrice: null,
            CategorySlug: "brewed-ganjang",
            IsFeatured: false,
            IsNew: false),

        new SeedProductDto(
            Name: "\uB9CC\uB2A5 \uB9DB\uAC04\uC7A5 500ml",
            Slug: "all-purpose-mat-ganjang-500ml",
            Description: "\uAC04\uC7A5\uC5D0 \uB9C8\uB298, \uD30C, \uCC38\uAE68\uB97C \uB123\uC5B4 \uB9CC\uB4E0 \uB9CC\uB2A5 \uB9DB\uAC04\uC7A5\uC785\uB2C8\uB2E4. \uBE44\uBE54\uBC25\uBD80\uD130 \uBCF6\uC74C\uAE4C\uC9C0 \uD55C \uBCD1\uC73C\uB85C \uD574\uACB0\uD558\uC138\uC694.",
            Price: 13000,
            SalePrice: 10000,
            CategorySlug: "mat-ganjang",
            IsFeatured: false,
            IsNew: true),

        // === \uC9C0\uB9AC\uD658 (Jirihwan) ===
        new SeedProductDto(
            Name: "\uC21C\uCC3D \uC9C0\uB9AC\uD658 100g",
            Slug: "sunchang-jirihwan-100g",
            Description: "\uC9C0\uB9AC\uC0B0 \uC57D\uCD08\uC640 \uC804\uD1B5 \uBC29\uBC95\uC73C\uB85C \uBE5A\uC740 \uAC74\uAC15 \uD658\uC785\uB2C8\uB2E4. \uB9E4\uC77C \uC544\uCE68 \uACF5\uBCF5\uC5D0 \uBB3C\uACFC \uD568\uAED8 \uB4DC\uC138\uC694.",
            Price: 35000,
            SalePrice: null,
            CategorySlug: "jirihwan",
            IsFeatured: true,
            IsNew: true),

        new SeedProductDto(
            Name: "\uC9C0\uB9AC\uD658 \uC120\uBB3C \uC138\uD2B8",
            Slug: "jirihwan-gift-set",
            Description: "\uACE0\uAE09 \uBAA9\uD568 \uD3EC\uC7A5\uC73C\uB85C \uAC10\uC2FC \uC9C0\uB9AC\uD658 \uC120\uBB3C \uC138\uD2B8\uC785\uB2C8\uB2E4. \uBD80\uBAA8\uB2D8, \uC5B4\uB978 \uC120\uBB3C\uB85C \uCD5C\uACE0\uC758 \uC120\uD0DD\uC785\uB2C8\uB2E4.",
            Price: 65000,
            SalePrice: null,
            CategorySlug: "jirihwan",
            IsFeatured: false,
            IsNew: true),

        // === \uC120\uBB3C\uC138\uD2B8 (Gift Sets) ===
        new SeedProductDto(
            Name: "\uD504\uB9AC\uBBF8\uC5C4 \uC7A5\uB958 3\uC885 \uC138\uD2B8",
            Slug: "premium-jang-3-set",
            Description: "\uB41C\uC7A5, \uACE0\uCD94\uC7A5, \uAC04\uC7A5 3\uC885\uC744 \uD55C \uBC88\uC5D0 \uB9DB\uBCFC \uC218 \uC788\uB294 \uD504\uB9AC\uBBF8\uC5C4 \uC138\uD2B8\uC785\uB2C8\uB2E4. \uACE0\uAE09 \uD3EC\uC7A5\uC73C\uB85C \uC120\uBB3C\uC6A9\uC73C\uB85C\uB3C4 \uC88B\uC2B5\uB2C8\uB2E4.",
            Price: 48000,
            SalePrice: null,
            CategorySlug: "premium-set",
            IsFeatured: true,
            IsNew: true),

        new SeedProductDto(
            Name: "\uBA85\uC808 \uC7A5\uB958 5\uC885 \uC138\uD2B8",
            Slug: "holiday-jang-5-set",
            Description: "\uC124\uB0A0\uACFC \uCD94\uC11D\uC5D0 \uB9DE\uB294 \uC131\uC2EC \uB2F4\uC740 5\uC885 \uC7A5\uB958 \uC138\uD2B8\uC785\uB2C8\uB2E4. \uAC10\uC0AC\uC758 \uB9C8\uC74C\uC744 \uC804\uD1B5 \uC7A5\uB958\uC5D0 \uB2F4\uC544\uBCF4\uC138\uC694.",
            Price: 78000,
            SalePrice: null,
            CategorySlug: "holiday-set",
            IsFeatured: false,
            IsNew: true),

        new SeedProductDto(
            Name: "\uC18C\uD3EC\uC7A5 \uB9DB\uBCF4\uAE30 \uC138\uD2B8",
            Slug: "small-tasting-set",
            Description: "\uCC98\uC74C \uC21C\uCC3D \uC7A5\uB958\uB97C \uC811\uD558\uB294 \uBD84\uB4E4\uC744 \uC704\uD55C \uC18C\uD3EC\uC7A5 \uB9DB\uBCF4\uAE30 \uC138\uD2B8\uC785\uB2C8\uB2E4. \uBD80\uB2F4 \uC5C6\uC774 \uB2E4\uC591\uD55C \uC7A5\uB958\uB97C \uACBD\uD5D8\uD558\uC138\uC694.",
            Price: 25000,
            SalePrice: 19900,
            CategorySlug: "small-set",
            IsFeatured: false,
            IsNew: true),

        // === \uC2DD\uCD08/\uAE30\uD0C0 (Vinegar & Others) ===
        new SeedProductDto(
            Name: "\uC804\uD1B5 \uD604\uBBF8\uC2DD\uCD08 500ml",
            Slug: "traditional-brown-rice-vinegar-500ml",
            Description: "\uD604\uBBF8\uB97C \uC790\uC5F0 \uBC1C\uD6A8\uC2DC\uCF1C \uBE5A\uC740 \uC804\uD1B5 \uD604\uBBF8\uC2DD\uCD08\uC785\uB2C8\uB2E4. \uC0C8\uCF64\uD558\uBA74\uC11C\uB3C4 \uBD80\uB4DC\uB7EC\uC6B4 \uB9DB\uC774 \uD2B9\uC9D5\uC785\uB2C8\uB2E4.",
            Price: 16000,
            SalePrice: null,
            CategorySlug: "traditional-vinegar",
            IsFeatured: true,
            IsNew: true),

        new SeedProductDto(
            Name: "\uB41C\uC7A5 \uC7A5\uC544\uCC0C 300g",
            Slug: "doenjang-jangajji-300g",
            Description: "\uB41C\uC7A5\uC5D0 \uBC15\uC544 \uC219\uC131\uC2DC\uD0A8 \uC804\uD1B5 \uC7A5\uC544\uCC0C\uC785\uB2C8\uB2E4. \uBC25\uBC18\uCC2C\uC73C\uB85C \uADF8\uB9CC\uC778 \uAE4A\uC740 \uAC10\uCE60\uB9DB\uC744 \uC990\uACA8\uBCF4\uC138\uC694.",
            Price: 9000,
            SalePrice: null,
            CategorySlug: "jangajji",
            IsFeatured: false,
            IsNew: false),

        new SeedProductDto(
            Name: "\uC21C\uCC3D \uACE0\uCD67\uAC00\uB8E8 500g",
            Slug: "sunchang-gochugaru-500g",
            Description: "\uC21C\uCC3D \uD587\uACE0\uCD94\uB97C \uD587\uBCCC\uC5D0 \uB9D0\uB824 \uBE7B\uC740 \uD504\uB9AC\uBBF8\uC5C4 \uACE0\uCD67\uAC00\uB8E8\uC785\uB2C8\uB2E4. \uAE40\uCE58\uB2F4\uADF8\uAE30\uC640 \uC694\uB9AC\uC5D0 \uC544\uC8FC \uC88B\uC2B5\uB2C8\uB2E4.",
            Price: 20000,
            SalePrice: null,
            CategorySlug: "gochugaru",
            IsFeatured: false,
            IsNew: true)
    ];
}
