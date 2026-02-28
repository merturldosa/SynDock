using MediatR;
using Shop.Application.Common.Interfaces;
using SynDock.Core.Common;

namespace Shop.Application.Chat.Commands;

public record ChatMessageRequest(string Role, string Content);

public record SendChatCommand(
    IReadOnlyList<ChatMessageRequest> Messages
) : IRequest<Result<string>>;

public class SendChatCommandHandler : IRequestHandler<SendChatCommand, Result<string>>
{
    private readonly IAIChatProvider _chatProvider;

    public SendChatCommandHandler(IAIChatProvider chatProvider)
    {
        _chatProvider = chatProvider;
    }

    public async Task<Result<string>> Handle(SendChatCommand request, CancellationToken cancellationToken)
    {
        var messages = request.Messages
            .Select(m => new ChatMessage(m.Role, m.Content))
            .ToList();

        var response = await _chatProvider.ChatAsync(messages, cancellationToken: cancellationToken);

        if (response.Error is not null && string.IsNullOrEmpty(response.Content))
            return Result<string>.Failure(response.Error);

        return Result<string>.Success(response.Content);
    }
}
