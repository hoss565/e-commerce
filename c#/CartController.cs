using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using EcommerceApi.Models;
using EcommerceApi.Models.DTOs;
using EcommerceApi.Models.Extensions;

namespace EcommerceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CartController : ControllerBase
    {
        private readonly EcommerceDbContext _context;
        private readonly ILogger<CartController> _logger;

        public CartController(EcommerceDbContext context, ILogger<CartController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet("{userId}")]
        public async Task<ActionResult<CartDto>> GetCart(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest(new { message = "Invalid user ID" });
                }

            var cart = await _context.Carts
                .Include(c => c.User)
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    cart = await GetOrCreateCart(userId);
                }

                // Refresh cart to get updated items with full product details
                cart = await _context.Carts
                    .Include(c => c.User)
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                            .ThenInclude(p => p.Category)
                    .FirstOrDefaultAsync(c => c.CartId == cart.CartId);

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                return Ok(cart?.ToDto(baseUrl) ?? new CartDto { UserId = userId });
            }
            catch (InvalidOperationException)
            {
                return BadRequest(new { message = "User not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error getting cart. UserId: {UserId}, Error: {Error}", 
                    userId, ex.ToString());
                return StatusCode(500, new { message = "Error retrieving cart" });
            }
        }

        [HttpPost("{userId}/add")]
        public async Task<ActionResult<CartDto>> AddToCart(int userId, AddToCartDto dto)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest(new { message = "Invalid user ID" });
                }

            var cart = await _context.Carts
                .Include(c => c.User)
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.Category)
                .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    var user = await _context.Users.FindAsync(userId);
                    if (user == null)
                    {
                        return BadRequest(new { message = "User not found" });
                    }

                    cart = new Cart
                    {
                        UserId = userId,
                        User = user,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.Carts.Add(cart);
                    await _context.SaveChangesAsync();
                }

                // Validate product exists and has enough stock
                var product = await _context.Products
                    .Include(p => p.ProductSizes)
                    .Include(p => p.Category)
                    .FirstOrDefaultAsync(p => p.ProductId == dto.ProductId);

                if (product == null)
                {
                    return NotFound(new { message = "Product not found" });
                }

                var productSize = product.ProductSizes
                    .FirstOrDefault(ps => ps.Size == dto.Size);

                if (productSize == null)
                {
                    return BadRequest(new { message = "Invalid size selected" });
                }

                if (productSize.Quantity < dto.Quantity)
                {
                    return BadRequest(new { message = "Not enough stock available" });
                }

                // Check if item already exists in cart
                var existingItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => 
                        ci.CartId == cart.CartId && 
                        ci.ProductId == dto.ProductId && 
                        ci.Size == dto.Size);

                if (existingItem != null)
                {
                    // Update quantity if item exists
                    existingItem.Quantity += dto.Quantity;
                    existingItem.UpdatedAt = DateTime.UtcNow;
                    cart.UpdatedAt = DateTime.UtcNow;
                }
                else
                {
                    // Add new item if it doesn't exist
                    var cartItem = new CartItem
                    {
                        CartId = cart.CartId,
                        Cart = cart,
                        ProductId = product.ProductId,
                        Product = product,
                        Size = dto.Size,
                        Quantity = dto.Quantity,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    _context.CartItems.Add(cartItem);
                }

                await _context.SaveChangesAsync();

                // Refresh cart to get updated items
                cart = await _context.Carts
                    .Include(c => c.User)
                    .Include(c => c.CartItems)
                        .ThenInclude(ci => ci.Product)
                            .ThenInclude(p => p.Category)
                    .FirstOrDefaultAsync(c => c.CartId == cart.CartId);

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                return Ok(cart?.ToDto(baseUrl) ?? new CartDto { UserId = userId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error adding item to cart. UserId: {UserId}, ProductId: {ProductId}, Size: {Size}, Quantity: {Quantity}, Error: {Error}", 
                    userId, dto.ProductId, dto.Size, dto.Quantity, ex.ToString());
                return StatusCode(500, new { message = "Error adding item to cart" });
            }
        }

        [HttpPut("{userId}/update")]
        public async Task<ActionResult<CartDto>> UpdateCartItem(int userId, UpdateCartItemDto dto)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest(new { message = "Invalid user ID" });
                }

                var cart = await GetOrCreateCart(userId);

                var cartItem = await _context.CartItems
                    .Include(ci => ci.Product)
                    .ThenInclude(p => p.ProductSizes)
                    .FirstOrDefaultAsync(ci => 
                        ci.CartItemId == dto.CartItemId && 
                        ci.CartId == cart.CartId);

                if (cartItem == null)
                {
                    return NotFound(new { message = "Cart item not found" });
                }

                var productSize = cartItem.Product.ProductSizes
                    .FirstOrDefault(ps => ps.Size == cartItem.Size);

                if (productSize == null || productSize.Quantity < dto.Quantity)
                {
                    return BadRequest(new { message = "Not enough stock available" });
                }

                cartItem.Quantity = dto.Quantity;
                cartItem.UpdatedAt = DateTime.UtcNow;
                cart.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                return Ok(cart?.ToDto(baseUrl) ?? new CartDto { UserId = userId });
            }
            catch (InvalidOperationException)
            {
                return BadRequest(new { message = "User not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error updating cart item. UserId: {UserId}, CartItemId: {CartItemId}, NewQuantity: {Quantity}, Error: {Error}", 
                    userId, dto.CartItemId, dto.Quantity, ex.ToString());
                return StatusCode(500, new { message = "Error updating cart item" });
            }
        }

        [HttpDelete("{userId}/items/{cartItemId}")]
        public async Task<ActionResult<CartDto>> RemoveCartItem(int userId, int cartItemId)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest(new { message = "Invalid user ID" });
                }

                var cart = await GetOrCreateCart(userId);

                var cartItem = await _context.CartItems
                    .FirstOrDefaultAsync(ci => 
                        ci.CartItemId == cartItemId && 
                        ci.CartId == cart.CartId);

                if (cartItem == null)
                {
                    return NotFound(new { message = "Cart item not found" });
                }

                _context.CartItems.Remove(cartItem);
                await _context.SaveChangesAsync();

                var baseUrl = $"{Request.Scheme}://{Request.Host}";
                return Ok(cart?.ToDto(baseUrl) ?? new CartDto { UserId = userId });
            }
            catch (InvalidOperationException)
            {
                return BadRequest(new { message = "User not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error removing cart item. UserId: {UserId}, CartItemId: {CartItemId}, Error: {Error}", 
                    userId, cartItemId, ex.ToString());
                return StatusCode(500, new { message = "Error removing cart item" });
            }
        }

        [HttpGet("{userId}/summary")]
        public async Task<ActionResult<CartSummaryDto>> GetCartSummary(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest(new { message = "Invalid user ID" });
                }

                var cart = await GetOrCreateCart(userId);
                return Ok(cart != null ? CartSummaryDto.FromCart(cart) : new CartSummaryDto { ItemCount = 0, Total = 0 });
            }
            catch (InvalidOperationException)
            {
                return BadRequest(new { message = "User not found" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error getting cart summary. UserId: {UserId}, Error: {Error}", 
                    userId, ex.ToString());
                return StatusCode(500, new { message = "Error retrieving cart summary" });
            }
        }

        [HttpDelete("{userId}/clear")]
        public async Task<ActionResult> ClearCart(int userId)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest(new { message = "Invalid user ID" });
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null)
                {
                    _logger.LogWarning("Cart not found for user {UserId}", userId);
                    return NotFound(new { message = "Cart not found" });
                }

                if (!cart.CartItems.Any())
                {
                    _logger.LogInformation("Cart is already empty for user {UserId}", userId);
                    return Ok(new { message = "Cart is already empty" });
                }

                // Log items being removed
                _logger.LogInformation(
                    "Clearing {ItemCount} items from cart for user {UserId}. Items: {Items}", 
                    cart.CartItems.Count,
                    userId,
                    JsonSerializer.Serialize(cart.CartItems.Select(ci => new { ci.ProductId, ci.Size, ci.Quantity }))
                );

                // Remove all cart items
                _context.CartItems.RemoveRange(cart.CartItems);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Successfully cleared cart for user {UserId}", userId);
                return Ok(new { message = "Cart cleared successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error clearing cart. UserId: {UserId}, Error: {Error}", 
                    userId, ex.ToString());
                return StatusCode(500, new { message = "Error clearing cart" });
            }
        }

        [HttpPost("{userId}/checkout")]
        public async Task<ActionResult> Checkout(int userId, [FromBody] CheckoutDto checkoutDto)
        {
            try
            {
                if (userId <= 0)
                {
                    return BadRequest(new { message = "Invalid user ID" });
                }

                var cart = await _context.Carts
                    .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                        .ThenInclude(p => p.Category)
                    .FirstOrDefaultAsync(c => c.UserId == userId);

                if (cart == null || !cart.CartItems.Any())
                {
                    return BadRequest(new { message = "Cart is empty" });
                }

                // Validate shipping address
                var shippingAddress = await _context.ShippingAddresses
                    .FirstOrDefaultAsync(sa => sa.ShippingAddressId == checkoutDto.ShippingAddressId && sa.UserId == userId);

                if (shippingAddress == null)
                {
                    return BadRequest(new { message = "Invalid shipping address" });
                }

                // Calculate totals
                decimal subTotal = cart.CartItems.Sum(ci => ci.Quantity * ci.Product.Price);
                decimal shippingCost = 10.00m; // Fixed shipping cost for now
                decimal total = subTotal + shippingCost;

                // Create order
                var order = new Order
                {
                    UserId = userId,
                    ShippingAddressId = checkoutDto.ShippingAddressId,
                    SubTotal = subTotal,
                    ShippingCost = shippingCost,
                    Total = total,
                    Status = "Pending",
                    PaymentMethod = checkoutDto.PaymentMethod,
                    PaymentStatus = "Pending",
                    CreatedAt = DateTime.UtcNow,
                    User = cart.User,
                    ShippingAddress = shippingAddress
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync(); // Save to get OrderId

                // Create order details
                foreach (var cartItem in cart.CartItems)
                {
                    var orderDetail = new OrderDetail
                    {
                        Order = order,
                        Product = cartItem.Product,
                        Size = cartItem.Size,
                        Quantity = cartItem.Quantity,
                        Price = cartItem.Product.Price,
                        SubTotal = cartItem.Product.Price * cartItem.Quantity
                    };
                    _context.OrderDetails.Add(orderDetail);
                }

                // Clear cart
                _context.CartItems.RemoveRange(cart.CartItems);

                await _context.SaveChangesAsync();

                return Ok(new { 
                    message = "Order placed successfully", 
                    orderId = order.OrderId,
                    total = order.Total
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, 
                    "Error during checkout. UserId: {UserId}, Error: {Error}", 
                    userId, ex.ToString());
                return StatusCode(500, new { message = "Error during checkout" });
            }
        }

        private async Task<Cart> GetOrCreateCart(int userId)
        {
            var cart = await _context.Carts
                .Include(c => c.User)
                .Include(c => c.CartItems)
                    .ThenInclude(ci => ci.Product)
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (cart == null)
            {
                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    throw new InvalidOperationException("User not found");
                }

                cart = new Cart
                {
                    UserId = userId,
                    User = user,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.Carts.Add(cart);
                await _context.SaveChangesAsync();
            }

            return cart;
        }
    }
}