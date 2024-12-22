using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.Models.DTOs
{
    public class CartDto
    {
        public int CartId { get; set; }
        public int UserId { get; set; }
        public List<CartItemDto> CartItems { get; set; } = new();
        public decimal Total { get; set; }
        public DateTime CreatedAt { get; set; }

        public static CartDto FromCart(Cart cart)
        {
            return new CartDto
            {
                CartId = cart.CartId,
                UserId = cart.UserId,
                CartItems = cart.CartItems?.Select(CartItemDto.FromCartItem).ToList() ?? new(),
                Total = cart.CartItems?.Sum(ci => ci.Quantity * ci.Product.Price) ?? 0,
                CreatedAt = cart.CreatedAt
            };
        }
    }

    public class CartItemDto
    {
        public int CartItemId { get; set; }
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string Size { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal SubTotal { get; set; }
        public string CategoryName { get; set; } = string.Empty;

        public static CartItemDto FromCartItem(CartItem item)
        {
            return new CartItemDto
            {
                CartItemId = item.CartItemId,
                CartId = item.CartId,
                ProductId = item.ProductId,
                ProductName = item.Product?.ProductName ?? "Unknown Product",
                Description = item.Product?.Description ?? string.Empty,
                ImagePath = item.Product?.ImagePath ?? string.Empty,
                Price = item.Product?.Price ?? 0,
                Size = item.Size,
                Quantity = item.Quantity,
                SubTotal = item.Quantity * (item.Product?.Price ?? 0),
                CategoryName = item.Product?.Category?.CategoryName ?? string.Empty
            };
        }
    }

    public class AddToCartDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [StringLength(10)]
        public string Size { get; set; } = string.Empty;

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public class UpdateCartItemDto
    {
        [Required]
        public int CartItemId { get; set; }

        [Required]
        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }
    }

    public class CartSummaryDto
    {
        public int ItemCount { get; set; }
        public decimal Total { get; set; }

        public static CartSummaryDto FromCart(Cart cart)
        {
            return new CartSummaryDto
            {
                ItemCount = cart.CartItems?.Sum(ci => ci.Quantity) ?? 0,
                Total = cart.CartItems?.Sum(ci => ci.Quantity * ci.Product.Price) ?? 0
            };
        }
    }

    public class CheckoutDto
    {
        [Required]
        public int ShippingAddressId { get; set; }

        [Required]
        [StringLength(20)]
        public string PaymentMethod { get; set; } = string.Empty;
    }
}

