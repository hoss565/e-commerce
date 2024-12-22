 using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using EcommerceApi.Models;
using EcommerceApi.Models.DTOs;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;

namespace EcommerceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly EcommerceDbContext _context;
        private readonly ILogger<ProductsController> _logger;
        private readonly IWebHostEnvironment _environment;

        public ProductsController(EcommerceDbContext context, ILogger<ProductsController> logger, IWebHostEnvironment environment)
        {
            _context = context;
            _logger = logger;
            _environment = environment;
        }

        [HttpPost]
        public async Task<ActionResult<ProductDto>> CreateProduct([FromForm] string productName,
            [FromForm] string description,
            [FromForm] decimal price,
            [FromForm] int categoryId,
            [FromForm] string[] sizes,
            [FromForm] int[] quantities,
            IFormFile? image)
        {
            try
            {
                if (sizes.Length != quantities.Length)
                {
                    return BadRequest(new { message = "Number of sizes must match number of quantities" });
                }

                var product = new Product
                {
                    ProductName = productName,
                    Description = description,
                    Price = price,
                    CategoryId = categoryId,
                    CreatedAt = DateTime.UtcNow
                };

                if (image != null)
                {
                    string uploadsFolder = Path.Combine(_environment.WebRootPath, "images");
                    string uniqueFileName = Guid.NewGuid().ToString() + "_" + image.FileName;
                    string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                    Directory.CreateDirectory(uploadsFolder);
                    using (var fileStream = new FileStream(filePath, FileMode.Create))
                    {
                        await image.CopyToAsync(fileStream);
                    }

                    product.ImagePath = $"images/{uniqueFileName}";
                }

                _context.Products.Add(product);
                await _context.SaveChangesAsync();

                // Add product sizes
                for (int i = 0; i < sizes.Length; i++)
                {
                    if (!string.IsNullOrEmpty(sizes[i]) && quantities[i] > 0)
                    {
                        var productSize = new ProductSize
                        {
                            ProductId = product.ProductId,
                            Size = sizes[i],
                            Quantity = quantities[i],
                            Product = product
                        };
                        _context.ProductSizes.Add(productSize);
                    }
                }
                await _context.SaveChangesAsync();

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var createdProductDto = ProductDto.FromProduct(product, baseUrl);

                return CreatedAtAction(nameof(GetProduct), new { id = product.ProductId }, 
                    new { message = "Product created successfully", product = createdProductDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating product");
                return StatusCode(500, new { message = "Error creating product", error = ex.Message });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDto>> GetProduct(int id)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.ProductSizes)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                {
                    return NotFound(new { message = "Product not found" });
                }

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var productDto = ProductDto.FromProduct(product, baseUrl);

                return Ok(new { message = "Product fetched successfully", product = productDto });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product {ProductId}", id);
                return StatusCode(500, new { message = "Error retrieving product", error = ex.Message });
            }
        }

        [HttpPut("{id}/update-stock")]
        public async Task<IActionResult> UpdateStock(int id, [FromBody] List<ProductSizeDto> sizes)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.ProductSizes)
                    .FirstOrDefaultAsync(p => p.ProductId == id);

                if (product == null)
                {
                    return NotFound(new { message = "Product not found" });
                }

                foreach (var size in sizes)
                {
                    var existingSize = product.ProductSizes.FirstOrDefault(s => s.Size == size.Size);
                    if (existingSize != null)
                    {
                        existingSize.Quantity = size.Quantity;
                    }
                    else
                    {
                        _context.ProductSizes.Add(new ProductSize
                        {
                            ProductId = id,
                            Size = size.Size,
                            Quantity = size.Quantity,
                            Product = product
                        });
                    }
                }

                await _context.SaveChangesAsync();
                return Ok(new { message = "Stock updated successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock for product {ProductId}", id);
                return StatusCode(500, new { message = "Error updating stock", error = ex.Message });
            }
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ProductDto>>> GetProducts()
        {
            try
            {
                var products = await _context.Products
                    .AsNoTracking()
                    .Include(p => p.Category)
                    .Include(p => p.ProductSizes)
                    .AsSplitQuery()
                    .ToListAsync();

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                var productDtos = products.Select(p => ProductDto.FromProduct(p, baseUrl));

                return Ok(productDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting products");
                return StatusCode(500, new { message = "Error retrieving products", error = ex.Message });
            }
        }

[HttpDelete("{id}")]
public async Task<IActionResult> DeleteProduct(int id)
{
    try
    {
        var product = await _context.Products
            .Include(p => p.ProductSizes)
            .FirstOrDefaultAsync(p => p.ProductId == id);

        if (product == null)
        {
            return NotFound(new { message = "Product not found" });
        }

        // Delete associated product sizes first
        _context.ProductSizes.RemoveRange(product.ProductSizes);
        
        // Delete the product
        _context.Products.Remove(product);
        
        // Delete the image file if it exists
        if (!string.IsNullOrEmpty(product.ImagePath))
        {
            var imagePath = Path.Combine(_environment.WebRootPath, product.ImagePath);
            if (System.IO.File.Exists(imagePath))
            {
                System.IO.File.Delete(imagePath);
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Product deleted successfully" });
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Error deleting product {ProductId}", id);
        return StatusCode(500, new { message = "Error deleting product", error = ex.Message });
    }
}

[HttpGet("category/{categoryId}")]
public async Task<ActionResult<IEnumerable<ProductDto>>> GetProductsByCategory(int categoryId)
{
    try
    {
        var products = await _context.Products
            .AsNoTracking()
            .Where(p => p.CategoryId == categoryId)
            .Select(p => new
            {
                ProductId = p.ProductId,
                ProductName = p.ProductName ?? string.Empty,
                Description = p.Description ?? string.Empty,
                Price = p.Price,
                CategoryId = p.CategoryId,
                CategoryName = p.Category.CategoryName ?? "فئة غير معروفة",
                ImagePath = p.ImagePath ?? string.Empty,
                CreatedAt = p.CreatedAt,
                UpdatedAt = p.UpdatedAt,
                Sizes = p.ProductSizes.Select(ps => new
                {
                    ProductSizeId = ps.ProductSizeId,
                    Size = ps.Size ?? string.Empty,
                    Quantity = ps.Quantity
                }).ToList()
            })
            .ToListAsync();

        if (!products.Any())
        {
            return NotFound(new { message = "لا توجد منتجات في هذه الفئة" });
        }

        var baseUrl = $"{Request.Scheme}://{Request.Host}";
        var productDtos = products.Select(p => new ProductDto
        {
            ProductId = p.ProductId,
            ProductName = p.ProductName,
            Description = p.Description,
            Price = p.Price,
            CategoryId = p.CategoryId,
            CategoryName = p.CategoryName,
            ImagePath = !string.IsNullOrEmpty(p.ImagePath) 
                ? $"{baseUrl}/{p.ImagePath}" 
                : $"{baseUrl}/images/default-product.jpg",
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt,
            Sizes = p.Sizes.Select(s => new ProductSizeDto
            {
                ProductSizeId = s.ProductSizeId,
                Size = s.Size,
                Quantity = s.Quantity
            }).ToList()
        }).ToList();

        return Ok(productDtos);
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "حدث خطأ أثناء استرجاع المنتجات للفئة {CategoryId}", categoryId);
        return StatusCode(500, new { message = "حدث خطأ أثناء استرجاع المنتجات", error = ex.Message });
    }
}
    }
}
