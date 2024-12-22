using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using EcommerceApi.Models;
using EcommerceApi.Models.DTOs;

namespace EcommerceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductSizesController : ControllerBase
    {
        private readonly EcommerceDbContext _context;
        private readonly ILogger<ProductSizesController> _logger;

        public ProductSizesController(EcommerceDbContext context, ILogger<ProductSizesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("product/{productId}")]
        public async Task<ActionResult<IEnumerable<ProductSizeDto>>> GetProductSizes(int productId)
        {
            try
            {
                var productSizes = await _context.ProductSizes
                    .Where(ps => ps.ProductId == productId)
                    .ToListAsync();

                var sizeDtos = productSizes.Select(ps => new ProductSizeDto
                {
                    Size = ps.Size,
                    Quantity = ps.Quantity
                });

                return Ok(sizeDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting product sizes for product {ProductId}", productId);
                return StatusCode(500, new { message = "Error retrieving product sizes", error = ex.Message });
            }
        }

        [HttpPost("product/{productId}")]
        public async Task<ActionResult<ProductSizeDto>> AddProductSize(int productId, ProductSizeDto sizeDto)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return NotFound(new { message = "Product not found" });
                }

                var productSize = new ProductSize
                {
                    ProductId = productId,
                    Size = sizeDto.Size,
                    Quantity = sizeDto.Quantity,
                    Product = product
                };

                _context.ProductSizes.Add(productSize);
                await _context.SaveChangesAsync();

                return CreatedAtAction(
                    nameof(GetProductSizes),
                    new { productId = productSize.ProductId },
                    new ProductSizeDto
                    {
                        Size = productSize.Size,
                        Quantity = productSize.Quantity
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adding product size for product {ProductId}", productId);
                return StatusCode(500, new { message = "Error adding product size", error = ex.Message });
            }
        }

        [HttpPut("product/{productId}/size/{size}")]
        public async Task<IActionResult> UpdateProductSize(int productId, string size, ProductSizeDto sizeDto)
        {
            try
            {
                var productSize = await _context.ProductSizes
                    .FirstOrDefaultAsync(ps => ps.ProductId == productId && ps.Size == size);

                if (productSize == null)
                {
                    return NotFound(new { message = "Product size not found" });
                }

                productSize.Quantity = sizeDto.Quantity;
                await _context.SaveChangesAsync();

                return Ok(new ProductSizeDto
                {
                    Size = productSize.Size,
                    Quantity = productSize.Quantity
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating product size for product {ProductId} and size {Size}", productId, size);
                return StatusCode(500, new { message = "Error updating product size", error = ex.Message });
            }
        }

        [HttpDelete("product/{productId}/size/{size}")]
        public async Task<IActionResult> DeleteProductSize(int productId, string size)
        {
            try
            {
                var productSize = await _context.ProductSizes
                    .FirstOrDefaultAsync(ps => ps.ProductId == productId && ps.Size == size);

                if (productSize == null)
                {
                    return NotFound(new { message = "Product size not found" });
                }

                _context.ProductSizes.Remove(productSize);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Product size deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting product size for product {ProductId} and size {Size}", productId, size);
                return StatusCode(500, new { message = "Error deleting product size", error = ex.Message });
            }
        }
    }
}
