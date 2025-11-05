using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportShop.Models
{
    [Table("AttributeType")]
    public class AttributeType
    {
        [Key]
        public int AttributeTypeID { get; set; }
        
        [Required(ErrorMessage = "Tên loại thuộc tính không được để trống")]
        [StringLength(100, ErrorMessage = "Tên loại thuộc tính không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;
        
        [Required]
        [StringLength(50)]
        public string InputType { get; set; } = "dropdown"; // dropdown, color, text, number
        
        public bool IsActive { get; set; } = true;
        
        public DateTime CreatedDate { get; set; } = DateTime.Now;
    }
}
