namespace Shop.Application.Common.Interfaces;

public record AiChatMessage(string Role, string Content);
public record ChatResponse(string Content, string? Error);

public interface IAIChatProvider
{
    Task<ChatResponse> ChatAsync(
        IReadOnlyList<AiChatMessage> messages,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default);
}
