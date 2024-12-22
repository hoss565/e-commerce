using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace EcommerceApi.Models
{
    [Table("ProductSizes")]
    public class ProductSize
    {
        [Key]
        public int ProductSizeId { get; set; }

        [Required]
        public int ProductId { get; set; }

        [Required]
        [StringLength(10)]
        public string Size { get; set; } = string.Empty;

        [Required]
        [Range(0, int.MaxValue)]
        public int Quantity { get; set; }

        // Navigation property
        [ForeignKey(nameof(ProductId))]
        public required virtual Product Product { get; set; }
    }
}
