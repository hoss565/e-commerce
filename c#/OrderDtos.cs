using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.Models.DTOs
{
    public class CreateOrderDto
    {
        [Required]
        public int UserId { get; set; }

        [Required]
        public int ShippingAddressId { get; set; }

        [Required]
        public decimal SubTotal { get; set; }

        [Required]
        public decimal ShippingCost { get; set; }

        [Required]
        public decimal Total { get; set; }

        public string Status { get; set; } = "Pending";
        public string PaymentMethod { get; set; } = "CashOnDelivery";
        public string PaymentStatus { get; set; } = "Pending";
        public List<CreateOrderDetailDto> OrderDetails { get; set; } = new();
    }

    public class CreateOrderDetailDto
    {
        [Required]
        public int ProductId { get; set; }

        [Required]
        [StringLength(10)]
        public string Size { get; set; } = string.Empty;

        [Required]
        public int Quantity { get; set; }

        [Required]
        public decimal Price { get; set; }

        [Required]
        public decimal SubTotal { get; set; }
    }

    public class OrderDto
    {
        public int OrderId { get; set; }
        public int UserId { get; set; }
        public int ShippingAddressId { get; set; }
        public decimal SubTotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Total { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public ShippingAddressDto? ShippingAddress { get; set; }
        public List<OrderDetailDto> OrderDetails { get; set; } = new();

        public static OrderDto FromOrder(Order order)
        {
            return new OrderDto
            {
                OrderId = order.OrderId,
                UserId = order.UserId,
                ShippingAddressId = order.ShippingAddressId,
                SubTotal = order.SubTotal,
                ShippingCost = order.ShippingCost,
                Total = order.Total,
                Status = order.Status,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                CreatedAt = order.CreatedAt,
                ShippingAddress = ShippingAddressDto.FromShippingAddress(order.ShippingAddress),
                OrderDetails = order.OrderDetails?.Select(OrderDetailDto.FromOrderDetail).ToList() ?? new()
            };
        }
    }

    public class OrderDetailDto
    {
        public int OrderDetailId { get; set; }
        public int OrderId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ImagePath { get; set; } = string.Empty;
        public string Size { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
        public decimal SubTotal { get; set; }

        public static OrderDetailDto FromOrderDetail(OrderDetail detail)
        {
            return new OrderDetailDto
            {
                OrderDetailId = detail.OrderDetailId,
                OrderId = detail.OrderId,
                ProductId = detail.ProductId,
                ProductName = detail.Product?.ProductName ?? "Unknown Product",
                ImagePath = detail.Product?.ImagePath ?? string.Empty,
                Size = detail.Size,
                Quantity = detail.Quantity,
                Price = detail.Price,
                SubTotal = detail.SubTotal
            };
        }
    }

    public class OrderSummaryDto
    {
        public int OrderId { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public decimal SubTotal { get; set; }
        public decimal ShippingCost { get; set; }
        public decimal Total { get; set; }
        public ShippingAddressDto? ShippingAddress { get; set; }
        public List<OrderDetailDto> OrderDetails { get; set; } = new();

        public static OrderSummaryDto FromOrder(Order order)
        {
            return new OrderSummaryDto
            {
                OrderId = order.OrderId,
                CreatedAt = order.CreatedAt,
                Status = order.Status,
                PaymentMethod = order.PaymentMethod,
                PaymentStatus = order.PaymentStatus,
                SubTotal = order.SubTotal,
                ShippingCost = order.ShippingCost,
                Total = order.Total,
                ShippingAddress = order.ShippingAddress != null ? ShippingAddressDto.FromShippingAddress(order.ShippingAddress) : null,
                OrderDetails = order.OrderDetails?.Select(OrderDetailDto.FromOrderDetail).ToList() ?? new()
            };
        }
    }
}
