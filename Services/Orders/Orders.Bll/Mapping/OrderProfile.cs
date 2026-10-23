using AutoMapper;
using Orders.Bll.DTOs;
using Orders.Domain.Entities;

namespace Orders.Bll.Mapping;

public class OrderProfile : Profile
{
    public OrderProfile()
    {
        CreateMap<Order, OrderDto>();
        CreateMap<OrderItem, OrderItemDto>();
        CreateMap<Customer, CustomerDto>();
        CreateMap<Product, ProductDto>();
    }
}
