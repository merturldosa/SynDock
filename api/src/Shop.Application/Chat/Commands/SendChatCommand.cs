using MediatR;
using Shop.Application.Common.Interfaces;
using Shop.Application.Liturgy.Services;
using Shop.Domain.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Chat.Commands;

public record ChatMessageRequest(string Role, string Content);

public record SendChatCommand(
    IReadOnlyList<ChatMessageRequest> Messages
) : IRequest<Result<string>>;

public class SendChatCommandHandler : IRequestHandler<SendChatCommand, Result<string>>
{
    private readonly IAIChatProvider _chatProvider;
    private readonly ITenantContext _tenantContext;
    private readonly ILiturgicalCalendarService _liturgyService;

    public SendChatCommandHandler(
        IAIChatProvider chatProvider,
        ITenantContext tenantContext,
        ILiturgicalCalendarService liturgyService)
    {
        _chatProvider = chatProvider;
        _tenantContext = tenantContext;
        _liturgyService = liturgyService;
    }

    public async Task<Result<string>> Handle(SendChatCommand request, CancellationToken cancellationToken)
    {
        var messages = request.Messages
            .Select(m => new AiChatMessage(m.Role, m.Content))
            .ToList();

        string? systemPrompt = null;

        if (_tenantContext.TenantSlug == "catholia")
        {
            var season = _liturgyService.GetCurrentSeason();
            systemPrompt = $"""
                당신은 '가브리엘'이라는 이름의 가톨릭 성물 쇼핑몰 '카톨리아(Catholia)' 도우미입니다.
                대천사 가브리엘처럼 하느님의 메시지를 전하는 친절하고 따뜻한 안내자 역할을 합니다.

                현재 전례 시기: {season.SeasonName} (전례색: {season.LiturgicalColor})

                지침:
                - 존댓말을 사용하고, 따뜻하고 공경하는 가톨릭 어투로 대화하세요.
                - 성물, 묵주, 성경, 성상 등 가톨릭 용품에 대해 전문적으로 안내하세요.
                - 현재 전례 시기에 맞는 상품을 추천할 수 있습니다.
                - "하느님의 은총이 함께하시길" 같은 축복의 말씀을 적절히 사용하세요.
                - 교리 질문에는 간단히 안내하되, 심층 교리는 사제나 수녀님께 여쭤보시길 권하세요.
                """;
        }

        var response = await _chatProvider.ChatAsync(messages, systemPrompt, cancellationToken);

        if (response.Error is not null && string.IsNullOrEmpty(response.Content))
            return Result<string>.Failure(response.Error);

        return Result<string>.Success(response.Content);
    }
}
