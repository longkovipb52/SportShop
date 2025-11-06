using System.ComponentModel.DataAnnotations;

namespace SportShop.Models.ViewModels
{
    public class VerifyOtpViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mã OTP")]
        [StringLength(6, MinimumLength = 6, ErrorMessage = "Mã OTP phải có 6 chữ số")]
        [RegularExpression(@"^\d{6}$", ErrorMessage = "Mã OTP chỉ bao gồm 6 chữ số")]
        [Display(Name = "Mã OTP")]
        public string OtpCode { get; set; } = string.Empty;

        public string Email { get; set; } = string.Empty;
    }
}
