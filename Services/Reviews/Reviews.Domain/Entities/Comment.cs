using MongoDB.Bson;
using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;
using Reviews.Domain.ValueObjects;

namespace Reviews.Domain.Entities;

public class Comment : BaseEntity
{
    public Author Author { get; private set; } = null!;
    public string Text { get; private set; } = "";

    // Backward-compatibility properties for existing mappings / seeder
    public string CommentId { get; private set; } = "";
    public string AuthorName { get; private set; } = "";
    public string CommentText { get; private set; } = "";

    private Comment()
    {
    }

    public Comment(Author author, string text)
    {
        if (author == null)
            throw new DomainException("Comment author cannot be null.");

        if (string.IsNullOrWhiteSpace(text))
            throw new DomainException("Comment text cannot be empty.");

        if (text.Trim().Length is < 2 or > 1000)
            throw new DomainException("Comment text must be between 2 and 1000 characters.");

        Author = author;
        Text = text.Trim();
        CommentId = Id.ToString();
        AuthorName = author.DisplayName;
        CommentText = Text;
    }

    public Comment(string authorName, string commentText, string? commentId = null, DateTime? createdAt = null)
        : this(new Author(null, authorName, $"{authorName.Trim().Replace(" ", "").ToLowerInvariant()}@example.com"), commentText)
    {
        if (!string.IsNullOrWhiteSpace(commentId) && ObjectId.TryParse(commentId, out var parsedId))
        {
            Id = parsedId;
            CommentId = commentId.Trim();
        }

        if (createdAt.HasValue)
        {
            CreatedAt = createdAt.Value;
        }
    }
}
