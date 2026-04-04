using Shop.Application.Platform.Commands;
using Shop.Domain.Entities;

namespace Shop.Infrastructure.Data;

/// <summary>
/// Industry-specific tenant templates.
/// Used during provisioning when no registered seed data (MoHyun, Catholia) exists.
/// </summary>
public static class TenantTemplates
{
    public static readonly string[] AvailableTypes =
        ["General", "Food", "Fashion", "Religious", "Manufacturing", "Beauty", "Electronics", "Lodging"];

    public static TenantConfig GetConfig(string businessType, string companyName)
    {
        var (primary, secondary, font) = businessType switch
        {
            "Food" => ("#D97706", "#92400E", "'Noto Serif KR', serif"),
            "Fashion" => ("#111827", "#6B7280", "'Pretendard', sans-serif"),
            "Religious" => ("#7C3AED", "#4C1D95", "'Noto Sans KR', sans-serif"),
            "Manufacturing" => ("#0369A1", "#0C4A6E", "'Inter', sans-serif"),
            "Beauty" => ("#EC4899", "#BE185D", "'Pretendard', sans-serif"),
            "Electronics" => ("#2563EB", "#1E3A5F", "'Inter', sans-serif"),
            "Lodging" => ("#0E7490", "#164E63", "'Pretendard', sans-serif"),
            _ => ("#3B82F6", "#1E40AF", "'Inter', sans-serif"),
        };

        var features = new List<string> { "aiChat", "collection", "community", "wishlist", "review", "qna" };
        if (businessType == "Religious")
        {
            features.AddRange(["liturgy", "saints", "baptismalName"]);
        }
        if (businessType == "Lodging")
        {
            features.Add("pms");
        }

        return new TenantConfig
        {
            Theme = new ThemeConfig
            {
                Primary = primary,
                Secondary = secondary,
                FontFamily = font,
                LogoUrl = "",
                FaviconUrl = ""
            },
            EnabledFeatures = features.ToArray(),
            Shipping = new ShippingConfig
            {
                FreeShippingThreshold = businessType == "Food" ? 30000 : 50000,
                DefaultShippingFee = businessType == "Food" ? 3500 : 3000,
                EstimatedDeliveryDays = 3
            },
            Seo = new SeoConfig
            {
                MetaTitle = companyName,
                MetaDescription = $"{companyName} 공식 온라인 쇼핑몰",
                MetaKeywords = businessType switch
                {
                    "Food" => "식품,건강식품,온라인쇼핑",
                    "Fashion" => "패션,의류,온라인쇼핑",
                    "Religious" => "성물,종교용품",
                    "Manufacturing" => "산업용품,부품,자재",
                    "Beauty" => "뷰티,화장품,스킨케어",
                    "Electronics" => "전자제품,가전,디지털",
                    "Lodging" => "숙박,호텔,펜션,예약",
                    _ => "온라인쇼핑몰"
                }
            },
            ChatPersona = new ChatPersonaConfig
            {
                Name = $"{companyName} AI 어시스턴트",
                Greeting = $"안녕하세요! {companyName}입니다. 무엇을 도와드릴까요?",
            },
            AutoCoupon = new AutoCouponConfig
            {
                WelcomeCouponEnabled = true,
                WelcomeCouponCode = "WELCOME",
                BirthdayCouponEnabled = true,
                BirthdayCouponCode = "BIRTHDAY"
            },
            Onboarding = new OnboardingConfig
            {
                IsCompleted = false,
                TemplateType = businessType,
                CompletedSteps = []
            }
        };
    }

