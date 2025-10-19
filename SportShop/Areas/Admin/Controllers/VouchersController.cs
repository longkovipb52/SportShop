using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Models;

namespace SportShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class VouchersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public VouchersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Vouchers
        public async Task<IActionResult> Index(string search = "", string status = "", int page = 1, int pageSize = 10)
        {
            ViewData["Title"] = "Quản lý Voucher";
            
            var query = _context.Vouchers.AsQueryable();

            // Tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(v => v.Code.Contains(search));
                ViewData["CurrentSearch"] = search;
            }

            // Lọc theo trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                var now = DateTime.Now;
                query = status switch
                {
                    "active" => query.Where(v => v.IsActive && v.StartDate <= now && v.EndDate >= now),
                    "inactive" => query.Where(v => !v.IsActive),
                    "expired" => query.Where(v => v.EndDate < now),
                    "upcoming" => query.Where(v => v.StartDate > now),
                    _ => query
                };
                ViewData["CurrentStatus"] = status;
            }

            // Đếm tổng số bản ghi
            var totalRecords = await query.CountAsync();
            
            // Phân trang
            var vouchers = await query
                .OrderByDescending(v => v.VoucherID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Include(v => v.Orders)
                .ToListAsync();

            // Thống kê
            var now2 = DateTime.Now;
            ViewData["TotalVouchers"] = await _context.Vouchers.CountAsync();
            ViewData["ActiveVouchers"] = await _context.Vouchers
                .CountAsync(v => v.IsActive && v.StartDate <= now2 && v.EndDate >= now2);
            ViewData["ExpiredVouchers"] = await _context.Vouchers
                .CountAsync(v => v.EndDate < now2);
            ViewData["UpcomingVouchers"] = await _context.Vouchers
                .CountAsync(v => v.StartDate > now2);

            // Thông tin phân trang
            ViewData["CurrentPage"] = page;
            ViewData["PageSize"] = pageSize;
            ViewData["TotalPages"] = (int)Math.Ceiling((double)totalRecords / pageSize);
            ViewData["TotalRecords"] = totalRecords;

            return View(vouchers);
        }

        // GET: Admin/Vouchers/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            ViewData["Title"] = "Chi tiết Voucher";
            
            if (id == null)
            {
                return NotFound();
            }

            var voucher = await _context.Vouchers
                .Include(v => v.Orders)
                    .ThenInclude(o => o.User)
                .FirstOrDefaultAsync(m => m.VoucherID == id);

            if (voucher == null)
            {
                return NotFound();
            }

            return View(voucher);
        }

        // GET: Admin/Vouchers/Create
        public IActionResult Create()
        {
            ViewData["Title"] = "Tạo Voucher mới";
            
            var voucher = new Voucher
            {
                StartDate = DateTime.Now,
                EndDate = DateTime.Now.AddMonths(1),
                IsActive = true,
                DiscountType = "Percentage"
            };

            return View(voucher);
        }

        // POST: Admin/Vouchers/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Code,DiscountType,DiscountValue,MinOrderAmount,StartDate,EndDate,IsActive")] Voucher voucher)
        {
            ViewData["Title"] = "Tạo Voucher mới";

            // Kiểm tra mã voucher đã tồn tại
            if (await _context.Vouchers.AnyAsync(v => v.Code == voucher.Code))
            {
                ModelState.AddModelError("Code", "Mã voucher đã tồn tại");
            }

            // Validate ngày
            if (voucher.EndDate <= voucher.StartDate)
            {
                ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu");
            }

            // Validate giá trị giảm giá
            if (voucher.DiscountType == "Percentage" && voucher.DiscountValue > 100)
            {
                ModelState.AddModelError("DiscountValue", "Giá trị giảm giá theo phần trăm không được vượt quá 100%");
            }

            if (ModelState.IsValid)
            {
                _context.Add(voucher);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Tạo voucher thành công!";
                return RedirectToAction(nameof(Index));
            }

            return View(voucher);
        }

        // GET: Admin/Vouchers/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            ViewData["Title"] = "Chỉnh sửa Voucher";
            
            if (id == null)
            {
                return NotFound();
            }

            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
            {
                return NotFound();
            }

            return View(voucher);
        }

        // POST: Admin/Vouchers/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("VoucherID,Code,DiscountType,DiscountValue,MinOrderAmount,StartDate,EndDate,IsActive")] Voucher voucher)
        {
            ViewData["Title"] = "Chỉnh sửa Voucher";
            
            if (id != voucher.VoucherID)
            {
                return NotFound();
            }

            // Kiểm tra mã voucher trùng (trừ voucher hiện tại)
            if (await _context.Vouchers.AnyAsync(v => v.Code == voucher.Code && v.VoucherID != id))
            {
                ModelState.AddModelError("Code", "Mã voucher đã tồn tại");
            }

            // Validate ngày
            if (voucher.EndDate <= voucher.StartDate)
            {
                ModelState.AddModelError("EndDate", "Ngày kết thúc phải sau ngày bắt đầu");
            }

            // Validate giá trị giảm giá
            if (voucher.DiscountType == "Percentage" && voucher.DiscountValue > 100)
            {
                ModelState.AddModelError("DiscountValue", "Giá trị giảm giá theo phần trăm không được vượt quá 100%");
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(voucher);
                    await _context.SaveChangesAsync();
                    TempData["SuccessMessage"] = "Cập nhật voucher thành công!";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!VoucherExists(voucher.VoucherID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(voucher);
        }

        // GET: Admin/Vouchers/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            ViewData["Title"] = "Xóa Voucher";
            
            if (id == null)
            {
                return NotFound();
            }

            var voucher = await _context.Vouchers
                .Include(v => v.Orders)
                .FirstOrDefaultAsync(m => m.VoucherID == id);

            if (voucher == null)
            {
                return NotFound();
            }

            return View(voucher);
        }

        // POST: Admin/Vouchers/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var voucher = await _context.Vouchers
                .Include(v => v.Orders)
                .FirstOrDefaultAsync(v => v.VoucherID == id);

            if (voucher == null)
            {
                return NotFound();
            }

            // Kiểm tra nếu voucher đã được sử dụng
            if (voucher.Orders.Any())
            {
                TempData["ErrorMessage"] = "Không thể xóa voucher đã được sử dụng trong đơn hàng!";
                return RedirectToAction(nameof(Index));
            }

            _context.Vouchers.Remove(voucher);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Xóa voucher thành công!";
            return RedirectToAction(nameof(Index));
        }

        // POST: Admin/Vouchers/ToggleStatus/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var voucher = await _context.Vouchers.FindAsync(id);
            if (voucher == null)
            {
                return NotFound();
            }

            voucher.IsActive = !voucher.IsActive;
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = voucher.IsActive 
                ? "Đã kích hoạt voucher!" 
                : "Đã tắt voucher!";

            return RedirectToAction(nameof(Index));
        }

        private bool VoucherExists(int id)
        {
            return _context.Vouchers.Any(e => e.VoucherID == id);
        }
    }
}
