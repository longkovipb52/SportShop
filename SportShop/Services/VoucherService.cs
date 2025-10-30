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

        // ==================== EVENT-BASED VOUCHER ASSIGNMENT ====================

        /// <summary>
        /// Phương án 1: Tặng voucher chào mừng khi user đăng ký
        /// </summary>
        public async Task<bool> AssignWelcomeVoucherAsync(int userId)
        {
            try
            {
                // Kiểm tra user đã nhận welcome voucher chưa
                var hasWelcomeVoucher = await _context.UserVouchers
                    .AnyAsync(uv => uv.UserID == userId && uv.Voucher.Code == "WELCOME10");

                if (hasWelcomeVoucher)
                    return false; // Đã nhận rồi

                // Tìm voucher WELCOME10
                var welcomeVoucher = await _context.Vouchers
                    .FirstOrDefaultAsync(v => v.Code == "WELCOME10" && v.IsActive);

                if (welcomeVoucher == null)
                    return false; // Không tìm thấy voucher

                // Assign voucher cho user
                var userVoucher = new UserVoucher
                {
                    UserID = userId,
                    VoucherID = welcomeVoucher.VoucherID,
                    AssignedDate = DateTime.Now,
                    IsUsed = false
                };

                _context.UserVouchers.Add(userVoucher);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Phương án 5a: Tặng voucher sinh nhật (Disabled - User model không có DateOfBirth)
        /// Nếu muốn dùng, cần thêm field DateOfBirth vào bảng User
        /// </summary>
        /*
        public async Task<bool> AssignBirthdayVoucherAsync(int userId)
        {
            // TODO: Thêm DateOfBirth vào User model trước
            return await Task.FromResult(false);
        }
        */

        /// <summary>
        /// Phương án 5b: Tặng voucher sau đơn hàng đầu tiên
        /// </summary>
        public async Task<bool> AssignFirstOrderVoucherAsync(int userId)
        {
            try
            {
                // Đếm số đơn hàng đã hoàn thành
                var completedOrders = await _context.Orders
                    .CountAsync(o => o.UserID == userId && o.Status == "Hoàn thành");

                // Chỉ tặng sau đơn đầu tiên
                if (completedOrders != 1)
                    return false;

                // Kiểm tra đã nhận voucher FIRSTORDER chưa
                var hasFirstOrderVoucher = await _context.UserVouchers
                    .AnyAsync(uv => uv.UserID == userId && uv.Voucher.Code == "FIRSTORDER");

                if (hasFirstOrderVoucher)
                    return false;

                // Tìm voucher FIRSTORDER
                var firstOrderVoucher = await _context.Vouchers
                    .FirstOrDefaultAsync(v => v.Code == "FIRSTORDER" && v.IsActive);

                if (firstOrderVoucher == null)
                    return false;

                // Assign voucher
                var userVoucher = new UserVoucher
                {
                    UserID = userId,
                    VoucherID = firstOrderVoucher.VoucherID,
                    AssignedDate = DateTime.Now,
                    IsUsed = false
                };

                _context.UserVouchers.Add(userVoucher);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Phương án 5c: Tặng voucher khi tổng giá trị đơn hàng đạt milestone
        /// </summary>
        public async Task<bool> AssignMilestoneVoucherAsync(int userId, decimal milestone)
        {
            try
            {
                // Tính tổng giá trị các đơn hàng đã hoàn thành
                var totalSpent = await _context.Orders
                    .Where(o => o.UserID == userId && o.Status == "Hoàn thành")
                    .SumAsync(o => o.TotalAmount);

                // Kiểm tra đã đạt milestone chưa
                if (totalSpent < milestone)
                    return false;

                // Xác định voucher code dựa vào milestone
                string? voucherCode = milestone switch
                {
                    1000000 => "VIP1M",    // 1 triệu
                    5000000 => "VIP5M",    // 5 triệu
                    10000000 => "VIP10M",  // 10 triệu
                    _ => null
                };

                if (string.IsNullOrEmpty(voucherCode))
                    return false;

                // Kiểm tra đã nhận voucher này chưa
                var hasMilestoneVoucher = await _context.UserVouchers
                    .AnyAsync(uv => uv.UserID == userId && uv.Voucher.Code == voucherCode);

                if (hasMilestoneVoucher)
                    return false;

                // Tìm voucher
                var milestoneVoucher = await _context.Vouchers
                    .FirstOrDefaultAsync(v => v.Code == voucherCode && v.IsActive);

                if (milestoneVoucher == null)
                    return false;

                // Assign voucher
                var userVoucher = new UserVoucher
                {
                    UserID = userId,
                    VoucherID = milestoneVoucher.VoucherID,
                    AssignedDate = DateTime.Now,
                    IsUsed = false
                };

                _context.UserVouchers.Add(userVoucher);
                await _context.SaveChangesAsync();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        /// <summary>
        /// Kiểm tra và tặng tất cả voucher milestone mà user đủ điều kiện
        /// </summary>
        public async Task CheckAndAssignAllMilestonesAsync(int userId)
        {
            await AssignMilestoneVoucherAsync(userId, 1000000);   // 1M
            await AssignMilestoneVoucherAsync(userId, 5000000);   // 5M
            await AssignMilestoneVoucherAsync(userId, 10000000);  // 10M
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
