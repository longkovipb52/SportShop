using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportShop.Models
{
    [Table("SubCategory")]
    public class SubCategory
    {
        [Key]
        public int SubCategoryID { get; set; }
        
        [Required(ErrorMessage = "Tên danh mục con là bắt buộc")]
        [StringLength(100, ErrorMessage = "Tên danh mục con không được vượt quá 100 ký tự")]
        [Display(Name = "Tên danh mục con")]
        public required string Name { get; set; }
        
        [StringLength(500, ErrorMessage = "Mô tả không được vượt quá 500 ký tự")]
        [Display(Name = "Mô tả")]
        public string? Description { get; set; }
        
        [Display(Name = "Hình ảnh")]
        public string? ImageURL { get; set; }
        
        [Required]
        [Display(Name = "Danh mục cha")]
        public int CategoryID { get; set; }
        
        [Display(Name = "Thứ tự hiển thị")]
        public int DisplayOrder { get; set; } = 0;
        
        [Display(Name = "Trạng thái")]
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        [ForeignKey("CategoryID")]
        public Category? Category { get; set; }
        
        public ICollection<Product>? Products { get; set; }
    }
}
