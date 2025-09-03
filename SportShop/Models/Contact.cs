using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportShop.Models
{
    [Table("Contact")]
    public class Contact
    {
        [Key]
        public int ContactID { get; set; }
        
        public int? UserID { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string Name { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string Email { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập nội dung")]
        public string Message { get; set; }
        
        public string Status { get; set; }
        
        public DateTime? CreatedAt { get; set; }
        
        public string Reply { get; set; }
        
        public int? RepliedBy { get; set; }
        
        public DateTime? RepliedAt { get; set; }
    }
}