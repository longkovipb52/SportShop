using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportShop.Models
{
    [Table("Order")]
    public class Order
    {
        [Key]
        public int OrderID { get; set; }
        
        public int UserID { get; set; }
        
        [ForeignKey("UserID")]
        public virtual User User { get; set; }
        
        public DateTime OrderDate { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal TotalAmount { get; set; }
        
        [StringLength(50)]
        public string Status { get; set; }
        
        [Required]
        [StringLength(255)]
        public string ShippingName { get; set; }
        
        [Required]
        [StringLength(255)]
        public string ShippingAddress { get; set; }
        
        [Required]
        [StringLength(20)]
        public string ShippingPhone { get; set; }
        
        [StringLength(255)]
        public string Note { get; set; }
        
        // Navigation property
        public virtual ICollection<OrderItem> OrderItems { get; set; }
        
        public virtual ICollection<Payment> Payments { get; set; }
    }
}