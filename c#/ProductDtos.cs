using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.Models.DTOs
{
    public class ProductDto
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Description { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<ProductSizeDto> Sizes { get; set; } = new();

        public static ProductDto FromProduct(Product product, string baseUrl = "")
        {
            var dto = new ProductDto
            {
                ProductId = product.ProductId,
                ProductName = product.ProductName,
                Price = product.Price,
                Description = product.Description,
                ImagePath = product.ImagePath,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.CategoryName ?? "Unknown Category",
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                Sizes = product.ProductSizes?.Select(ps => new ProductSizeDto
                {
                    ProductSizeId = ps.ProductSizeId,
                    ProductId = ps.ProductId,
                    Size = ps.Size,
                    Quantity = ps.Quantity
                }).ToList() ?? new()
            };

            // Update image path with base URL if provided
            if (!string.IsNullOrEmpty(baseUrl) && !string.IsNullOrEmpty(dto.ImagePath))
            {
                dto.ImagePath = $"{baseUrl.TrimEnd('/')}/{dto.ImagePath.TrimStart('/')}";
            }

            return dto;
        }
    }

    public class ProductSizeDto
    {
        public int ProductSizeId { get; set; }
        public int ProductId { get; set; }
        public string Size { get; set; } = string.Empty;
        public int Quantity { get; set; }

        public static ProductSizeDto FromProductSize(ProductSize productSize)
        {
            return new ProductSizeDto
            {
                ProductSizeId = productSize.ProductSizeId,
                ProductId = productSize.ProductId,
                Size = productSize.Size,
                Quantity = productSize.Quantity
            };
        }
    }

    public class CreateProductSizeDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [StringLength(10)]
        public string Size { get; set; } = string.Empty;

        [Required]
        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public class UpdateProductSizeDto
    {
        [Required]
        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }
    }
}
