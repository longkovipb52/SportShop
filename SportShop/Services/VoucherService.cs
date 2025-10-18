using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Models;

namespace SportShop.Services
{
    public class VoucherValidationResult
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public Voucher? Voucher { get; set; }
        public UserVoucher? UserVoucher { get; set; }
        public decimal DiscountAmount { get; set; }
    }

    public class UserVoucherDTO
    {
        public int UserVoucherID { get; set; }
        public int VoucherID { get; set; }
        public string Code { get; set; } = string.Empty;
        public string DiscountDisplay { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool CanUse { get; set; }
        public string StatusDisplay { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
        public DateTime? EndDate { get; set; }
    }

    public class VoucherService
    {
        private readonly ApplicationDbContext _context;

        public VoucherService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Voucher?> GetByCodeAsync(string code)
        {
            if (string.IsNullOrWhiteSpace(code)) return null;
            code = code.Trim();
            return await _context.Vouchers.FirstOrDefaultAsync(v => v.Code == code);
        }

        // Lấy danh sách voucher có thể sử dụng của user
        public async Task<List<UserVoucherDTO>> GetAvailableVouchersForUserAsync(int userId)
        {
            var userVouchers = await _context.UserVouchers
                .Include(uv => uv.Voucher)
                .Where(uv => uv.UserID == userId && !uv.IsUsed)
                .Where(uv => uv.Voucher.IsActive && 
                            uv.Voucher.StartDate <= DateTime.Now && 
                            uv.Voucher.EndDate >= DateTime.Now)
                .Select(uv => new UserVoucherDTO
                {
                    UserVoucherID = uv.UserVoucherID,
                    VoucherID = uv.VoucherID,
                    Code = uv.Voucher.Code,
                    DiscountDisplay = uv.Voucher.DiscountDisplay,
                    Description = uv.Voucher.DiscountType == "Percentage" 
                        ? $"Giảm {uv.Voucher.DiscountValue}% tối đa cho đơn hàng"
                        : $"Giảm {uv.Voucher.DiscountValue:N0}đ cho đơn hàng",
                    CanUse = uv.CanUse,
                    StatusDisplay = uv.StatusDisplay,
                    StatusClass = uv.StatusClass,
                    EndDate = uv.Voucher.EndDate
                })
                .OrderBy(uv => uv.EndDate)
                .ToListAsync();

            return userVouchers;
        }

        // Validate voucher bằng UserVoucherID
        public async Task<VoucherValidationResult> ValidateAndCalculateByUserVoucherAsync(int userVoucherID, int userId, decimal orderSubtotal)
        {
            var result = new VoucherValidationResult();

            var userVoucher = await _context.UserVouchers
                .Include(uv => uv.Voucher)
                .FirstOrDefaultAsync(uv => uv.UserVoucherID == userVoucherID && uv.UserID == userId);

            if (userVoucher == null)
            {
                result.Success = false;
                result.Message = "Voucher không tồn tại hoặc không thuộc về bạn.";
                return result;
            }

            if (userVoucher.IsUsed)
            {
                result.Success = false;
                result.Message = "Voucher đã được sử dụng.";
                return result;
            }

            var voucher = userVoucher.Voucher;

            // Basic checks
            var now = DateTime.Now;
            if (!voucher.IsActive)
            {
                result.Success = false;
                result.Message = "Voucher đang ngưng hoạt động.";
                return result;
            }

            if (now < voucher.StartDate)
            {
                result.Success = false;
                result.Message = "Voucher chưa bắt đầu hiệu lực.";
                return result;
            }

            if (now > voucher.EndDate)
            {
                result.Success = false;
                result.Message = "Voucher đã hết hạn.";
                return result;
            }

            if (voucher.MinOrderAmount.HasValue && orderSubtotal < voucher.MinOrderAmount.Value)
            {
                result.Success = false;
                result.Message = $"Đơn hàng tối thiểu để áp dụng voucher là {voucher.MinOrderAmount.Value:N0}đ.";
                return result;
            }

            decimal discount = 0m;
            if (voucher.DiscountType.Equals("FixedAmount", StringComparison.OrdinalIgnoreCase))
            {
                discount = voucher.DiscountValue;
            }
            else
            {
                // Percentage
                discount = Math.Round(orderSubtotal * (voucher.DiscountValue / 100m), 2);
            }

            // Ensure not over subtotal
            if (discount > orderSubtotal) discount = orderSubtotal;
            if (discount < 0) discount = 0;

            result.Success = true;
            result.Message = "Áp dụng voucher thành công.";
            result.Voucher = voucher;
            result.UserVoucher = userVoucher;
            result.DiscountAmount = discount;
            return result;
        }

        // Đánh dấu voucher đã sử dụng
        public async Task<bool> MarkVoucherAsUsedAsync(int userVoucherID, int userId)
        {
            var userVoucher = await _context.UserVouchers
                .FirstOrDefaultAsync(uv => uv.UserVoucherID == userVoucherID && uv.UserID == userId);

            if (userVoucher == null || userVoucher.IsUsed)
                return false;

            userVoucher.IsUsed = true;
            userVoucher.UsedDate = DateTime.Now;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<VoucherValidationResult> ValidateAndCalculateAsync(string code, decimal orderSubtotal)
        {
            var result = new VoucherValidationResult();

            if (string.IsNullOrWhiteSpace(code))
            {
                result.Success = false;
                result.Message = "Vui lòng nhập mã voucher.";
                return result;
            }

            var voucher = await GetByCodeAsync(code);
            if (voucher == null)
            {
                result.Success = false;
                result.Message = "Mã voucher không tồn tại.";
                return result;
            }

            // Basic checks
            var now = DateTime.Now;
            if (!voucher.IsActive)
            {
                result.Success = false;
                result.Message = "Voucher đang ngưng hoạt động.";
                return result;
            }

            if (now < voucher.StartDate)
            {
                result.Success = false;
                result.Message = "Voucher chưa bắt đầu hiệu lực.";
                return result;
            }

            if (now > voucher.EndDate)
            {
                result.Success = false;
                result.Message = "Voucher đã hết hạn.";
                return result;
            }

            if (voucher.MinOrderAmount.HasValue && orderSubtotal < voucher.MinOrderAmount.Value)
            {
                result.Success = false;
                result.Message = $"Đơn hàng tối thiểu để áp dụng voucher là {voucher.MinOrderAmount.Value:N0}đ.";
                return result;
            }

            decimal discount = 0m;
            if (voucher.DiscountType.Equals("FixedAmount", StringComparison.OrdinalIgnoreCase))
            {
                discount = voucher.DiscountValue;
            }
            else
            {
                // Percentage
                discount = Math.Round(orderSubtotal * (voucher.DiscountValue / 100m), 2);
            }

            // Ensure not over subtotal
            if (discount > orderSubtotal) discount = orderSubtotal;
            if (discount < 0) discount = 0;

            result.Success = true;
            result.Message = "Áp dụng voucher thành công.";
            result.Voucher = voucher;
            result.DiscountAmount = discount;
            return result;
        }
    }
}
