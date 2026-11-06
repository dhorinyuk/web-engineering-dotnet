using System.ComponentModel.DataAnnotations;

namespace Reviews.WebApi.Models.Requests;

public class CreateReviewRequest
{
    [Required]
    [Range(1, long.MaxValue, ErrorMessage = "ProductId must be greater than zero.")]
    public long ProductId { get; set; }

    [Required]
    [StringLength(250, MinimumLength = 2)]
    public string ProductName { get; set; } = null!;

    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int Rating { get; set; }

    [Required]
    [StringLength(200, MinimumLength = 3)]
    public string Headline { get; set; } = null!;

    [Required]
    [StringLength(4000, MinimumLength = 5)]
    public string Content { get; set; } = null!;

    public string? AuthorUserId { get; set; }

    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string AuthorName { get; set; } = null!;

    [Required]
    [EmailAddress]
    [StringLength(255)]
    public string AuthorEmail { get; set; } = null!;

    public bool IsVerifiedBuyer { get; set; }

    public string? DiscussionId { get; set; }

    public List<string>? Photos { get; set; }
}

public class UpdateReviewRequest
{
    [Required]
    [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5.")]
    public int Rating { get; set; }

    [StringLength(200, MinimumLength = 3)]
    public string? Headline { get; set; }

    [StringLength(4000, MinimumLength = 5)]
    public string? Content { get; set; }
}

public class AddCommentRequest
{
    [Required]
    [StringLength(100, MinimumLength = 2)]
    public string AuthorName { get; set; } = null!;

    [Required]
    [StringLength(1000, MinimumLength = 2)]
    public string CommentText { get; set; } = null!;
}
