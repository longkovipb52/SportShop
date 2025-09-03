using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportShop.Models
{
    [Table("Payment")]
    public class Payment
    {
        [Key]
        public int PaymentID { get; set; }
        
        public int OrderID { get; set; }
        
        [ForeignKey("OrderID")]
        public virtual Order Order { get; set; }
        
        [Required]
        [StringLength(50)]
        public string Method { get; set; }
        
        [StringLength(50)]
        public string Status { get; set; }
        
        public DateTime? PaymentDate { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Amount { get; set; }
    }
}