using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportShop.Models
{
    [Table("ProductAttribute")]
    public class ProductAttribute
    {
        [Key]
        public int AttributeID { get; set; }
        public int ProductID { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
        public int Stock { get; set; }
        public decimal? Price { get; set; }
        public string? ImageURL { get; set; }
        
        // New fields for Option 3 (Hybrid Approach)
        [StringLength(100)]
        public string? SKU { get; set; }
        
        [StringLength(100)]
        public string? Barcode { get; set; }
        
        public int? SizeOptionID { get; set; }
        public int? ColorOptionID { get; set; }
        
        // Navigation properties
        public Product? Product { get; set; }
        public SizeOption? SizeOption { get; set; }
        public ColorOption? ColorOption { get; set; }
    }
}