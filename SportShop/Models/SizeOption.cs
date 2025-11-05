using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportShop.Models
{
    [Table("SizeOption")]
    public class SizeOption
    {
        [Key]
        public int SizeOptionID { get; set; }
        
        [Display(Name = "Danh mục con")]
        [Column("CategoryID")] // Mapping tới cột CategoryID trong database (thực chất là SubCategoryID)
        public int? SubCategoryID { get; set; }
        
        [Required(ErrorMessage = "Tên kích thước không được để trống")]
        [StringLength(50, ErrorMessage = "Tên kích thước không được vượt quá 50 ký tự")]
        public string SizeName { get; set; } = string.Empty;
        
        public int DisplayOrder { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        [ForeignKey("SubCategoryID")]
        public SubCategory? SubCategory { get; set; }
    }
}
