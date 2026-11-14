using System;
using System.Threading.Tasks;
using Grpc.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Orders.Api.Grpc;
using Orders.Bll.DTOs;
using Orders.Bll.Services;
using Orders.Domain.Exceptions;

namespace Orders.Api.Services;

public class OrdersGrpcService : OrdersService.OrdersServiceBase
{
    private readonly IOrderService _orderService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<OrdersGrpcService> _logger;

    public OrdersGrpcService(
        IOrderService orderService,
        IMemoryCache cache,
        ILogger<OrdersGrpcService> logger)
    {
        _orderService = orderService ?? throw new ArgumentNullException(nameof(orderService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<OrderResponse> GetOrderById(GetOrderByIdRequest request, ServerCallContext context)
    {
        if (request.Id <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Order ID must be greater than zero. Received: {request.Id}"));
        }

        try
        {
            var order = await _orderService.GetByIdAsync(request.Id, context.CancellationToken);
            return MapToOrderResponse(order);
        }
        catch (NotFoundException ex)
        {
            _logger.LogWarning("Order {OrderId} not found: {Message}", request.Id, ex.Message);
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid argument for order {OrderId}: {Message}", request.Id, ex.Message);
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            _logger.LogError(ex, "Unexpected error retrieving order {OrderId}: {Message}", request.Id, ex.Message);
            throw new RpcException(new Status(StatusCode.Internal, $"An internal error occurred: {ex.Message}"));
        }
    }

    private static OrderResponse MapToOrderResponse(OrderDto dto)
    {
        var response = new OrderResponse
        {
            Id = dto.Id,
            OrderNumber = dto.OrderNumber ?? string.Empty,
            CustomerId = dto.CustomerId,
            Status = dto.Status ?? string.Empty,
            TotalAmount = (double)dto.TotalAmount,
            CreatedAt = dto.CreatedAt.ToString("O")
        };

        if (dto.Items != null)
        {
            foreach (var item in dto.Items)
            {
                response.Items.Add(new OrderItemResponse
                {
                    ProductId = item.ProductId,
                    ProductName = item.ProductName ?? string.Empty,
                    UnitPrice = (double)item.UnitPrice,
                    Units = item.Units,
                    SubTotal = (double)item.SubTotal
                });
            }
        }

        return response;
    }
}
