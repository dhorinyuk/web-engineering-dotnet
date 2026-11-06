using Reviews.Domain.Exceptions;

namespace Reviews.Domain.Entities;

public class Comment
{
    public string CommentId { get; private set; } = null!;
    public string AuthorName { get; private set; } = null!;
    public string CommentText { get; private set; } = null!;
    public DateTime CreatedAt { get; private set; }

    private Comment()
    {
    }

    public Comment(string authorName, string commentText, string? commentId = null, DateTime? createdAt = null)
    {
        if (string.IsNullOrWhiteSpace(authorName))
            throw new DomainException("Comment author name cannot be empty.");

        if (authorName.Trim().Length is < 2 or > 100)
            throw new DomainException("Comment author name must be between 2 and 100 characters.");

        if (string.IsNullOrWhiteSpace(commentText))
            throw new DomainException("Comment text cannot be empty.");

        if (commentText.Trim().Length is < 2 or > 1000)
            throw new DomainException("Comment text must be between 2 and 1000 characters.");

        CommentId = string.IsNullOrWhiteSpace(commentId)
            ? $"c-{Guid.NewGuid().ToString("N")[..8]}"
            : commentId.Trim();

        AuthorName = authorName.Trim();
        CommentText = commentText.Trim();
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }
}
