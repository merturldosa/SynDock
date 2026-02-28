namespace Shop.Application.Common.DTOs;

public record PostDto(
    int Id,
    int UserId,
    string UserName,
    string? Title,
    string Content,
    string PostType,
    int? ProductId,
    string? ProductName,
    int ViewCount,
    int ReactionCount,
    int CommentCount,
    IReadOnlyList<PostImageDto> Images,
    IReadOnlyList<string> Hashtags,
    IReadOnlyList<PostCommentDto>? Comments,
    string? MyReaction,
    DateTime CreatedAt);

public record PostImageDto(
    int Id,
    string Url,
    string? AltText,
    int SortOrder);

public record PostCommentDto(
    int Id,
    int UserId,
    string UserName,
    string Content,
    int? ParentId,
    IReadOnlyList<PostCommentDto>? Replies,
    DateTime CreatedAt);

public record PostSummaryDto(
    int Id,
    int UserId,
    string UserName,
    string? Title,
    string ContentPreview,
    string PostType,
    string? ThumbnailUrl,
    int ReactionCount,
    int CommentCount,
    IReadOnlyList<string> Hashtags,
    DateTime CreatedAt);

public record PagedPostResult(
    int TotalCount,
    int Page,
    int PageSize,
    IReadOnlyList<PostSummaryDto> Items);

public record FollowDto(
    int UserId,
    string UserName,
    string? Name,
    DateTime FollowedAt);

public record SocialProfileDto(
    int UserId,
    string UserName,
    string? Name,
    int PostCount,
    int FollowerCount,
    int FollowingCount,
    bool IsFollowing);

public record HashtagDto(
    int Id,
    string Tag,
    int PostCount);
