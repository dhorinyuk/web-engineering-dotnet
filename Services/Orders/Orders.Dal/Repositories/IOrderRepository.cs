using Orders.Domain.Entities;

namespace Orders.Dal.Repositories;

public interface IOrderRepository : IGenericRepository<Order>
{
    Task<Order?> GetWithItemsAsync(long orderId, CancellationToken ct = default);
    Task ConfirmAsync(long orderId, CancellationToken ct = default);
    Task AddOrderItemAsync(OrderItem item, CancellationToken ct = default);
    Task AddOrderDetailsAsync(OrderDetails details, CancellationToken ct = default);
}
