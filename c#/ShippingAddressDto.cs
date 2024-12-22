using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.Models.DTOs
{
    public class ShippingAddressDto
    {
        public int ShippingAddressId { get; set; }
        public int UserId { get; set; }

        [Required(ErrorMessage = "Street address is required")]
        [StringLength(255, ErrorMessage = "Street address cannot exceed 255 characters")]
        public string StreetAddress { get; set; } = string.Empty;

        [Required(ErrorMessage = "City is required")]
        [StringLength(100, ErrorMessage = "City name cannot exceed 100 characters")]
        public string City { get; set; } = string.Empty;

        [Required(ErrorMessage = "State is required")]
        [StringLength(100, ErrorMessage = "State name cannot exceed 100 characters")]
        public string State { get; set; } = string.Empty;

        [Required(ErrorMessage = "Country is required")]
        [StringLength(100, ErrorMessage = "Country name cannot exceed 100 characters")]
        public string Country { get; set; } = string.Empty;

        [Required(ErrorMessage = "Phone number is required")]
        [StringLength(20, ErrorMessage = "Phone number cannot exceed 20 characters")]
        [RegularExpression(@"^01[0125][0-9]{8}$", ErrorMessage = "Please enter a valid Egyptian phone number (e.g., 01234567890)")]
        public string Phone { get; set; } = string.Empty;

        public static ShippingAddressDto FromShippingAddress(ShippingAddress address)
        {
            if (address == null)
                return new ShippingAddressDto();

            return new ShippingAddressDto
            {
                ShippingAddressId = address.ShippingAddressId,
                UserId = address.UserId,
                StreetAddress = address.StreetAddress,
                City = address.City,
                State = address.State,
                Country = address.Country,
                Phone = address.Phone
            };
        }
    }
}
