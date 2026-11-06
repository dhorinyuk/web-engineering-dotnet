using FluentValidation;
using MediatR;
using Reviews.Application.Common.Interfaces;
using Reviews.Application.Common.Models;
using Reviews.Application.DTOs;

namespace Reviews.Application.Features.Reviews.Queries.GetReviewsList;

public record GetReviewsListQuery(
    long? ProductId = null,
    int Page = 1,
    int PageSize = 20) : IQuery<PagedResult<ReviewDto>>;

public class GetReviewsListQueryHandler : IRequestHandler<GetReviewsListQuery, PagedResult<ReviewDto>>
{
    private readonly IReviewQueries _queries;

    public GetReviewsListQueryHandler(IReviewQueries queries)
    {
        _queries = queries;
    }

    public async Task<PagedResult<ReviewDto>> Handle(GetReviewsListQuery request, CancellationToken cancellationToken)
    {
        var page = Math.Max(1, request.Page);
        var pageSize = Math.Clamp(request.PageSize, 1, 100);

        return await _queries.GetListAsync(request.ProductId, page, pageSize, cancellationToken);
    }
}

public class GetReviewsListQueryValidator : AbstractValidator<GetReviewsListQuery>
{
    public GetReviewsListQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 100).WithMessage("Page size must be between 1 and 100.");

        When(x => x.ProductId.HasValue, () =>
        {
            RuleFor(x => x.ProductId!.Value)
                .GreaterThan(0).WithMessage("ProductId must be greater than zero.");
        });
    }
}
