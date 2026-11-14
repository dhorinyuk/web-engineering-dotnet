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
    DateTime CreatedAt)
{
    public string Id => CommentId;
    public string Text => CommentText;
}

public record ReviewDto
{
    public string Id { get; set; } = "";
    public long ProductId { get; set; }
    public string ProductName { get; set; } = "";
    public string AuthorName { get; set; } = "";
    public int Rating { get; set; }
    public string Text { get; set; } = "";
    public int CommentsCount { get; set; }
    public int Version { get; set; }

    public ReviewDto()
    {
    }

    public ReviewDto(
        string id,
        long productId,
        string productName,
        string authorName,
        int rating,
        string text,
        int commentsCount,
        int version)
    {
        Id = id;
        ProductId = productId;
        ProductName = productName;
        AuthorName = authorName;
        Rating = rating;
        Text = text;
        CommentsCount = commentsCount;
        Version = version;
    }
}
