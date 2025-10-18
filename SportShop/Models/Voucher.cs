using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportShop.Models
{
    [Table("Voucher")]
    public class Voucher
    {
        [Key]
        public int VoucherID { get; set; }

        [Required(ErrorMessage = "Mã voucher là bắt buộc")]
        [StringLength(50)]
        [Display(Name = "Mã voucher")]
        public string Code { get; set; } = string.Empty;

        [Required(ErrorMessage = "Loại giảm giá là bắt buộc")]
        [StringLength(20)]
        [Display(Name = "Loại giảm giá")]
        public string DiscountType { get; set; } = "Percentage"; // "Percentage" or "FixedAmount"

        [Required(ErrorMessage = "Giá trị giảm giá là bắt buộc")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá trị giảm giá phải lớn hơn 0")]
        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giá trị giảm")]
        public decimal DiscountValue { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        [Display(Name = "Giá trị đơn hàng tối thiểu")]
        public decimal? MinOrderAmount { get; set; }

        [Required(ErrorMessage = "Ngày bắt đầu là bắt buộc")]
        [Display(Name = "Ngày bắt đầu")]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "Ngày kết thúc là bắt buộc")]
        [Display(Name = "Ngày kết thúc")]
        public DateTime EndDate { get; set; }

        [Display(Name = "Kích hoạt")]
        public bool IsActive { get; set; } = true;

        // Navigation properties
        public virtual ICollection<Order> Orders { get; set; } = new List<Order>();

        // Computed properties
        [NotMapped]
        public bool IsExpired => DateTime.Now > EndDate;

        [NotMapped]
        public bool IsNotStarted => DateTime.Now < StartDate;

        [NotMapped]
        public bool IsValid => IsActive && !IsExpired && !IsNotStarted;

        [NotMapped]
        public string DiscountDisplay => DiscountType == "Percentage" 
            ? $"{DiscountValue}%" 
            : $"{DiscountValue:N0}đ";

        [NotMapped]
        public string StatusDisplay
        {
            get
            {
                if (!IsActive) return "Ngưng hoạt động";
                if (IsNotStarted) return "Chưa bắt đầu";
                if (IsExpired) return "Đã hết hạn";
                return "Đang hoạt động";
            }
        }

        [NotMapped]
        public string StatusClass
        {
            get
            {
                if (!IsActive) return "badge bg-secondary";
                if (IsNotStarted) return "badge bg-info";
                if (IsExpired) return "badge bg-danger";
                return "badge bg-success";
            }
        }
    }
}
