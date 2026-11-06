using MediatR;

namespace Reviews.Application.Common.Interfaces;

public interface IQuery<out TResponse> : IRequest<TResponse>
{
}
