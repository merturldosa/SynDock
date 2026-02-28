namespace Shop.Application.Common.Interfaces;

public record ChatMessage(string Role, string Content);
public record ChatResponse(string Content, string? Error);

public interface IAIChatProvider
{
    Task<ChatResponse> ChatAsync(
        IReadOnlyList<ChatMessage> messages,
        string? systemPrompt = null,
        CancellationToken cancellationToken = default);
}
