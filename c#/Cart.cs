using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceApi.Models
{
    [Table("Carts")]
    public class Cart
    {
        [Key]
        public int CartId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        [ForeignKey(nameof(UserId))]
        public virtual User User { get; set; } = null!;

        public virtual ICollection<CartItem> CartItems { get; set; } = new HashSet<CartItem>();

        [NotMapped]
        public decimal TotalAmount => CartItems.Sum(item => item.Quantity * item.Product.Price);

        [NotMapped]
        public int TotalItems => CartItems.Sum(item => item.Quantity);
    }
}
