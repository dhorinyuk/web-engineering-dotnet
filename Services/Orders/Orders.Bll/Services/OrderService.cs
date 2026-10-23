using System.Data;
using AutoMapper;
using Microsoft.Data.SqlClient;
using Orders.Bll.DTOs;
using Orders.Dal;
using Orders.Domain.Entities;
using Orders.Domain.Exceptions;

namespace Orders.Bll.Services;

public class OrderService : IOrderService
{
    private readonly IUnitOfWork _uow;
    private readonly IMapper _mapper;

    public OrderService(IUnitOfWork uow, IMapper mapper)
    {
        _uow = uow ?? throw new ArgumentNullException(nameof(uow));
        _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
    }

    public async Task<OrderDto> GetByIdAsync(long orderId, CancellationToken ct = default)
    {
        var order = await _uow.Orders.GetWithItemsAsync(orderId, ct);
        if (order == null)
        {
            throw new NotFoundException(nameof(Order), orderId);
        }

        return _mapper.Map<OrderDto>(order);
    }

    public async Task<OrderDto> CreateAsync(CreateOrderDto dto, CancellationToken ct = default)
    {
        if (dto == null)
            throw new ArgumentNullException(nameof(dto));

        // Рівень 2 бізнес-валідації: перевірка клієнта в БД
        var customer = await _uow.Customers.GetByIdAsync(dto.CustomerId, ct);
        if (customer == null)
        {
            throw new NotFoundException(nameof(Customer), dto.CustomerId);
        }

        // Рівень 2 бізнес-валідації: перевірка наявності кожного товару та фіксація незмінних знімків (Price Snapshotting)
        var orderItems = new List<OrderItem>();
        decimal totalAmount = 0;

        foreach (var itemDto in dto.Items)
        {
            var product = await _uow.Products.GetByIdAsync(itemDto.ProductId, ct);
            if (product == null)
            {
                throw new NotFoundException(nameof(Product), itemDto.ProductId);
            }

            if (!product.IsAvailable)
            {
                throw new BusinessConflictException($"Product '{product.Name}' (ID {product.Id}) is currently not available for purchase.");
            }

            if (product.Stock < itemDto.Units)
            {
                throw new BusinessConflictException($"Insufficient stock for product '{product.Name}'. Available: {product.Stock}, Requested: {itemDto.Units}.");
            }

            var subTotal = product.Price * itemDto.Units;
            totalAmount += subTotal;

            orderItems.Add(new OrderItem
            {
                ProductId = product.Id,
                ProductName = product.Name,
                UnitPrice = product.Price,
                Units = itemDto.Units,
                SubTotal = subTotal,
                CreatedAt = DateTime.UtcNow,
                CreatedBy = "API"
            });
        }

        var order = new Order
        {
            OrderNumber = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpperInvariant()}",
            CustomerId = dto.CustomerId,
            Status = "Submitted",
            TotalAmount = totalAmount,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = "API",
            Items = orderItems
        };

        await _uow.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            var orderId = await _uow.Orders.AddAsync(order, ct);
            order.Id = orderId;

            foreach (var item in order.Items)
            {
                item.OrderId = orderId;
                await _uow.Orders.AddOrderItemAsync(item, ct);
            }

            if (!string.IsNullOrWhiteSpace(dto.ShippingAddress))
            {
                var details = new OrderDetails
                {
                    OrderId = orderId,
                    RecipientName = string.IsNullOrWhiteSpace(dto.RecipientName) ? customer.FullName : dto.RecipientName,
                    RecipientPhone = string.IsNullOrWhiteSpace(dto.RecipientPhone) ? customer.Phone : dto.RecipientPhone,
                    ShippingAddress = dto.ShippingAddress,
                    City = string.IsNullOrWhiteSpace(dto.City) ? "Київ" : dto.City,
                    PostalCode = dto.PostalCode,
                    ShippingMethod = "NovaPoshta",
                    CreatedAt = DateTime.UtcNow,
                    CreatedBy = "API"
                };
                await _uow.Orders.AddOrderDetailsAsync(details, ct);
                order.Details = details;
            }

            await _uow.CommitAsync(ct);
        }
        catch (SqlException ex) when (ex.Number == 50001)
        {
            await _uow.RollbackAsync(ct);
            throw new NotFoundException(nameof(Order), order.Id);
        }
        catch (SqlException ex) when (ex.Number == 50002)
        {
            await _uow.RollbackAsync(ct);
            throw new BusinessConflictException(ex.Message);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            await _uow.RollbackAsync(ct);
            throw new BusinessConflictException($"Unique constraint violation: {ex.Message}");
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }

        return _mapper.Map<OrderDto>(order);
    }

    public async Task ConfirmOrderAsync(long orderId, CancellationToken ct = default)
    {
        await _uow.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
        try
        {
            // Отримання замовлення з позиціями
            var order = await _uow.Orders.GetWithItemsAsync(orderId, ct);
            if (order == null)
            {
                throw new NotFoundException(nameof(Order), orderId);
            }

            // Перевірка наявності позицій у замовленні
            if (order.Items == null || order.Items.Count == 0)
            {
                throw new BusinessConflictException($"Cannot confirm order {orderId} because it contains no items.");
            }

            if (order.Status is "StockConfirmed" or "Paid" or "Shipped")
            {
                throw new BusinessConflictException($"Order {orderId} has already been confirmed/processed (Status: {order.Status}).");
            }

            if (order.Status == "Cancelled")
            {
                throw new BusinessConflictException($"Cannot confirm cancelled order {orderId}.");
            }

            // 1. Виклик зміни статусу через збережену процедуру з row-locking
            await _uow.Orders.ConfirmAsync(orderId, ct);

            // 2. Зменшення складських залишків для кожної позиції
            foreach (var item in order.Items)
            {
                await _uow.Products.DecreaseStockAsync(item.ProductId, item.Units, ct);
            }

            // 3. Фіксація транзакції
            await _uow.CommitAsync(ct);
        }
        catch (SqlException ex) when (ex.Number == 50001)
        {
            await _uow.RollbackAsync(ct);
            throw new NotFoundException(nameof(Order), orderId);
        }
        catch (SqlException ex) when (ex.Number == 50002)
        {
            await _uow.RollbackAsync(ct);
            throw new BusinessConflictException(ex.Message);
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            await _uow.RollbackAsync(ct);
            throw new BusinessConflictException($"Unique constraint violation: {ex.Message}");
        }
        catch
        {
            await _uow.RollbackAsync(ct);
            throw;
        }
    }
}
