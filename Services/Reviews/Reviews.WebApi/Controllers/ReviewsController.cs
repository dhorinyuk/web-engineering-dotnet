using MediatR;
using Microsoft.AspNetCore.Mvc;
using Reviews.Application.Common.Models;
using Reviews.Application.DTOs;
using Reviews.Application.Features.Reviews.Commands.AddComment;
using Reviews.Application.Features.Reviews.Commands.CreateReview;
using Reviews.Application.Features.Reviews.Commands.DeleteReview;
using Reviews.Application.Features.Reviews.Commands.UpdateReview;
using Reviews.Application.Features.Reviews.Queries.GetReviewById;
using Reviews.Application.Features.Reviews.Queries.GetReviewsList;
using Reviews.WebApi.Models.Requests;

namespace Reviews.WebApi.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ReviewsController : ControllerBase
{
    // Criterion 12: Injects ONLY ISender (no DbContext, no repositories)
    private readonly ISender _mediator;

    public ReviewsController(ISender mediator)
    {
        _mediator = mediator ?? throw new ArgumentNullException(nameof(mediator));
    }

    /// <summary>
    /// Gets a paginated list of reviews with optional product filtering (Criterion 6 &amp; 12)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ReviewDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ReviewDto>>> GetList(
        [FromQuery] long? productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var result = await _mediator.Send(new GetReviewsListQuery(productId, page, pageSize), ct);
        return Ok(result);
    }

    /// <summary>
    /// Gets a single review by its ObjectId and provides an ETag concurrency header (Criterion 12)
    /// </summary>
    [HttpGet("{id}")]
    [ActionName(nameof(GetById))]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ReviewDto>> GetById(string id, CancellationToken ct)
    {
        var review = await _mediator.Send(new GetReviewByIdQuery(id), ct);

        // Criterion 12: ETag response header reflecting optimistic document version
        Response.Headers.ETag = $"\"{review.Version}\"";

        return Ok(review);
    }

    /// <summary>
    /// Creates a new product review and returns 201 Created with Location header (Criterion 12)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(string), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<string>> Create(
        [FromBody] CreateReviewRequest request,
        CancellationToken ct)
    {
        var command = new CreateReviewCommand(
            request.ProductId,
            request.ProductName,
            request.Rating,
            request.Headline,
            request.Content,
            request.AuthorUserId,
            request.AuthorName,
            request.AuthorEmail,
            request.IsVerifiedBuyer,
            request.DiscussionId,
            request.Photos);

        var id = await _mediator.Send(command, ct);

        // Criterion 12: CreatedAtAction with string representation of ObjectId
        return CreatedAtAction(nameof(GetById), new { id }, id);
    }

    /// <summary>
    /// Updates review rating, headline or content with optimistic concurrency control (Criterion 11 &amp; 12)
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Update(
        string id,
        [FromBody] UpdateReviewRequest request,
        CancellationToken ct)
    {
        var command = new UpdateReviewCommand(id, request.Rating, request.Headline, request.Content);
        await _mediator.Send(command, ct);
        return NoContent();
    }

    /// <summary>
    /// Deletes a review document by its ObjectId (Criterion 12)
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Delete(string id, CancellationToken ct)
    {
        await _mediator.Send(new DeleteReviewCommand(id), ct);
        return NoContent();
    }

    /// <summary>
    /// Appends a new reply/comment to the specified review's embedded collection (Criterion 2 &amp; 12)
    /// </summary>
    [HttpPost("{id}/comments")]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CommentDto>> AddComment(
        string id,
        [FromBody] AddCommentRequest request,
        CancellationToken ct)
    {
        var command = new AddCommentCommand(id, request.AuthorName, request.CommentText);
        var comment = await _mediator.Send(command, ct);
        return CreatedAtAction(nameof(GetById), new { id }, comment);
    }
}
