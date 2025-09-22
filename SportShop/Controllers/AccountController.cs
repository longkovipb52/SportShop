using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Models;
using SportShop.Models.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;

namespace SportShop.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                try
                {
                    // Lấy thông tin user
                    var user = await _context.Users
                        .Include(u => u.Role)
                        .FirstOrDefaultAsync(u => u.Username == model.Username);

                    if (user == null)
                    {
                        ModelState.AddModelError(string.Empty, "Tên đăng nhập không tồn tại.");
                        return View(model);
                    }

                    // Kiểm tra password
                    if (VerifyPassword(model.Password, user.PasswordHash))
                    {
                        // Lưu thông tin user vào session
                        HttpContext.Session.SetInt32("UserId", user.UserID);
                        HttpContext.Session.SetString("Username", user.Username);
                        
                        // Xử lý an toàn cho các trường có thể null
                        if (!string.IsNullOrEmpty(user.FullName))
                        {
                            HttpContext.Session.SetString("FullName", user.FullName);
                        }
                        
                        // Xử lý trường hợp Role có thể null
                        string roleName = "Customer"; // Mặc định
                        if (user.Role != null && !string.IsNullOrEmpty(user.Role.RoleName))
                        {
                            roleName = user.Role.RoleName;
                        }
                        HttpContext.Session.SetString("Role", roleName);

                        TempData["SuccessMessage"] = "Đăng nhập thành công!";
                        
                        if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                        {
                            return Redirect(returnUrl);
                        }
                        else
                        {
                            return RedirectToAction("Index", "Home");
                        }
                    }

                    ModelState.AddModelError(string.Empty, "Mật khẩu không đúng.");
                }
                catch (Exception ex)
                {
                    // Log lỗi chi tiết
                    Console.WriteLine($"Login error: {ex.Message}");
                    
                    ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi trong quá trình đăng nhập. Vui lòng thử lại sau.");
                }
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View(new RegisterViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    if (await _context.Users.AnyAsync(u => u.Username == model.Username))
                    {
                        ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại.");
                        return View(model);
                    }

                    if (await _context.Users.AnyAsync(u => u.Email == model.Email))
                    {
                        ModelState.AddModelError("Email", "Email đã được sử dụng.");
                        return View(model);
                    }

                    // Lấy RoleID của Customer
                    var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Customer");
                    if (customerRole == null)
                    {
                        // Nếu không tồn tại role Customer, tạo mới
                        customerRole = new Role { RoleName = "Customer" };
                        _context.Roles.Add(customerRole);
                        await _context.SaveChangesAsync();
                    }

                    var user = new User
                    {
                        Username = model.Username,
                        FullName = model.FullName,
                        Email = model.Email,
                        PasswordHash = HashPassword(model.Password), // Sử dụng BCrypt
                        Phone = model.Phone,
                        Address = model.Address, // Thêm địa chỉ
                        RoleID = customerRole.RoleID,
                        CreatedAt = DateTime.Now
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Đăng ký tài khoản thành công! Vui lòng đăng nhập.";
                    return RedirectToAction("Login");
                }
                catch (Exception ex)
                {
                    // Log lỗi
                    Console.WriteLine("Register error: " + ex.Message);
                    ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi đăng ký. Vui lòng thử lại sau.");
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            // Xóa session
            HttpContext.Session.Clear();
            
            TempData["SuccessMessage"] = "Đăng xuất thành công!";
            return RedirectToAction("Index", "Home");
        }

        // Phương thức mã hóa mật khẩu bằng BCrypt
        private string HashPassword(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, workFactor: 12);
        }

        // Phương thức kiểm tra mật khẩu
        private bool VerifyPassword(string password, string passwordHash)
        {
            try
            {
                // Xử lý trường hợp passwordHash là null
                if (string.IsNullOrEmpty(passwordHash))
                {
                    return false;
                }
                
                // Kiểm tra nếu là BCrypt hash
                if (passwordHash.StartsWith("$2a$") || passwordHash.StartsWith("$2b$") || passwordHash.StartsWith("$2y$"))
                {
                    return BCrypt.Net.BCrypt.Verify(password, passwordHash);
                }
                else
                {
                    // Tài khoản mẫu trong DB mà chưa được mã hóa bằng BCrypt
                    // Chỉ dùng cho mục đích demo, với tài khoản mẫu
                    if ((passwordHash == "+AQb8XYrm8ENLSW8sko8ZI=" || passwordHash.Length < 30) && 
                        (password == "123456" || password == "password" || password == "admin"))
                    {
                        return true;
                    }
                    
                    // Trường hợp mật khẩu đã được mã hóa bằng cách khác
                    return false;
                }
            }
            catch (Exception ex)
            {
                // Log lỗi
                Console.WriteLine($"Password verification error: {ex.Message}");
                return false;
            }
        }

        // GET: Account/Profile - Hiển thị trang tài khoản
        public async Task<IActionResult> Profile()
        {
            // Kiểm tra xem user đã đăng nhập chưa
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }
            var user = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserID == userId.Value);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var viewModel = new ProfileViewModel
            {
                UserID = user.UserID,
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                Phone = user.Phone ?? "",
                Address = user.Address ?? "",
                RoleName = user.Role.RoleName,
                CreatedAt = user.CreatedAt ?? DateTime.Now
            };

            return View(viewModel);
        }

        // POST: Account/UpdateProfile - Cập nhật thông tin tài khoản
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(UpdateProfileViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Vui lòng kiểm tra lại thông tin đã nhập.";
                return RedirectToAction("Profile");
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(userId.Value);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Kiểm tra email đã tồn tại chưa (ngoại trừ email hiện tại)
            var existingEmailUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == model.Email && u.UserID != userId.Value);
            
            if (existingEmailUser != null)
            {
                TempData["ErrorMessage"] = "Email này đã được sử dụng bởi tài khoản khác.";
                return RedirectToAction("Profile");
            }

            // Cập nhật thông tin
            user.FullName = model.FullName;
            user.Email = model.Email;
            user.Phone = model.Phone;
            user.Address = model.Address;

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi cập nhật thông tin. Vui lòng thử lại.";
            }

            return RedirectToAction("Profile");
        }

        // POST: Account/ChangePassword - Đổi mật khẩu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Vui lòng kiểm tra lại thông tin đã nhập.";
                return RedirectToAction("Profile");
            }

            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var user = await _context.Users.FindAsync(userId.Value);

            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Kiểm tra mật khẩu hiện tại
            if (!VerifyPassword(model.CurrentPassword, user.PasswordHash))
            {
                TempData["ErrorMessage"] = "Mật khẩu hiện tại không đúng.";
                return RedirectToAction("Profile");
            }

            // Cập nhật mật khẩu mới với BCrypt
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

            try
            {
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi đổi mật khẩu. Vui lòng thử lại.";
            }

            return RedirectToAction("Profile");
        }
    }
}