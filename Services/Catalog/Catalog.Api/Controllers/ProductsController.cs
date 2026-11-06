using Catalog.Bll.Common;
using Catalog.Bll.Dtos;
using Catalog.Bll.Services;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace Catalog.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class ProductsController : ControllerBase
{
    private readonly IProductService _productService;
    private readonly IValidator<ProductQueryParams> _queryValidator;
    private readonly IValidator<CreateProductDto> _createValidator;
    private readonly IValidator<UpdateProductDto> _updateValidator;
    private readonly IValidator<UpdatePriceDto> _priceValidator;
    private readonly IValidator<UpdateStockDto> _stockValidator;

    public ProductsController(
        IProductService productService,
        IValidator<ProductQueryParams> queryValidator,
        IValidator<CreateProductDto> createValidator,
        IValidator<UpdateProductDto> updateValidator,
        IValidator<UpdatePriceDto> priceValidator,
        IValidator<UpdateStockDto> stockValidator)
    {
        _productService = productService ?? throw new ArgumentNullException(nameof(productService));
        _queryValidator = queryValidator ?? throw new ArgumentNullException(nameof(queryValidator));
        _createValidator = createValidator ?? throw new ArgumentNullException(nameof(createValidator));
        _updateValidator = updateValidator ?? throw new ArgumentNullException(nameof(updateValidator));
        _priceValidator = priceValidator ?? throw new ArgumentNullException(nameof(priceValidator));
        _stockValidator = stockValidator ?? throw new ArgumentNullException(nameof(stockValidator));
    }

    /// <summary>
    /// Gets a paginated, filtered, and sorted list of products via Specification pattern at SQL level (Criteria 10 &amp; 14)
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PagedResult<ProductDto>>> GetPaged(
        [FromQuery] ProductQueryParams query,
        CancellationToken ct)
    {
        var validation = await _queryValidator.ValidateAsync(query, ct);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var result = await _productService.GetProductsPagedAsync(query, ct);
        return Ok(result);
    }

    /// <summary>
    /// Gets a single product by Id with all related entities in a single SQL query via Eager Loading (Criteria 6 &amp; 8)
    /// </summary>
    [HttpGet("{id:int}")]
    [ActionName(nameof(GetById))]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetById(int id, CancellationToken ct)
    {
        var product = await _productService.GetProductByIdAsync(id, ct);
        return Ok(product);
    }

    /// <summary>
    /// Demonstrates Explicit Loading: loads root product then explicitly loads images collection on-demand (Criterion 6)
    /// </summary>
    [HttpGet("{id:int}/explicit")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ProductDto>> GetWithExplicitImages(int id, CancellationToken ct)
    {
        var product = await _productService.GetProductWithExplicitImagesAsync(id, ct);
        return Ok(product);
    }

    /// <summary>
    /// Demonstrates LINQ to Entities M:N navigation query filtered and sorted by junction payload AddedAt (Criterion 6)
    /// </summary>
    [HttpGet("categories/{categoryId:int}/recent")]
    [ProducesResponseType(typeof(IReadOnlyList<ProductDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<ProductDto>>> GetRecentCategoryProducts(
        int categoryId,
        [FromQuery] int days = 30,
        CancellationToken ct = default)
    {
        var products = await _productService.GetRecentCategoryProductsAsync(categoryId, days, ct);
        return Ok(products);
    }

    /// <summary>
    /// Creates a new product with business validations, UoW and returns 201 Created with Location header (Criteria 7, 8, 11)
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> Create(
        [FromBody] CreateProductDto dto,
        CancellationToken ct)
    {
        var validation = await _createValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var result = await _productService.CreateProductAsync(dto, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Updates product details and specifications (Criterion 8)
    /// </summary>
    [HttpPut("{id:int}")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> Update(
        int id,
        [FromBody] UpdateProductDto dto,
        CancellationToken ct)
    {
        var validation = await _updateValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var result = await _productService.UpdateProductAsync(id, dto, ct);
        return Ok(result);
    }

    /// <summary>
    /// Updates product price using domain method (Criteria 1 &amp; 8)
    /// </summary>
    [HttpPatch("{id:int}/price")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> UpdatePrice(
        int id,
        [FromBody] UpdatePriceDto dto,
        CancellationToken ct)
    {
        var validation = await _priceValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var result = await _productService.UpdatePriceAsync(id, dto, ct);
        return Ok(result);
    }

    /// <summary>
    /// Adds or removes stock using domain business methods (Criteria 1 &amp; 8)
    /// </summary>
    [HttpPatch("{id:int}/stock")]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ProductDto>> UpdateStock(
        int id,
        [FromBody] UpdateStockDto dto,
        CancellationToken ct)
    {
        var validation = await _stockValidator.ValidateAsync(dto, ct);
        if (!validation.IsValid)
        {
            return ValidationProblem(new ValidationProblemDetails(validation.ToDictionary()));
        }

        var result = await _productService.UpdateStockAsync(id, dto, ct);
        return Ok(result);
    }

    /// <summary>
    /// Deletes a product and cascades linked details (Criterion 8)
    /// </summary>
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _productService.DeleteProductAsync(id, ct);
        return NoContent();
    }
}
