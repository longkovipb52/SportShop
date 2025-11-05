using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportShop.Models
{
    [Table("ColorOption")]
    public class ColorOption
    {
        [Key]
        public int ColorOptionID { get; set; }
        
        [Required(ErrorMessage = "Tên màu không được để trống")]
        [StringLength(100, ErrorMessage = "Tên màu không được vượt quá 100 ký tự")]
        public string ColorName { get; set; } = string.Empty;
        
        [StringLength(7, ErrorMessage = "Mã màu phải có định dạng #RRGGBB")]
        [RegularExpression(@"^#[0-9A-Fa-f]{6}$", ErrorMessage = "Mã màu không hợp lệ (phải có định dạng #RRGGBB)")]
        public string? HexCode { get; set; }
        
        public bool IsActive { get; set; } = true;
        
        public DateTime? CreatedAt { get; set; } = DateTime.Now;
    }
}
