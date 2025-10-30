using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Services;

namespace SportShop.Controllers
{
    public class VoucherController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly VoucherService _voucherService;

        public VoucherController(ApplicationDbContext context, VoucherService voucherService)
        {
            _context = context;
            _voucherService = voucherService;
        }

        // Trang hiển thị voucher của user
        public async Task<IActionResult> MyVouchers()
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (!userId.HasValue)
            {
                return RedirectToAction("Login", "Account");
            }

            try
            {
                // Lấy tất cả voucher của user
                var userVouchers = await _context.UserVouchers
                    .Include(uv => uv.Voucher)
                    .Where(uv => uv.UserID == userId.Value)
                    .OrderByDescending(uv => uv.AssignedDate)
                    .ToListAsync();

                // Phân loại voucher
                var availableVouchers = userVouchers
                    .Where(uv => !uv.IsUsed && uv.Voucher.EndDate > DateTime.Now && uv.Voucher.IsActive)
                    .ToList();

                var usedVouchers = userVouchers
                    .Where(uv => uv.IsUsed)
                    .ToList();

                var expiredVouchers = userVouchers
                    .Where(uv => !uv.IsUsed && (uv.Voucher.EndDate <= DateTime.Now || !uv.Voucher.IsActive))
                    .ToList();

                ViewBag.AvailableVouchers = availableVouchers;
                ViewBag.UsedVouchers = usedVouchers;
                ViewBag.ExpiredVouchers = expiredVouchers;

                return View(userVouchers);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Có lỗi khi tải voucher: {ex.Message}";
                return View(new List<SportShop.Models.UserVoucher>());
            }
        }
    }
}
