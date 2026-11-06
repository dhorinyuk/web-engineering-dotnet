namespace Reviews.Application.DTOs;

public record AuthorDto(
    string? UserId,
    string Name,
    string Email,
    bool IsVerifiedBuyer);

public record CommentDto(
    string CommentId,
    string AuthorName,
    string CommentText,
    DateTime CreatedAt);

public record ReviewDto(
    string Id,
    long ProductId,
    string ProductName,
    int Rating,
    string Headline,
    string Content,
    AuthorDto Author,
    string? DiscussionId,
    IReadOnlyList<CommentDto> Comments,
    IReadOnlyList<string> Photos,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    int Version);
