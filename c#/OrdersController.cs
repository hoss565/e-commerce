using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Text.Json;
using EcommerceApi.Models;
using EcommerceApi.Models.DTOs;

namespace EcommerceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class OrdersController : ControllerBase
    {
        private readonly EcommerceDbContext _context;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(EcommerceDbContext context, ILogger<OrdersController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<OrderDto>> CreateOrder(CreateOrderDto dto)
        {
            if (dto == null)
            {
                return BadRequest(new { message = "Order data is required" });
            }

            var user = await _context.Users.FindAsync(dto.UserId);
            if (user == null)
            {
                return NotFound(new { message = "User not found" });
            }

            var shippingAddress = await _context.ShippingAddresses.FindAsync(dto.ShippingAddressId);
            if (shippingAddress == null)
            {
                return NotFound(new { message = "Shipping address not found" });
            }

            var order = new Order
            {
                UserId = dto.UserId,
                User = user,
                ShippingAddressId = dto.ShippingAddressId,
                ShippingAddress = shippingAddress,
                SubTotal = dto.SubTotal,
                ShippingCost = dto.ShippingCost,
                Total = dto.Total,
                Status = dto.Status,
                PaymentMethod = dto.PaymentMethod,
                PaymentStatus = dto.PaymentStatus,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                OrderDetails = new List<OrderDetail>()
            };

            foreach (var detail in dto.OrderDetails)
            {
                var product = await _context.Products.FindAsync(detail.ProductId);
                if (product == null)
                {
                    return NotFound(new { message = $"Product with ID {detail.ProductId} not found" });
                }

                order.OrderDetails.Add(new OrderDetail
                {
                    ProductId = detail.ProductId,
                    Product = product,
                    Size = detail.Size,
                    Quantity = detail.Quantity,
                    Price = detail.Price,
                    SubTotal = detail.Price * detail.Quantity,
                    Order = order
                });
            }

            try
            {
                using (var transaction = await _context.Database.BeginTransactionAsync())
                {
                    try
                    {
                        _context.Orders.Add(order);
                        await _context.SaveChangesAsync();

                        var createdOrder = await _context.Orders
                            .Include(o => o.ShippingAddress)
                            .Include(o => o.OrderDetails)
                                .ThenInclude(od => od.Product)
                            .FirstOrDefaultAsync(o => o.OrderId == order.OrderId);

                        if (createdOrder == null)
                        {
                            await transaction.RollbackAsync();
                            return StatusCode(500, new { message = "Failed to retrieve created order" });
                        }

                        await transaction.CommitAsync();

                        return CreatedAtAction(
                            nameof(GetOrder),
                            new { id = createdOrder.OrderId },
                            OrderDto.FromOrder(createdOrder)
                        );
                    }
                    catch (Exception)
                    {
                        await transaction.RollbackAsync();
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to create order. UserId: {UserId}, ShippingAddressId: {ShippingAddressId}, " +
                    "OrderDetails: {OrderDetails}, Error: {Error}",
                    dto.UserId,
                    dto.ShippingAddressId,
                    JsonSerializer.Serialize(dto.OrderDetails),
                    ex.ToString());
                return StatusCode(500, new { message = $"Failed to create order: {ex.Message}" });
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDto>> GetOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.ShippingAddress)
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound(new { message = $"Order with ID {id} not found" });
            }

            return OrderDto.FromOrder(order);
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<OrderSummaryDto>>> GetUserOrders(int userId)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderDetails)
                    .ThenInclude(od => od.Product)
                .Include(o => o.ShippingAddress)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();

            return Ok(orders.Select(OrderSummaryDto.FromOrder));
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateOrderStatus(int id, [FromBody] string status)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound(new { message = $"Order with ID {id} not found" });
            }

            var validStatuses = new[] { "Pending", "Processing", "Shipped", "Delivered", "Cancelled" };
            if (!validStatuses.Contains(status))
            {
                return BadRequest(new { message = $"Invalid status. Valid statuses are: {string.Join(", ", validStatuses)}" });
            }

            order.Status = status;
            order.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to update order status. OrderId: {OrderId}, NewStatus: {Status}, Error: {Error}",
                    id, status, ex.ToString());
                return StatusCode(500, new { message = $"Failed to update order status: {ex.Message}" });
            }
        }

        [HttpPut("{id}/payment-status")]
        public async Task<IActionResult> UpdatePaymentStatus(int id, [FromBody] string paymentStatus)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order == null)
            {
                return NotFound(new { message = $"Order with ID {id} not found" });
            }

            var validStatuses = new[] { "Pending", "Paid", "Failed" };
            if (!validStatuses.Contains(paymentStatus))
            {
                return BadRequest(new { message = $"Invalid payment status. Valid statuses are: {string.Join(", ", validStatuses)}" });
            }

            order.PaymentStatus = paymentStatus;
            order.UpdatedAt = DateTime.UtcNow;

            try
            {
                await _context.SaveChangesAsync();
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to update payment status. OrderId: {OrderId}, NewStatus: {PaymentStatus}, Error: {Error}",
                    id, paymentStatus, ex.ToString());
                return StatusCode(500, new { message = $"Failed to update payment status: {ex.Message}" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteOrder(int id)
        {
            var order = await _context.Orders
                .Include(o => o.OrderDetails)
                .FirstOrDefaultAsync(o => o.OrderId == id);

            if (order == null)
            {
                return NotFound(new { message = $"Order with ID {id} not found" });
            }

            try
            {
                _context.OrderDetails.RemoveRange(order.OrderDetails);
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to delete order. OrderId: {OrderId}, Error: {Error}",
                    id, ex.ToString());
                return StatusCode(500, new { message = $"Failed to delete order: {ex.Message}" });
            }
        }

        [HttpDelete("details/{orderDetailId}")]
        public async Task<IActionResult> DeleteOrderDetail(int orderDetailId)
        {
            try
            {
                var orderDetail = await _context.OrderDetails
                    .Include(od => od.Order)
                    .FirstOrDefaultAsync(od => od.OrderDetailId == orderDetailId);

                if (orderDetail == null)
                {
                    return NotFound(new { message = $"Order detail with ID {orderDetailId} not found" });
                }

                var order = orderDetail.Order;
                order.SubTotal -= orderDetail.SubTotal;
                order.Total = order.SubTotal + order.ShippingCost;
                order.UpdatedAt = DateTime.UtcNow;

                _context.OrderDetails.Remove(orderDetail);
                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Failed to delete order detail. OrderDetailId: {OrderDetailId}, Error: {Error}",
                    orderDetailId, ex.ToString());
                return StatusCode(500, new { message = $"Failed to delete order detail: {ex.Message}" });
            }
        }
    }
}