    public static List<SeedCategoryDto> GetCategories(string businessType)
    {
        return businessType switch
        {
            "Food" => new()
            {
                new("전체상품", "all", "모든 상품", "ShoppingBag", 1),
                new("신선식품", "fresh", "신선한 재료", "Leaf", 2, new()
                {
                    new("채소/과일", "vegetables-fruits", null, null, 1),
                    new("육류/수산", "meat-seafood", null, null, 2),
                    new("유제품/계란", "dairy-eggs", null, null, 3),
                }),
                new("가공식품", "processed", "가공/즉석식품", "Package", 3, new()
                {
                    new("소스/양념", "sauce-seasoning", null, null, 1),
                    new("면/밀가루", "noodles-flour", null, null, 2),
                    new("통조림/레토르트", "canned-retort", null, null, 3),
                }),
                new("건강식품", "health", "건강/다이어트", "Heart", 4),
                new("음료/차", "beverage", "음료 및 차", "Coffee", 5),
                new("선물세트", "gift-set", "명절/선물용", "Gift", 6),
            },
            "Fashion" => new()
            {
                new("전체상품", "all", "모든 상품", "ShoppingBag", 1),
                new("여성의류", "women", "여성 패션", "User", 2, new()
                {
                    new("상의", "women-top", null, null, 1),
                    new("하의", "women-bottom", null, null, 2),
                    new("원피스", "women-dress", null, null, 3),
                    new("아우터", "women-outer", null, null, 4),
                }),
                new("남성의류", "men", "남성 패션", "User", 3, new()
                {
                    new("상의", "men-top", null, null, 1),
                    new("하의", "men-bottom", null, null, 2),
                    new("아우터", "men-outer", null, null, 3),
                }),
                new("가방/잡화", "accessories", "가방, 지갑, 벨트", "Briefcase", 4),
                new("신발", "shoes", "운동화, 구두, 부츠", "Footprints", 5),
                new("세일", "sale", "할인/이벤트", "Tag", 6),
            },
            "Beauty" => new()
            {
                new("전체상품", "all", "모든 상품", "ShoppingBag", 1),
                new("스킨케어", "skincare", "기초 화장품", "Droplet", 2, new()
                {
                    new("클렌징", "cleansing", null, null, 1),
                    new("토너/에센스", "toner-essence", null, null, 2),
                    new("크림/로션", "cream-lotion", null, null, 3),
                    new("선케어", "suncare", null, null, 4),
                }),
                new("메이크업", "makeup", "색조 화장품", "Palette", 3),
                new("바디/헤어", "body-hair", "바디워시, 샴푸", "Sparkles", 4),
                new("향수", "perfume", "향수/디퓨저", "Wind", 5),
                new("세일", "sale", "할인 상품", "Tag", 6),
            },
            "Electronics" => new()
            {
                new("전체상품", "all", "모든 상품", "ShoppingBag", 1),
                new("스마트폰/태블릿", "mobile", "모바일 기기", "Smartphone", 2),
                new("노트북/PC", "computer", "컴퓨터/주변기기", "Monitor", 3),
                new("가전제품", "appliance", "생활가전", "Tv", 4),
                new("오디오/영상", "audio-video", "스피커, 이어폰", "Headphones", 5),
                new("액세서리", "accessories", "케이스, 충전기", "Cable", 6),
            },
            "Lodging" => new()
            {
                new("객실", "rooms", "객실 안내", "Bed", 1, new()
                {
                    new("스탠다드", "standard", "스탠다드 객실", null, 1),
                    new("디럭스", "deluxe", "디럭스 객실", null, 2),
                    new("스위트", "suite", "스위트 객실", null, 3),
                    new("패밀리", "family", "패밀리 객실", null, 4),
                }),
                new("패키지", "packages", "숙박 패키지", "Gift", 2),
                new("부대시설", "facilities", "부대시설 안내", "Building", 3),
                new("다이닝", "dining", "레스토랑/카페", "UtensilsCrossed", 4),
                new("이벤트", "events", "프로모션/이벤트", "Tag", 5),
                new("주변관광", "attractions", "주변 관광지", "MapPin", 6),
            },
            _ => new() // General / Manufacturing / Other
            {
                new("전체상품", "all", "모든 상품", "ShoppingBag", 1),
                new("신상품", "new-arrivals", "새로 입고된 상품", "Sparkles", 2),
                new("베스트", "best-sellers", "인기 상품", "TrendingUp", 3),
                new("카테고리1", "category-1", null, "Folder", 4),
                new("카테고리2", "category-2", null, "Folder", 5),
                new("할인/이벤트", "sale", "세일 상품", "Tag", 6),
            },
        };
    }

    public static List<SeedProductDto> GetSampleProducts(string businessType)
    {
        return businessType switch
        {
            "Food" => new()
            {
                new("유기농 토마토 1kg", "organic-tomato", "GAP 인증 친환경 토마토", 8900, null, "vegetables-fruits", true, true),
                new("한우 등심 300g", "hanwoo-sirloin", "1++ 등급 한우 등심", 39000, 35000, "meat-seafood", true),
                new("특제 간장 500ml", "premium-soy-sauce", "전통 방식으로 숙성한 간장", 12000, null, "sauce-seasoning"),
                new("프리미엄 선물세트", "premium-gift-set", "명절 선물용 종합 세트", 59000, 49000, "gift-set", true, true),
            },
            "Fashion" => new()
            {
                new("오버핏 니트", "overfit-knit", "부드러운 캐시미어 블렌드", 49000, 39000, "women-top", true, true),
                new("슬림핏 데님", "slim-denim", "스트레치 데님 청바지", 59000, null, "men-bottom", true),
                new("미니 크로스백", "mini-crossbag", "데일리 미니 크로스백", 35000, 29000, "accessories", false, true),
                new("화이트 스니커즈", "white-sneakers", "클래식 화이트 스니커즈", 69000, null, "shoes", true),
            },
            "Beauty" => new()
            {
                new("수분 에센스 50ml", "hydra-essence", "히알루론산 집중 보습 에센스", 32000, 25600, "toner-essence", true, true),
                new("클렌징 폼 150ml", "cleansing-foam", "저자극 약산성 클렌징 폼", 18000, null, "cleansing"),
                new("선크림 SPF50+", "sun-cream", "촉촉한 수분 선크림", 22000, 17600, "suncare", true),
                new("시그니처 향수 50ml", "signature-perfume", "플로럴 우디 노트", 89000, null, "perfume", true, true),
            },
            _ => new()
            {
                new("샘플 상품 1", "sample-product-1", "샘플 상품입니다. 관리자에서 수정해주세요.", 10000, null, "new-arrivals", true, true),
                new("샘플 상품 2", "sample-product-2", "샘플 상품입니다. 관리자에서 수정해주세요.", 20000, 15000, "best-sellers", true),
            },
        };
    }
}
