using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportShop.Models
{
    [Table("Cart")]
    public class Cart
    {
        [Key]
        public int CartID { get; set; }
        
        public int UserID { get; set; }
        
        [ForeignKey("UserID")]
        public virtual User User { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        public int ProductID { get; set; }
        
        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
        
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Số lượng phải lớn hơn 0")]
        public int Quantity { get; set; }
        
        public int? AttributeID { get; set; }
        
        [ForeignKey("AttributeID")]
        public virtual ProductAttribute Attribute { get; set; }
    }
}