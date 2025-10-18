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
        
        // Voucher fields
        public int? VoucherID { get; set; }
        
        [Column(TypeName = "decimal(18, 2)")]
        public decimal? DiscountAmount { get; set; } = 0;
        
        [ForeignKey("VoucherID")]
        public virtual Voucher? Voucher { get; set; }
        
        // Navigation property
        public virtual ICollection<OrderItem> OrderItems { get; set; }
        
        public virtual ICollection<Payment> Payments { get; set; }
        
        // Computed property
        [NotMapped]
        public decimal FinalAmount => TotalAmount - (DiscountAmount ?? 0);
    }
}