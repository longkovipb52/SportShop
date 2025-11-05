using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportShop.Models
{
    [Table("SizeOption")]
    public class SizeOption
    {
        [Key]
        public int SizeOptionID { get; set; }
        
        public int? CategoryID { get; set; }
        
        [Required(ErrorMessage = "Tên kích thước không được để trống")]
        [StringLength(50, ErrorMessage = "Tên kích thước không được vượt quá 50 ký tự")]
        public string SizeName { get; set; } = string.Empty;
        
        public int DisplayOrder { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
        
        // Navigation properties
        public Category? Category { get; set; }
    }
}
