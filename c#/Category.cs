using System.ComponentModel.DataAnnotations;

namespace EcommerceApi.Models
{
    public class Category
    {
        [Key]
        public int CategoryId { get; set; }
        
        [Required]
        public required string CategoryName { get; set; }
        
        [Required]
        public required string Description { get; set; }
    }
}
