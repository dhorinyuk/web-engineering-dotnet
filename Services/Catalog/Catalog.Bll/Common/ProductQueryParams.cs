namespace Catalog.Bll.Common;

public class ProductQueryParams
{
    private int _page = 1;
    private int _pageSize = 10;

    public int Page
    {
        get => _page;
        set => _page = Math.Max(1, value);
    }

    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = Math.Clamp(value, 1, 100);
    }

    public string? SearchTerm { get; set; }
    public int? BrandId { get; set; }
    public int? CategoryId { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public bool? IsAvailable { get; set; }

    public string? SortBy { get; set; } // "price", "name", "createdat"
    public bool SortDescending { get; set; }
}
