using Reviews.Application.Common.Models;
using Reviews.Application.DTOs;

namespace Reviews.Application.Common.Interfaces;

public interface IReviewQueries
{
    Task<ReviewDto?> GetByIdAsync(string id, CancellationToken ct = default);
    Task<PagedResult<ReviewDto>> GetListAsync(long? productId, int page, int pageSize, CancellationToken ct = default);
}
