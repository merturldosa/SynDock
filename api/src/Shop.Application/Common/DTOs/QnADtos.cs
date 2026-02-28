namespace Shop.Application.Common.DTOs;

public record QnADto(
    int Id,
    int ProductId,
    int UserId,
    string UserName,
    string Title,
    string Content,
    bool IsAnswered,
    bool IsSecret,
    QnAReplyDto? Reply,
    DateTime CreatedAt);

public record QnAReplyDto(
    int Id,
    int UserId,
    string UserName,
    string Content,
    DateTime CreatedAt);
