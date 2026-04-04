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
            LogoUrl = "/uploads/tenants/mohyun/logo.png",
            FaviconUrl = "/uploads/tenants/mohyun/favicon.ico",
            FontFamily = "'Noto Serif KR', serif"
        },
        EnabledFeatures = ["aiChat", "collection", "community", "wishlist", "review", "qna"],
        SeasonalThemes = new Dictionary<string, SeasonalThemeConfig>
        {
            ["spring"] = new() { Primary = "#6B8E23", Secondary = "#556B2F", Background = "#FAFFF0" },
            ["summer"] = new() { Primary = "#CD853F", Secondary = "#2E8B57", Background = "#FFFFF0" },
            ["autumn"] = new() { Primary = "#A0522D", Secondary = "#8B4513", Background = "#FFF5EB" },
            ["winter"] = new() { Primary = "#8B0000", Secondary = "#2F4F4F", Background = "#FFF8F5" }
        },
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
        Onboarding = new OnboardingConfig { IsCompleted = true, TemplateType = "mohyun" },
        PromoBanner = new PromoBannerConfig
        {
            Title = "\uC21C\uCC3D \uC804\uD1B5 \uC7A5\uB958 \uC2DC\uC98C",
            Description = "\uC62C\uD574 \uD56B \uBA54\uC8FC\uB85C \uB2F4\uADFC \uB41C\uC7A5\uC744 \uB9CC\uB098\uBCF4\uC138\uC694",
            IsActive = true,
            BackgroundColor = "#2D5016"
        },
        AutoCoupon = new AutoCouponConfig
        {
            WelcomeCouponEnabled = true, WelcomeCouponCode = "MOHYUN-WELCOME",
            BirthdayCouponEnabled = true, BirthdayCouponCode = "MOHYUN-BIRTHDAY"
        },
        Settings = new Dictionary<string, object>
        {
            ["companyName"] = "\uBAA8\uD604",
            ["companyAddress"] = "\uC804\uB77C\uBD81\uB3C4 \uC21C\uCC3D\uAD70 \uC801\uC131\uBA74 \uC801\uC131\uB85C 145-9",
            ["businessNumber"] = "456-78-90123",
            ["ceoName"] = "\uC774\uC6B0\uC11D",
            ["contactPhone"] = "010-4050-4009",
            ["contactEmail"] = "goosbeery@hanmail.net",
            ["heroSubtitle"] = "100\uB144 \uC804\uD1B5\uC758 \uB9DB, \uC21C\uCC3D \uC7A5\uB958",
            ["heroTagline"] = "\uBAA8\uD604 MoHyun",
            ["heroDescription"] = "\uC21C\uCC3D\uC758 \uB9D1\uC740 \uBB3C\uACFC \uAE68\uB057\uD55C \uACF5\uAE30\uB85C\n\uC815\uC131\uAECB \uBE5A\uC740 \uC804\uD1B5 \uC7A5\uB958\uC758 \uCC38\uB9DB"
        }
    };

    public static List<SeedCategoryDto> GetCategories() =>
    [
        new SeedCategoryDto("전통장류", "traditional-jang", "순창 전통 간장, 고추장, 된장", "🫘", 1,
            [
                new SeedCategoryDto("간장", "ganjang", "전통 숙성 간장", null, 1),
                new SeedCategoryDto("고추장", "gochujang", "전통 고추장", null, 2),
                new SeedCategoryDto("된장", "doenjang", "전통 된장", null, 3)
            ]),
        new SeedCategoryDto("소스", "sauce", "전통 장류 기반 만능 소스", "🥫", 2,
            [
                new SeedCategoryDto("간장소스", "soy-sauce", "간장 베이스 소스", null, 1),
                new SeedCategoryDto("된장소스", "bean-sauce", "된장 베이스 소스", null, 2),
                new SeedCategoryDto("고추장소스", "pepper-sauce", "고추장 베이스 소스", null, 3),
                new SeedCategoryDto("핫소스", "hot-sauce", "매운 핫소스", null, 4)
            ]),
        new SeedCategoryDto("건강식품", "health", "전통 건강 보조 식품", "💊", 3,
            [
                new SeedCategoryDto("지리환", "jirihwan", "전통 건강 환", null, 1),
                new SeedCategoryDto("꽃벵이환", "kkotbaengi", "꽃벵이 발효 건강환", null, 2),
                new SeedCategoryDto("건강식초", "health-vinegar", "발효 건강 식초", null, 3)
            ]),
        new SeedCategoryDto("선물세트", "gift-set", "명절 및 선물용 장류 세트", "🎁", 4),
        new SeedCategoryDto("간식", "snack", "건강한 영양 간식", "🍪", 5),
        new SeedCategoryDto("액젓/기타", "etc", "액젓 및 기타 발효 식품", "🐟", 6)
    ];

    private static string Specs(string json) => json;

    public static List<SeedProductDto> GetProducts() =>
    [
        // ─── 전통장류: 간장 ───
        new("맛있는 간장 300ml", "delicious-ganjang-300ml",
            "깊은 맛과 풍미가 살아있는 프리미엄 간장입니다. 자연 숙성 방식으로 제조되어 건강한 맛을 자랑합니다.",
            15000, null, "ganjang", true, false, "300ml / 한식간장 / 국산 대두",
            "/uploads/tenants/mohyun/products/soy_sauce.jpg",
            Specs(@"{""features"":[""100% 국산 대두 사용 (NON-GMO)"",""3년 이상 전통 항아리 자연 숙성"",""무방부제, 무색소, 무화학조미료"",""HACCP 인증 시설에서 위생적인 생산""],""specs"":{""volume"":""300ml"",""type"":""한식간장"",""ingredients"":""대두(국산), 천일염(국산), 정제수"",""storage"":""직사광선을 피하고 서늘한 곳 보관""}}")),

        // ─── 전통장류: 고추장 ───
        new("영양 고추장 300g", "nutrient-gochujang-300g",
            "매콤하면서도 감칠맛이 도는 영양 만점 고추장입니다. 다양한 요리에 활용하기 좋습니다.",
            22000, null, "gochujang", true, false, "300g / 전통고추장 / 국산 태양초",
            "/uploads/tenants/mohyun/products/red_paste.jpg",
            Specs(@"{""features"":[""100% 국산 태양초 고추 사용"",""국산 찹쌀 조청으로 낸 건강한 단맛"",""전통 방식 그대로 메주가루 발효"",""깔끔하고 칼칼한 매운맛""],""specs"":{""volume"":""300g"",""type"":""전통고추장"",""ingredients"":""고춧가루(국산), 찹쌀(국산), 메주가루, 조청, 천일염"",""storage"":""냉장보관 (0~10℃)""}}")),

        // ─── 전통장류: 된장 ───
        new("영양 된장 300g", "nutrient-doenjang-300g",
            "구수한 맛이 일품인 전통 방식의 영양 된장입니다. 찌개나 국 요리에 최적입니다.",
            18000, null, "doenjang", true, false, "300g / 한식된장 / 국산 콩",
            "/uploads/tenants/mohyun/products/soybean_paste.jpg",
            Specs(@"{""features"":[""국산 콩 100% 사용"",""전통 방식 메주 발효"",""살아있는 콩 알갱이 식감"",""깊고 진한 구수한 맛""],""specs"":{""volume"":""300g"",""type"":""한식된장"",""ingredients"":""대두(국산), 천일염(국산), 정제수"",""storage"":""냉장보관""}}")),

        // ─── 소스: 간장소스 ───
        new("간장 소스 300ml", "soy-sauce-300ml",
            "바베큐, 양념갈비, 샐러드에 잘 어울리는 만능 간장 소스입니다.",
            8000, null, "soy-sauce", false, true, "300ml / 만능 소스",
            "/uploads/tenants/mohyun/products/sauce_soy.jpg",
            Specs(@"{""features"":[""양념갈비 재울 때 최적"",""샐러드 오리엔탈 드레싱 대용 가능"",""바베큐 디핑 소스로 활용""],""specs"":{""volume"":""300ml"",""uses"":""바베큐, 양념갈비, 샐러드""}}")),

        // ─── 소스: 된장소스 ───
        new("된장 소스 300ml", "bean-sauce-300ml",
            "바베큐에 가장 잘 어울리는 특제 된장 소스입니다. 파스타에도 활용 가능합니다.",
            8000, null, "bean-sauce", false, true, "300ml / 된장 베이스",
            "/uploads/tenants/mohyun/products/sauce_bean.jpg",
            Specs(@"{""features"":[""바베큐 딥핑 소스로 강력 추천"",""크림 파스타에 넣어 된장 파스타 가능"",""고기 요리와 최상의 궁합""],""specs"":{""volume"":""300ml"",""uses"":""바베큐, 크림 파스타""}}")),

        // ─── 소스: 고추장소스 ───
        new("고추장 소스 300ml", "pepper-sauce-300ml",
            "매콤달콤한 맛으로 양념갈비, 비빔국수, 골뱅이무침에 딱입니다.",
            8000, null, "pepper-sauce", false, true, "300ml / 고추장 베이스",
            "/uploads/tenants/mohyun/products/sauce_red.jpg",
            Specs(@"{""features"":[""양념갈비(Best)"",""비빔면 소스 활용"",""골뱅이 무침 양념으로 최적""],""specs"":{""volume"":""300ml"",""uses"":""양념갈비, 비빔국수, 골뱅이무침""}}")),

        // ─── 소스: 핫소스 ───
        new("핫 소스 300ml", "hot-sauce-300ml",
            "로스트 치킨이나 돼지고기, 타코 샐러드에 어울리는 화끈한 매운맛 소스입니다.",
            8000, null, "hot-sauce", false, true, "300ml / 핫소스",
            "/uploads/tenants/mohyun/products/sauce_hot.jpg",
            Specs(@"{""features"":[""로스트 치킨 디핑 소스"",""타코 샐러드 드레싱"",""샌드위치 스프레드로 활용""],""specs"":{""volume"":""300ml"",""uses"":""로스트 치킨/포크, 타코샐러드, 샌드위치""}}")),

        // ─── 건강식품: 꾸지뽕 식초 ───
        new("꾸지뽕 식초 500ml", "cudrania-vinegar-500ml",
            "꾸지뽕의 영양을 그대로 담은 건강 식초입니다. 물에 희석하여 드시면 좋습니다.",
            30000, null, "health-vinegar", true, false, "500ml / 발효식초 / 국산 꾸지뽕",
            "/uploads/tenants/mohyun/products/vinegar.png",
            Specs(@"{""features"":[""국내산 꾸지뽕 열매 100% 사용"",""전통 항아리 자연 발효 숙성"",""부드러운 목넘김과 깊은 향"",""다양한 요리 및 건강 음료로 활용 가능""],""specs"":{""volume"":""500ml"",""type"":""발효식초"",""ingredients"":""꾸지뽕열매(국산), 정제수, 유기농설탕"",""storage"":""직사광선을 피하고 서늘한 곳 보관""}}")),

        // ─── 건강식품: 블루베리 식초 ───
        new("블루베리 식초 500ml", "blueberry-vinegar-500ml",
            "항산화 성분이 풍부한 블루베리를 통째로 발효시킨 상큼한 식초입니다.",
            32000, null, "health-vinegar", false, true, "500ml / 발효식초 / 국산 블루베리",
            "/uploads/tenants/mohyun/products/blueberry_vinegar.png",
            Specs(@"{""features"":[""국산 유기농 블루베리 사용"",""안토시아닌 풍부"",""샐러드 드레싱으로 최적"",""전통 항아리 자연 발효""],""specs"":{""volume"":""500ml"",""type"":""발효식초"",""ingredients"":""블루베리(국산), 정제수, 유기농설탕"",""storage"":""직사광선을 피하고 서늘한 곳 보관""}}")),

        // ─── 건강식품: 꽃벵이 환 ───
        new("꽃벵이 환 300g", "kkotbaengi-pill-300g",
            "활력 넘치는 하루를 위한 꽃벵이 환입니다. 먹기 편한 스틱형 포장입니다.",
            45000, null, "kkotbaengi", true, false, "300g (3g x 100포) / 1일 1~2회",
            "/uploads/tenants/mohyun/products/fermented_pill.jpg",
            Specs(@"{""features"":[""국내산 꽃벵이 100% 사용"",""혈전 용해에 도움을 주는 인돌 알칼로이드 함유"",""특허 유산균 발효로 냄새 없이 고소한 맛"",""휴대가 간편한 스틱형 포장""],""specs"":{""volume"":""300g (3g x 100포)"",""consumption"":""1일 1~2회, 1회 1포"",""ingredients"":""꽃벵이 발효분말 95%, 찹쌀풀 5%"",""storage"":""직사광선을 피하고 서늘한 곳 보관""},""healthBenefits"":[""간 건강"",""혈액 순환 개선""]}")),

        // ─── 건강식품: 프리미엄 지리환 ───
        new("[프리미엄] 지리환 300g", "premium-jirihwan-300g",
            "지리산의 정기와 10가지 귀한 한방 재료가 만난 황제의 보약입니다.",
            120000, null, "jirihwan", true, true, "300g (30환) / 1일 1환 / 프리미엄",
            "/uploads/tenants/mohyun/products/jirihwan_premium.jpg",
            Specs(@"{""features"":[""지리산 꽃벵이 + 녹용 + 침향 + 홍삼 배합"",""전통 방식 그대로 72시간 중탕 및 제환"",""설탕, 합성착향료, 보존료 無첨가"",""VIP용 고급 패키지 및 보자기 포장""],""specs"":{""volume"":""300g (30환)"",""consumption"":""1일 1환, 천천히 씹어서 섭취"",""ingredients"":""꽃벵이(국산), 침향(인도네시아), 녹용(러시아), 홍삼, 당귀, 산수유, 꿀"",""storage"":""직사광선을 피하고 서늘한 곳 보관""},""premiumIngredients"":[""러시아산 녹용"",""인도네시아산 침향"",""6년근 홍삼""]}")),

        // ─── 선물세트 ───
        new("명품 장류 선물 세트", "premium-jang-gift-set",
            "간장, 고추장, 된장으로 구성된 품격 있는 선물 세트입니다. 명절 선물로 추천합니다.",
            55000, null, "gift-set", true, false, "간장 300ml + 고추장 300g + 된장 300g",
            "/uploads/tenants/mohyun/products/gift_set.png",
            Specs(@"{""features"":[""베스트셀러 3종(간장, 고추장, 된장) 통합 구성"",""고급스러운 디자인의 선물 박스 + 쇼핑백"",""명절/생일/감사 선물로 최적"",""전통 장류의 깊은 맛을 한 번에""],""specs"":{""composition"":""맛있는 간장 300ml + 영양 고추장 300g + 영양 된장 300g"",""packaging"":""고급 하드케이스 + 쇼핑백""}}")),

        // ─── 간식 ───
        new("고소애 쿠키 120g", "insect-cookie-120g",
            "고단백 영양 간식, 고소애를 넣어 만든 바삭하고 고소한 쿠키입니다.",
            5000, null, "snack", false, true, "120g (12g x 10개입) / 고단백 간식",
            "/uploads/tenants/mohyun/products/insect_cookie.jpg",
            Specs(@"{""features"":[""고단백 식용 곤충(고소애) 분말 함유"",""일반 쿠키 대비 높은 단백질 함량"",""고소하고 바삭한 식감 (거부감 ZERO)"",""개별 포장으로 간편한 휴대""],""specs"":{""volume"":""120g (12g x 10개입)"",""type"":""과자/쿠키"",""ingredients"":""밀가루(국산), 버터, 설탕, 고소애분말 15%, 계란"",""storage"":""직사광선을 피하고 서늘한 곳 보관""},""nutritionalFacts"":{""protein"":""53g/100g (소고기 26g 대비 2배)"",""unsaturatedFat"":""75%"",""vitamins"":""비타민 B3, B5 풍부""}}")),

        // ─── 액젓/기타 ───
        new("프리미엄 액젓 1kg", "premium-fish-sauce-1kg",
            "김치 담글 때나 국물 요리의 감칠맛을 더해주는 프리미엄 액젓입니다.",
            12000, null, "etc", false, false, "1kg / 멸치액젓 / 국산 멸치",
            "/uploads/tenants/mohyun/products/fish_sauce.png",
            Specs(@"{""features"":[""국내산 멸치와 천일염 사용"",""2년 이상 장기 숙성으로 비린내 제거"",""맑고 투명한 빛깔"",""김치 담그기에 필수""],""specs"":{""volume"":""1kg"",""type"":""멸치액젓"",""ingredients"":""멸치(국산) 75%, 천일염(국산) 25%"",""storage"":""서늘한 곳 보관""}}"))
    ];
}
