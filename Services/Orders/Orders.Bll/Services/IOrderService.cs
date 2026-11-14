using Orders.Bll.DTOs;

namespace Orders.Bll.Services;

public interface IOrderService
{
    Task<OrderDto> GetByIdAsync(long orderId, CancellationToken ct = default);
    Task<OrderDto> CreateAsync(CreateOrderDto dto, CancellationToken ct = default);
    Task ConfirmOrderAsync(long orderId, CancellationToken ct = default);
}
