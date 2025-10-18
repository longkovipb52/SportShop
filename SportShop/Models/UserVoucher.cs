using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportShop.Models
{
    [Table("UserVoucher")]
    public class UserVoucher
    {
        [Key]
        public int UserVoucherID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        public int VoucherID { get; set; }

        [Display(Name = "Ngày nhận")]
        public DateTime AssignedDate { get; set; } = DateTime.Now;

        [Display(Name = "Đã sử dụng")]
        public bool IsUsed { get; set; } = false;

        [Display(Name = "Ngày sử dụng")]
        public DateTime? UsedDate { get; set; }

        // Navigation properties
        [ForeignKey("UserID")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("VoucherID")]
        public virtual Voucher Voucher { get; set; } = null!;

        // Computed properties
        [NotMapped]
        public bool CanUse => !IsUsed && Voucher.IsValid;

        [NotMapped]
        public string StatusDisplay
        {
            get
            {
                if (IsUsed) return "Đã sử dụng";
                if (!Voucher.IsActive) return "Ngưng hoạt động";
                if (Voucher.IsExpired) return "Đã hết hạn";
                if (Voucher.IsNotStarted) return "Chưa bắt đầu";
                return "Có thể sử dụng";
            }
        }

        [NotMapped]
        public string StatusClass
        {
            get
            {
                if (IsUsed) return "badge bg-secondary";
                if (!Voucher.IsActive || Voucher.IsExpired) return "badge bg-danger";
                if (Voucher.IsNotStarted) return "badge bg-info";
                return "badge bg-success";
            }
        }
    }
}