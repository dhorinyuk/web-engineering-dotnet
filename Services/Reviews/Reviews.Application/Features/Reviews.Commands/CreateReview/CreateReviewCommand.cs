using Reviews.Application.Common.Interfaces;

namespace Reviews.Application.Features.Reviews.Commands.CreateReview;

public record CreateReviewCommand(
    long ProductId,
    string ProductName,
    int Rating,
    string Headline,
    string Content,
    string? AuthorUserId,
    string AuthorName,
    string AuthorEmail,
    bool IsVerifiedBuyer = false,
    string? DiscussionId = null,
    List<string>? Photos = null) : ICommand<string>;
