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
        public string Size { get; set; }
        public string Color { get; set; }
        public int Stock { get; set; }
        public decimal? Price { get; set; }
        public string ImageURL { get; set; }
        
        public Product Product { get; set; }
    }
}