using System;
using System.Collections.Generic;

namespace Platform.Aggregator.Models;

public record CatalogProductDto(
    int Id,
    int BrandId,
    string? BrandName,
    string Sku,
    string Slug,
    string Name,
    decimal Price,
    decimal CostPrice,
    decimal WeightKg,
    int Stock,
    bool IsAvailable,
    DateTime CreatedAt,
    DateTime? UpdatedAt,
    CatalogProductDetailsDto? Details,
    List<CatalogProductImageDto>? Images,
    List<CatalogProductCategoryDto>? Categories);

public record CatalogProductDetailsDto(
    string TechnicalDescription,
    int WarrantyMonths,
    string CountryOfOrigin,
    string? Dimensions,
    string? InstallationGuideUrl);

public record CatalogProductImageDto(
    int Id,
    string Url,
    int DisplayOrder,
    bool IsMain);

public record CatalogProductCategoryDto(
    int CategoryId,
    string CategoryName,
    string CategorySlug,
    bool IsPrimary,
    DateTime AddedAt);

public record ReviewItemDto(
    string Id,
    long ProductId,
    string ProductName,
    string AuthorName,
    int Rating,
    string Text,
    int CommentsCount,
    int Version);

public record ReviewsPagedResult(
    List<ReviewItemDto> Items,
    int TotalCount,
    int Page,
    int PageSize);

public record ReviewsSummaryDto(
    long ProductId,
    int TotalReviews,
    double AverageRating,
    List<ReviewItemDto> RecentReviews);

public record OrderItemDto(
    long ProductId,
    string ProductName,
    decimal UnitPrice,
    int Units,
    decimal SubTotal);

public record OrderDto(
    long Id,
    string OrderNumber,
    long CustomerId,
    string Status,
    decimal TotalAmount,
    DateTime CreatedAt,
    List<OrderItemDto> Items);

public class ProductAggregateResponse
{
    public int ProductId { get; set; }
    public CatalogProductDto? Product { get; set; }
    public ReviewsSummaryDto? Reviews { get; set; }
    public List<string> Warnings { get; set; } = new();
}

public class OrderAggregateResponse
{
    public long OrderId { get; set; }
    public OrderDto? Order { get; set; }
    public List<ProductAggregateResponse> ProductsDetails { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}
