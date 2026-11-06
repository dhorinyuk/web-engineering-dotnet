using Reviews.Application.Common.Interfaces;

namespace Reviews.Application.Features.Reviews.Commands.CreateReview;

public record CreateReviewCommand(
    long ProductId,
    string ProductName,
    int Rating,
    string Text,
    string? AuthorUserId,
    string AuthorName,
    string AuthorEmail,
    bool IsVerifiedBuyer = false,
    string? DiscussionId = null,
    List<string>? Photos = null) : ICommand<string>
{
    public string Headline => !string.IsNullOrWhiteSpace(Text) && Text.Length > 50 ? Text[..50] : Text;
    public string Content => Text;

    public CreateReviewCommand(
        long productId,
        string productName,
        int rating,
        string headline,
        string content,
        string? authorUserId,
        string authorName,
        string authorEmail,
        bool isVerifiedBuyer = false,
        string? discussionId = null,
        List<string>? photos = null)
        : this(productId, productName, rating, !string.IsNullOrWhiteSpace(content) ? content : (headline ?? ""), authorUserId, authorName, authorEmail, isVerifiedBuyer, discussionId, photos)
    {
    }
}
