using System.ComponentModel.DataAnnotations;

namespace SportShop.Models.ViewModels
{
    public class ContactFormViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public required string Name { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public required string Email { get; set; }
        
        public string? Title { get; set; }
        
        public string? Phone { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập nội dung")]
        public required string Message { get; set; }
    }
}
