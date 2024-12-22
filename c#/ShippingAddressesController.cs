using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using EcommerceApi.Models;
using EcommerceApi.Models.DTOs;

namespace EcommerceApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ShippingAddressesController : ControllerBase
    {
        private readonly EcommerceDbContext _context;
        private readonly ILogger<ShippingAddressesController> _logger;

        public ShippingAddressesController(EcommerceDbContext context, ILogger<ShippingAddressesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpPost]
        public async Task<ActionResult<ShippingAddressDto>> CreateShippingAddress(ShippingAddressDto addressDto, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (addressDto == null)
                {
                    _logger.LogWarning("CreateShippingAddress: Received null addressDto");
                    return BadRequest("Shipping address data is required.");
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(addressDto.StreetAddress) ||
                    string.IsNullOrWhiteSpace(addressDto.City) ||
                    string.IsNullOrWhiteSpace(addressDto.State) ||
                    string.IsNullOrWhiteSpace(addressDto.Country) ||
                    string.IsNullOrWhiteSpace(addressDto.Phone))
                {
                    _logger.LogWarning("CreateShippingAddress: Missing required fields in addressDto");
                    return BadRequest("All fields are required.");
                }

                var shippingAddress = new ShippingAddress
                {
                    UserId = addressDto.UserId,
                    StreetAddress = addressDto.StreetAddress.Trim(),
                    City = addressDto.City.Trim(),
                    State = addressDto.State.Trim(),
                    Country = addressDto.Country.Trim(),
                    Phone = addressDto.Phone.Trim()
                };

                _context.ShippingAddresses.Add(shippingAddress);
                
                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error saving shipping address to database. UserId: {UserId}", addressDto.UserId);
                    return StatusCode(500, "An error occurred while saving the shipping address.");
                }

                _logger.LogInformation("Successfully created shipping address for user {UserId}", addressDto.UserId);
                return CreatedAtAction(nameof(GetShippingAddress), 
                    new { id = shippingAddress.ShippingAddressId }, 
                    ShippingAddressDto.FromShippingAddress(shippingAddress));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in CreateShippingAddress for user {UserId}", addressDto?.UserId);
                return StatusCode(500, "An unexpected error occurred while processing your request.");
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ShippingAddressDto>> GetShippingAddress(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var shippingAddress = await _context.ShippingAddresses
                    .FirstOrDefaultAsync(sa => sa.ShippingAddressId == id, cancellationToken);

                if (shippingAddress == null)
                {
                    _logger.LogWarning("GetShippingAddress: Address not found. Id: {Id}", id);
                    return NotFound("Shipping address not found.");
                }

                return ShippingAddressDto.FromShippingAddress(shippingAddress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetShippingAddress for id {Id}", id);
                return StatusCode(500, "An unexpected error occurred while retrieving the shipping address.");
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<ShippingAddressDto>>> GetUserShippingAddresses(int userId, CancellationToken cancellationToken = default)
        {
            try
            {
                var addresses = await _context.ShippingAddresses
                    .Where(sa => sa.UserId == userId)
                    .Select(sa => ShippingAddressDto.FromShippingAddress(sa))
                    .ToListAsync(cancellationToken);

                _logger.LogInformation("Retrieved {Count} shipping addresses for user {UserId}", addresses.Count, userId);
                return addresses;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in GetUserShippingAddresses for userId {UserId}", userId);
                return StatusCode(500, "An unexpected error occurred while retrieving the shipping addresses.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateShippingAddress(int id, ShippingAddressDto addressDto, CancellationToken cancellationToken = default)
        {
            try
            {
                // Validate input
                if (addressDto == null)
                {
                    _logger.LogWarning("UpdateShippingAddress: Received null addressDto for id {Id}", id);
                    return BadRequest("Shipping address data is required.");
                }

                var shippingAddress = await _context.ShippingAddresses
                    .FirstOrDefaultAsync(sa => sa.ShippingAddressId == id && sa.UserId == addressDto.UserId, cancellationToken);

                if (shippingAddress == null)
                {
                    _logger.LogWarning("UpdateShippingAddress: Address not found or unauthorized. Id: {Id}, UserId: {UserId}", id, addressDto.UserId);
                    return NotFound("Shipping address not found or you're not authorized to modify it.");
                }

                // Validate required fields
                if (string.IsNullOrWhiteSpace(addressDto.StreetAddress) ||
                    string.IsNullOrWhiteSpace(addressDto.City) ||
                    string.IsNullOrWhiteSpace(addressDto.State) ||
                    string.IsNullOrWhiteSpace(addressDto.Country) ||
                    string.IsNullOrWhiteSpace(addressDto.Phone))
                {
                    _logger.LogWarning("UpdateShippingAddress: Missing required fields for id {Id}", id);
                    return BadRequest("All fields are required.");
                }

                shippingAddress.StreetAddress = addressDto.StreetAddress.Trim();
                shippingAddress.City = addressDto.City.Trim();
                shippingAddress.State = addressDto.State.Trim();
                shippingAddress.Country = addressDto.Country.Trim();
                shippingAddress.Phone = addressDto.Phone.Trim();
                shippingAddress.UpdatedAt = DateTime.UtcNow;

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error updating shipping address {Id} for user {UserId}", id, addressDto.UserId);
                    return StatusCode(500, "An error occurred while updating the shipping address.");
                }

                _logger.LogInformation("Successfully updated shipping address {Id} for user {UserId}", id, addressDto.UserId);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in UpdateShippingAddress for id {Id}", id);
                return StatusCode(500, "An unexpected error occurred while processing your request.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteShippingAddress(int id, CancellationToken cancellationToken = default)
        {
            try
            {
                var shippingAddress = await _context.ShippingAddresses
                    .FirstOrDefaultAsync(sa => sa.ShippingAddressId == id, cancellationToken);
                
                if (shippingAddress == null)
                {
                    _logger.LogWarning("DeleteShippingAddress: Address not found. Id: {Id}", id);
                    return NotFound("Shipping address not found.");
                }

                // Check if the address is being used in any orders
                var isAddressInUse = await _context.Orders
                    .AnyAsync(o => o.ShippingAddressId == id, cancellationToken);

                if (isAddressInUse)
                {
                    _logger.LogWarning("DeleteShippingAddress: Cannot delete address {Id} as it is used in orders", id);
                    return BadRequest("Cannot delete this shipping address as it is associated with one or more orders.");
                }

                _context.ShippingAddresses.Remove(shippingAddress);

                try
                {
                    await _context.SaveChangesAsync(cancellationToken);
                }
                catch (DbUpdateException ex)
                {
                    _logger.LogError(ex, "Error deleting shipping address {Id}", id);
                    return StatusCode(500, "An error occurred while deleting the shipping address.");
                }

                _logger.LogInformation("Successfully deleted shipping address {Id}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in DeleteShippingAddress for id {Id}", id);
                return StatusCode(500, "An unexpected error occurred while deleting the shipping address.");
            }
        }
    }
}
