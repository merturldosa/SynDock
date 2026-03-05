using MediatR;
using Microsoft.EntityFrameworkCore;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Products.Commands;

public record GeneratedContent(string HeroSection, string FeatureSection, string ClosingSection, string FullDescription);

public record GenerateProductContentCommand(int ProductId) : IRequest<Result<GeneratedContent>>;

public class GenerateProductContentCommandHandler : IRequestHandler<GenerateProductContentCommand, Result<GeneratedContent>>
{
    private readonly IShopDbContext _db;
    private readonly IAIChatProvider _ai;

    public GenerateProductContentCommandHandler(IShopDbContext db, IAIChatProvider ai)
    {
        _db = db;
        _ai = ai;
    }

    public async Task<Result<GeneratedContent>> Handle(GenerateProductContentCommand request, CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .Include(p => p.Category)
            .FirstOrDefaultAsync(p => p.Id == request.ProductId, cancellationToken);

        if (product is null)
            return Result<GeneratedContent>.Failure("Product not found.");

        var systemPrompt = @"당신은 전문 쇼핑몰 상세 페이지 콘텐츠 작성자입니다.
주어진 상품 정보를 바탕으로 매력적인 상세 페이지 콘텐츠를 작성해주세요.
반드시 아래 형식으로 작성하세요:

[HERO]
상단 섹션: 상품의 핵심 가치와 임팩트 있는 헤드카피
[/HERO]

[FEATURES]
중단 섹션: 상품의 주요 특징, 장점, 사용법 등 상세 설명
[/FEATURES]

[CLOSING]
하단 섹션: 구매 유도 CTA, 보증/안심 문구
[/CLOSING]";

        var userMessage = $"상품명: {product.Name}\n카테고리: {product.Category?.Name ?? "미분류"}\n현재 설명: {product.Description ?? "없음"}\n규격: {product.Specification ?? "없음"}\n\n이 상품의 상세 페이지를 상단(Hero)/중단(Features)/하단(CTA) 3섹션으로 작성해주세요.";

        var messages = new List<ChatMessage>
        {
            new("user", userMessage)
        };

        var response = await _ai.ChatAsync(messages, systemPrompt, cancellationToken);

        if (response.Error is not null)
            return Result<GeneratedContent>.Failure($"AI content generation failed: {response.Error}");

        var content = response.Content;
        var hero = ExtractSection(content, "HERO");
        var features = ExtractSection(content, "FEATURES");
        var closing = ExtractSection(content, "CLOSING");

        var fullDescription = $"{hero}\n\n{features}\n\n{closing}";

        return Result<GeneratedContent>.Success(new GeneratedContent(hero, features, closing, fullDescription));
    }

    private static string ExtractSection(string content, string tag)
    {
        var startTag = $"[{tag}]";
        var endTag = $"[/{tag}]";
        var startIdx = content.IndexOf(startTag, StringComparison.OrdinalIgnoreCase);
        var endIdx = content.IndexOf(endTag, StringComparison.OrdinalIgnoreCase);

        if (startIdx < 0 || endIdx < 0 || endIdx <= startIdx)
            return content;

        return content[(startIdx + startTag.Length)..endIdx].Trim();
    }
}
