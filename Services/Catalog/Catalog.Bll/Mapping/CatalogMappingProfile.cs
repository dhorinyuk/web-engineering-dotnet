using AutoMapper;
using Catalog.Bll.Dtos;
using Catalog.Domain.Entities;

namespace Catalog.Bll.Mapping;

public class CatalogMappingProfile : Profile
{
    public CatalogMappingProfile()
    {
        CreateMap<Brand, BrandDto>();
        CreateMap<Category, CategoryDto>();

        CreateMap<ProductDetails, ProductDetailsDto>();
        CreateMap<ProductImage, ProductImageDto>();

        CreateMap<ProductCategory, ProductCategoryItemDto>()
            .ForMember(d => d.CategoryId, opt => opt.MapFrom(s => s.CategoryId))
            .ForMember(d => d.CategoryName, opt => opt.MapFrom(s => s.Category != null ? s.Category.Name : string.Empty))
            .ForMember(d => d.CategorySlug, opt => opt.MapFrom(s => s.Category != null ? s.Category.Slug : string.Empty))
            .ForMember(d => d.IsPrimary, opt => opt.MapFrom(s => s.IsPrimary))
            .ForMember(d => d.AddedAt, opt => opt.MapFrom(s => s.AddedAt));

        CreateMap<Product, ProductDto>()
            .ForMember(d => d.BrandName, opt => opt.MapFrom(s => s.Brand != null ? s.Brand.Name : null))
            .ForMember(d => d.Details, opt => opt.MapFrom(s => s.Details))
            .ForMember(d => d.Images, opt => opt.MapFrom(s => s.Images.OrderBy(i => i.DisplayOrder)))
            .ForMember(d => d.Categories, opt => opt.MapFrom(s => s.ProductCategories));
    }
}
