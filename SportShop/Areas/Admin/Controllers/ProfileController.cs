using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Models;
using SportShop.Models.ViewModels;
using System.Security.Claims;

namespace SportShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ProfileController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProfileController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Profile
        public async Task<IActionResult> Index()
        {
            try
            {
                // Lấy thông tin user hiện tại từ Claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    TempData["ErrorMessage"] = "Không thể xác định thông tin người dùng.";
                    return RedirectToAction("Index", "Dashboard");
                }

                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.UserID == userId);
                    
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng.";
                    return RedirectToAction("Index", "Dashboard");
                }

                var profileViewModel = new ProfileViewModel
                {
                    UserID = user.UserID,
                    Username = user.Username,
                    FullName = user.FullName,
                    Email = user.Email,
                    Phone = user.Phone ?? string.Empty,
                    Address = user.Address ?? string.Empty,
                    RoleName = user.Role?.RoleName ?? "Admin",
                    CreatedAt = user.CreatedAt ?? DateTime.Now
                };

                return View(profileViewModel);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi tải thông tin cá nhân: " + ex.Message;
                return RedirectToAction("Index", "Dashboard");
            }
        }

        // POST: Admin/Profile
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(ProfileViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Lấy thông tin user hiện tại từ Claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    TempData["ErrorMessage"] = "Không thể xác định thông tin người dùng.";
                    return View(model);
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng.";
                    return View(model);
                }

                // Kiểm tra email đã tồn tại chưa (trừ email hiện tại)
                var existingEmailUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == model.Email && u.UserID != userId);
                if (existingEmailUser != null)
                {
                    ModelState.AddModelError("Email", "Email này đã được sử dụng bởi tài khoản khác.");
                    return View(model);
                }

                // Kiểm tra username đã tồn tại chưa (trừ username hiện tại)
                var existingUsernameUser = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username && u.UserID != userId);
                if (existingUsernameUser != null)
                {
                    ModelState.AddModelError("Username", "Tên đăng nhập này đã được sử dụng.");
                    return View(model);
                }

                // Cập nhật thông tin
                user.Username = model.Username;
                user.FullName = model.FullName;
                user.Email = model.Email;
                user.Phone = model.Phone;
                user.Address = model.Address;

                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cập nhật thông tin cá nhân thành công!";
                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật thông tin: " + ex.Message;
                return View(model);
            }
        }

        // GET: Admin/Profile/ChangePassword
        public IActionResult ChangePassword()
        {
            return View(new ChangePasswordViewModel());
        }

        // POST: Admin/Profile/ChangePassword
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                // Lấy thông tin user hiện tại từ Claims
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    TempData["ErrorMessage"] = "Không thể xác định thông tin người dùng.";
                    return View(model);
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    TempData["ErrorMessage"] = "Không tìm thấy thông tin người dùng.";
                    return View(model);
                }

                // Kiểm tra mật khẩu cũ
                if (!BCrypt.Net.BCrypt.Verify(model.CurrentPassword, user.PasswordHash))
                {
                    ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng.");
                    return View(model);
                }

                // Cập nhật mật khẩu mới
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);
                
                _context.Users.Update(user);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
                return RedirectToAction("Index");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi đổi mật khẩu: " + ex.Message;
                return View(model);
            }
        }
    }
}