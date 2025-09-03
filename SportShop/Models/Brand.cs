using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace SportShop.Models
{
    [Table("Brand")]
    public class Brand
    {
        [Key]
        public int BrandID { get; set; }
        
        [Required(ErrorMessage = "Tên thương hiệu là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên thương hiệu không được vượt quá 100 ký tự")]
        public string Name { get; set; }
        
        [StringLength(255)]
        public string Description { get; set; }
        
        [StringLength(255)]
        public string Logo { get; set; }
        
        public DateTime? CreatedAt { get; set; }
        
        // Navigation property
        public virtual ICollection<Product> Products { get; set; }
    }
}