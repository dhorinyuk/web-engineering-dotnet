using MongoDB.Bson;
using Reviews.Domain.Common;
using Reviews.Domain.Exceptions;
using Reviews.Domain.ValueObjects;

namespace Reviews.Domain.Entities;

public class Review : BaseEntity
{
    private readonly List<Comment> _comments = new();
    private readonly List<string> _photos = new();

    public long ProductId { get; private set; }
    public string ProductName { get; private set; } = null!;
    public int Rating { get; private set; }
    public string Headline { get; private set; } = null!;
    public string Content { get; private set; } = null!;
    public Author Author { get; private set; } = null!;
    public ObjectId? DiscussionId { get; private set; }

    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();
    public IReadOnlyCollection<string> Photos => _photos.AsReadOnly();

    // Private constructor for MongoDB BSON deserialization
    private Review()
    {
    }

    public Review(
        long productId,
        string productName,
        int rating,
        string headline,
        string content,
        Author author,
        ObjectId? discussionId = null,
        IEnumerable<string>? photos = null)
    {
        ValidateInvariants(productId, productName, rating, headline, content, author);

        ProductId = productId;
        ProductName = productName.Trim();
        Rating = rating;
        Headline = headline.Trim();
        Content = content.Trim();
        Author = author;
        DiscussionId = discussionId;

        if (photos != null)
        {
            foreach (var photo in photos)
            {
                if (!string.IsNullOrWhiteSpace(photo))
                    _photos.Add(photo.Trim());
            }
        }
    }

    public void UpdateRating(int newRating, string? newHeadline = null, string? newContent = null)
    {
        if (newRating is < 1 or > 5)
            throw new DomainException($"Rating must be between 1 and 5. Provided: {newRating}");

        Rating = newRating;

        if (!string.IsNullOrWhiteSpace(newHeadline))
        {
            if (newHeadline.Trim().Length is < 3 or > 200)
                throw new DomainException("Headline must be between 3 and 200 characters.");
            Headline = newHeadline.Trim();
        }

        if (!string.IsNullOrWhiteSpace(newContent))
        {
            if (newContent.Trim().Length is < 5 or > 4000)
                throw new DomainException("Review content must be between 5 and 4000 characters.");
            Content = newContent.Trim();
        }

        Touch();
    }

    public Comment AddComment(string authorName, string commentText)
    {
        var comment = new Comment(authorName, commentText);
        _comments.Add(comment);
        Touch();
        return comment;
    }

    public void RemoveComment(string commentId)
    {
        if (string.IsNullOrWhiteSpace(commentId))
            throw new DomainException("Comment identifier cannot be empty.");

        var existing = _comments.FirstOrDefault(c => c.CommentId.Equals(commentId, StringComparison.OrdinalIgnoreCase));
        if (existing == null)
            throw new DomainException($"Comment with identifier '{commentId}' does not exist on this review.");

        _comments.Remove(existing);
        Touch();
    }

    public void AddPhoto(string photoUrl)
    {
        if (string.IsNullOrWhiteSpace(photoUrl))
            throw new DomainException("Photo URL cannot be empty.");

        var trimmed = photoUrl.Trim();
        if (_photos.Contains(trimmed))
            return;

        _photos.Add(trimmed);
        Touch();
    }

    public void SetDiscussionId(ObjectId? discussionId)
    {
        DiscussionId = discussionId;
        Touch();
    }

    private static void ValidateInvariants(
        long productId,
        string productName,
        int rating,
        string headline,
        string content,
        Author author)
    {
        if (productId <= 0)
            throw new DomainException("Product identifier must be greater than zero.");

        if (string.IsNullOrWhiteSpace(productName))
            throw new DomainException("Product name cannot be empty.");

        if (productName.Trim().Length is < 2 or > 250)
            throw new DomainException("Product name must be between 2 and 250 characters.");

        if (rating is < 1 or > 5)
            throw new DomainException($"Rating must be between 1 and 5. Provided: {rating}");

        if (string.IsNullOrWhiteSpace(headline))
            throw new DomainException("Review headline cannot be empty.");

        if (headline.Trim().Length is < 3 or > 200)
            throw new DomainException("Review headline must be between 3 and 200 characters.");

        if (string.IsNullOrWhiteSpace(content))
            throw new DomainException("Review content cannot be empty.");

        if (content.Trim().Length is < 5 or > 4000)
            throw new DomainException("Review content must be between 5 and 4000 characters.");

        if (author == null)
            throw new DomainException("Review author cannot be null.");
    }
}
