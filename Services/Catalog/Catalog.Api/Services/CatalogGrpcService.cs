using System;
using System.Threading.Tasks;
using Catalog.Api.Grpc;
using Catalog.Bll.Common;
using Catalog.Bll.Dtos;
using Catalog.Bll.Services;
using Catalog.Domain.Exceptions;
using Grpc.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace Catalog.Api.Services;

public class CatalogGrpcService : CatalogService.CatalogServiceBase
{
    private readonly IProductService _productService;
    private readonly IMemoryCache _cache;
    private readonly ILogger<CatalogGrpcService> _logger;

    public CatalogGrpcService(
        IProductService productService,
        IMemoryCache cache,
        ILogger<CatalogGrpcService> logger)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public override async Task<ProductResponse> GetProductById(GetProductByIdRequest request, ServerCallContext context)
    {
        if (request.Id <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, $"Product ID must be greater than zero. Received: {request.Id}"));
        }

        try
        {
            var product = await _productService.GetProductByIdAsync(request.Id, context.CancellationToken);
            return MapToProductResponse(product);
        }
        catch (EntityNotFoundException ex)
        {
            _logger.LogWarning("Product {ProductId} not found: {Message}", request.Id, ex.Message);
            throw new RpcException(new Status(StatusCode.NotFound, ex.Message));
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid argument for product {ProductId}: {Message}", request.Id, ex.Message);
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
        catch (Exception ex) when (ex is not RpcException)
        {
            _logger.LogError(ex, "Unexpected error retrieving product {ProductId}: {Message}", request.Id, ex.Message);
            throw new RpcException(new Status(StatusCode.Internal, $"An internal error occurred: {ex.Message}"));
        }
    }

    public override async Task<ProductsListResponse> GetProducts(GetProductsRequest request, ServerCallContext context)
    {
        var page = request.Page > 0 ? request.Page : 1;
        var pageSize = request.PageSize > 0 ? request.PageSize : 20;

        try
        {
            var paged = await _productService.GetProductsPagedAsync(new ProductQueryParams
            {
                Page = page,
                PageSize = pageSize
            }, context.CancellationToken);

            var response = new ProductsListResponse
            {
                TotalCount = paged.TotalCount,
                Page = paged.Page,
                PageSize = paged.PageSize
            };

            foreach (var item in paged.Items)
            {
                response.Items.Add(MapToProductResponse(item));
            }

            return response;
        }
        catch (ArgumentException ex)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, ex.Message));
        }
    }

    private static ProductResponse MapToProductResponse(ProductDto dto)
    {
        var response = new ProductResponse
        {
            Id = dto.Id,
            BrandId = dto.BrandId,
            BrandName = dto.BrandName ?? string.Empty,
            Sku = dto.Sku ?? string.Empty,
            Slug = dto.Slug ?? string.Empty,
            Name = dto.Name ?? string.Empty,
            Price = (double)dto.Price,
            CostPrice = (double)dto.CostPrice,
            WeightKg = (double)dto.WeightKg,
            Stock = dto.Stock,
            IsAvailable = dto.IsAvailable,
            CreatedAt = dto.CreatedAt.ToString("O"),
            UpdatedAt = dto.UpdatedAt.HasValue ? dto.UpdatedAt.Value.ToString("O") : string.Empty
        };

        if (dto.Details != null)
        {
            response.Details = new CatalogProductDetails
            {
                TechnicalDescription = dto.Details.TechnicalDescription ?? string.Empty,
                WarrantyMonths = dto.Details.WarrantyMonths,
                CountryOfOrigin = dto.Details.CountryOfOrigin ?? string.Empty,
                Dimensions = dto.Details.Dimensions ?? string.Empty,
                InstallationGuideUrl = dto.Details.InstallationGuideUrl ?? string.Empty
            };
        }

        if (dto.Images != null)
        {
            foreach (var img in dto.Images)
            {
                response.Images.Add(new CatalogProductImage
                {
                    Id = img.Id,
                    Url = img.Url ?? string.Empty,
                    DisplayOrder = img.DisplayOrder,
                    IsMain = img.IsPrimary
                });
            }
        }

        if (dto.Categories != null)
        {
            foreach (var cat in dto.Categories)
            {
                response.Categories.Add(new CatalogProductCategory
                {
                    CategoryId = cat.CategoryId,
                    CategoryName = cat.CategoryName ?? string.Empty,
                    CategorySlug = cat.CategorySlug ?? string.Empty,
                    IsPrimary = cat.IsPrimary,
                    AddedAt = cat.AddedAt.ToString("O")
                });
            }
        }

        return response;
    }
}
