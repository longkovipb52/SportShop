using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportShop.Models
{
    [Table("OrderItem")]
    public class OrderItem
    {
        [Key]
        public int OrderItemID { get; set; }
        
        public int OrderID { get; set; }
        
        [ForeignKey("OrderID")]
        public virtual Order Order { get; set; }
        
        public int ProductID { get; set; }
        
        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
        
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal UnitPrice { get; set; }
        
        public int? AttributeID { get; set; }
        
        [ForeignKey("AttributeID")]
        public virtual ProductAttribute Attribute { get; set; }
    }
}