using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Models;
using SportShop.Models.ViewModels;
using SportShop.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using BCrypt.Net;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using System.Security.Claims;

namespace SportShop.Controllers
{
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly VoucherService _voucherService;
        private readonly EmailService _emailService;

        public AccountController(ApplicationDbContext context, VoucherService voucherService, EmailService emailService)
        {
            _context = context;
            _voucherService = voucherService;
            _emailService = emailService;
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
                        
                        // Đánh dấu đăng nhập thành công cho recommendation system
                        HttpContext.Session.SetString("LoginTimestamp", DateTime.UtcNow.ToString());
                        
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
                    // Kiểm tra tên đăng nhập đã tồn tại
                    var existingUsername = await _context.Users.AnyAsync(u => u.Username == model.Username);
                    
                    if (existingUsername)
                    {
                        ModelState.AddModelError("Username", "Tên đăng nhập đã tồn tại");
                        return View(model);
                    }

                    // Kiểm tra email đã tồn tại
                    var existingEmail = await _context.Users.AnyAsync(u => u.Email == model.Email);
                    
                    if (existingEmail)
                    {
                        ModelState.AddModelError("Email", "Email đã được sử dụng");
                        return View(model);
                    }

                    // Kiểm tra số điện thoại đã tồn tại
                    if (!string.IsNullOrEmpty(model.Phone))
                    {
                        var existingPhone = await _context.Users.AnyAsync(u => u.Phone == model.Phone);
                        
                        if (existingPhone)
                        {
                            ModelState.AddModelError("Phone", "Số điện thoại đã được sử dụng");
                            return View(model);
                        }
                    }

                    // Tạo mã OTP
                    var otpCode = EmailService.GenerateOtpCode();
                    var otpExpiry = DateTime.Now.AddMinutes(5); // OTP có hiệu lực 5 phút
                    
                    // Lưu thông tin đăng ký và OTP vào Session
                    HttpContext.Session.SetString("RegisterData", System.Text.Json.JsonSerializer.Serialize(model));
                    HttpContext.Session.SetString("OtpCode", otpCode);
                    HttpContext.Session.SetString("OtpExpiry", otpExpiry.ToString("o"));
                    HttpContext.Session.SetString("OtpEmail", model.Email);
                    
                    // Gửi email OTP
                    var emailSent = await _emailService.SendOtpEmailAsync(model.Email, model.FullName, otpCode);
                    
                    if (!emailSent)
                    {
                        ModelState.AddModelError(string.Empty, "Không thể gửi mã xác thực. Vui lòng thử lại!");
                        return View(model);
                    }

                    // Lưu thông báo success vào Session
                    HttpContext.Session.SetString("SuccessMessage", $"Mã xác thực đã được gửi đến email {model.Email}. Vui lòng kiểm tra hộp thư!");
                    
                    return RedirectToAction("VerifyOtp");
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

        // GET: Account/VerifyOtp - Trang nhập mã OTP (dùng chung cho đăng ký và quên mật khẩu)
        [HttpGet]
        public IActionResult VerifyOtp(bool isResetPassword = false)
        {
            string? email;
            
            if (isResetPassword)
            {
                // Quên mật khẩu
                var username = HttpContext.Session.GetString("ResetPasswordUsername");
                email = HttpContext.Session.GetString("ResetPasswordEmail");
                
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email))
                {
                    return RedirectToAction("ForgotPassword");
                }
                
                ViewBag.IsResetPassword = true;
            }
            else
            {
                // Đăng ký
                email = HttpContext.Session.GetString("OtpEmail");
                
                if (string.IsNullOrEmpty(email))
                {
                    return RedirectToAction("Register");
                }
                
                ViewBag.IsResetPassword = false;
            }
            
            var model = new VerifyOtpViewModel
            {
                Email = email
            };
            
            return View(model);
        }

        // POST: Account/VerifyOtp - Xác thực mã OTP (dùng chung cho đăng ký và quên mật khẩu)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyOtp(VerifyOtpViewModel model, bool isResetPassword = false)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.IsResetPassword = isResetPassword;
                return View(model);
            }

            try
            {
                if (isResetPassword)
                {
                    // Xử lý quên mật khẩu
                    var sessionOtp = HttpContext.Session.GetString("ResetPasswordOtp");
                    var sessionExpiry = HttpContext.Session.GetString("ResetPasswordOtpExpiry");
                    var sessionEmail = HttpContext.Session.GetString("ResetPasswordEmail");
                    var username = HttpContext.Session.GetString("ResetPasswordUsername");

                    if (string.IsNullOrEmpty(sessionOtp) || string.IsNullOrEmpty(sessionExpiry) || 
                        string.IsNullOrEmpty(sessionEmail) || string.IsNullOrEmpty(username))
                    {
                        return RedirectToAction("ForgotPassword");
                    }

                    // Kiểm tra email
                    if (model.Email != sessionEmail)
                    {
                        return RedirectToAction("ForgotPassword");
                    }

                    // Kiểm tra OTP đã hết hạn chưa
                    var expiryTime = DateTime.Parse(sessionExpiry);
                    if (DateTime.Now > expiryTime)
                    {
                        HttpContext.Session.Remove("ResetPasswordOtp");
                        HttpContext.Session.Remove("ResetPasswordOtpExpiry");
                        HttpContext.Session.Remove("ResetPasswordUsername");
                        HttpContext.Session.Remove("ResetPasswordEmail");
                        ModelState.AddModelError("OtpCode", "Mã OTP đã hết hạn. Vui lòng thực hiện lại!");
                        ViewBag.IsResetPassword = true;
                        return View(model);
                    }

                    // Kiểm tra mã OTP
                    if (model.OtpCode != sessionOtp)
                    {
                        ModelState.AddModelError("OtpCode", "Mã OTP không chính xác!");
                        ViewBag.IsResetPassword = true;
                        return View(model);
                    }

                    // OTP đúng, đặt cờ xác thực và chuyển đến trang reset password
                    HttpContext.Session.SetString("ResetPasswordVerified", "true");
                    HttpContext.Session.Remove("ResetPasswordOtp");
                    HttpContext.Session.Remove("ResetPasswordOtpExpiry");

                    // Lưu thông báo success vào Session
                    HttpContext.Session.SetString("SuccessMessage", "Xác thực thành công! Vui lòng đặt mật khẩu mới.");

                    return RedirectToAction("ResetPassword");
                }
                else
                {
                    // Xử lý đăng ký
                    var sessionOtp = HttpContext.Session.GetString("OtpCode");
                    var sessionExpiry = HttpContext.Session.GetString("OtpExpiry");
                    var sessionEmail = HttpContext.Session.GetString("OtpEmail");
                    var registerDataJson = HttpContext.Session.GetString("RegisterData");

                    if (string.IsNullOrEmpty(sessionOtp) || string.IsNullOrEmpty(sessionExpiry) || 
                        string.IsNullOrEmpty(registerDataJson) || string.IsNullOrEmpty(sessionEmail))
                    {
                        return RedirectToAction("Register");
                    }

                    // Kiểm tra email
                    if (model.Email != sessionEmail)
                    {
                        return RedirectToAction("Register");
                    }

                    // Kiểm tra OTP đã hết hạn chưa
                    var expiryTime = DateTime.Parse(sessionExpiry);
                    if (DateTime.Now > expiryTime)
                    {
                        HttpContext.Session.Remove("OtpCode");
                        HttpContext.Session.Remove("OtpExpiry");
                        HttpContext.Session.Remove("RegisterData");
                        HttpContext.Session.Remove("OtpEmail");
                        ModelState.AddModelError("OtpCode", "Mã OTP đã hết hạn. Vui lòng đăng ký lại!");
                        ViewBag.IsResetPassword = false;
                        return View(model);
                    }

                    // Kiểm tra mã OTP
                    if (model.OtpCode != sessionOtp)
                    {
                        ModelState.AddModelError("OtpCode", "Mã OTP không chính xác!");
                        ViewBag.IsResetPassword = false;
                        return View(model);
                    }

                    // OTP đúng, tiến hành đăng ký tài khoản
                    var registerData = System.Text.Json.JsonSerializer.Deserialize<RegisterViewModel>(registerDataJson);
                    
                    if (registerData == null)
                    {
                        return RedirectToAction("Register");
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
                        Username = registerData.Username,
                        FullName = registerData.FullName,
                        Email = registerData.Email,
                        PasswordHash = HashPassword(registerData.Password),
                        Phone = registerData.Phone,
                        Address = registerData.Address,
                        RoleID = customerRole.RoleID,
                        CreatedAt = DateTime.Now
                    };

                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();

                    // Xóa thông tin OTP khỏi Session
                    HttpContext.Session.Remove("OtpCode");
                    HttpContext.Session.Remove("OtpExpiry");
                    HttpContext.Session.Remove("RegisterData");
                    HttpContext.Session.Remove("OtpEmail");

                    // Tặng voucher chào mừng
                    try
                    {
                        await _voucherService.AssignWelcomeVoucherAsync(user.UserID);
                    }
                    catch
                    {
                        // Log nhưng không hiển thị lỗi voucher cho user
                    }

                    // Gửi email chào mừng (không chờ)
                    _ = _emailService.SendWelcomeEmailAsync(user.Email, user.FullName);

                    // Lưu thông báo success vào Session
                    HttpContext.Session.SetString("SuccessMessage", "Đăng ký thành công! Bạn đã nhận được voucher chào mừng WELCOME10. Vui lòng đăng nhập.");

                    return RedirectToAction("Login");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("VerifyOtp error: " + ex.Message);
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi xác thực. Vui lòng thử lại!");
                ViewBag.IsResetPassword = isResetPassword;
                return View(model);
            }
        }

        // POST: Account/ResendOtp - Gửi lại mã OTP
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendOtp()
        {
            try
            {
                var registerDataJson = HttpContext.Session.GetString("RegisterData");
                var sessionEmail = HttpContext.Session.GetString("OtpEmail");

                if (string.IsNullOrEmpty(registerDataJson) || string.IsNullOrEmpty(sessionEmail))
                {
                    return Json(new { success = false, message = "Phiên xác thực đã hết hạn!" });
                }

                var registerData = System.Text.Json.JsonSerializer.Deserialize<RegisterViewModel>(registerDataJson);
                
                if (registerData == null)
                {
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ!" });
                }

                // Tạo mã OTP mới
                var otpCode = EmailService.GenerateOtpCode();
                var otpExpiry = DateTime.Now.AddMinutes(5);

                // Cập nhật Session
                HttpContext.Session.SetString("OtpCode", otpCode);
                HttpContext.Session.SetString("OtpExpiry", otpExpiry.ToString("o"));

                // Gửi email OTP
                var emailSent = await _emailService.SendOtpEmailAsync(registerData.Email, registerData.FullName, otpCode);

                if (!emailSent)
                {
                    return Json(new { success = false, message = "Không thể gửi mã xác thực. Vui lòng thử lại!" });
                }

                return Json(new { success = true, message = "Mã OTP mới đã được gửi đến email của bạn!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Đã xảy ra lỗi: {ex.Message}" });
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Logout()
        {
            // Xóa session
            HttpContext.Session.Clear();
            
            return RedirectToAction("Index", "Home");
        }

        // ==================== FORGOT PASSWORD FLOW ====================

        // GET: Account/ForgotPassword - Trang quên mật khẩu
        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // POST: Account/ForgotPassword - Xử lý quên mật khẩu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                // Kiểm tra username và email có khớp nhau không
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Username == model.Username && u.Email == model.Email);

                if (user == null)
                {
                    ModelState.AddModelError(string.Empty, "Tên đăng nhập và email không khớp với bất kỳ tài khoản nào!");
                    return View(model);
                }

                // Tạo mã OTP
                var otpCode = EmailService.GenerateOtpCode();
                var otpExpiry = DateTime.Now.AddMinutes(5);

                // Lưu OTP vào Session
                HttpContext.Session.SetString("ResetPasswordOtp", otpCode);
                HttpContext.Session.SetString("ResetPasswordOtpExpiry", otpExpiry.ToString("o"));
                HttpContext.Session.SetString("ResetPasswordUsername", user.Username);
                HttpContext.Session.SetString("ResetPasswordEmail", user.Email);

                // Gửi email OTP
                var emailSent = await _emailService.SendResetPasswordOtpAsync(user.Email, user.Username, otpCode);

                if (!emailSent)
                {
                    ModelState.AddModelError(string.Empty, "Không thể gửi email. Vui lòng thử lại sau!");
                    return View(model);
                }

                // Lưu thông báo success vào Session
                HttpContext.Session.SetString("SuccessMessage", $"Mã OTP đã được gửi đến email {user.Email}. Vui lòng kiểm tra hộp thư!");

                return RedirectToAction("VerifyOtp", new { isResetPassword = true });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Có lỗi xảy ra: {ex.Message}");
                return View(model);
            }
        }

        // GET: Account/ResetPassword - Trang đặt mật khẩu mới
        [HttpGet]
        public IActionResult ResetPassword()
        {
            var verified = HttpContext.Session.GetString("ResetPasswordVerified");
            var username = HttpContext.Session.GetString("ResetPasswordUsername");

            if (string.IsNullOrEmpty(verified) || verified != "true" || string.IsNullOrEmpty(username))
            {
                return RedirectToAction("ForgotPassword");
            }

            var model = new ResetPasswordViewModel
            {
                Username = username
            };

            return View(model);
        }

        // POST: Account/ResetPassword - Xử lý đặt mật khẩu mới
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            try
            {
                var verified = HttpContext.Session.GetString("ResetPasswordVerified");
                var username = HttpContext.Session.GetString("ResetPasswordUsername");

                if (string.IsNullOrEmpty(verified) || verified != "true" || string.IsNullOrEmpty(username))
                {
                    return RedirectToAction("ForgotPassword");
                }

                // Tìm user
                var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == username);

                if (user == null)
                {
                    return RedirectToAction("ForgotPassword");
                }

                // Cập nhật mật khẩu mới
                user.PasswordHash = HashPassword(model.NewPassword);

                await _context.SaveChangesAsync();

                // Xóa session
                HttpContext.Session.Remove("ResetPasswordVerified");
                HttpContext.Session.Remove("ResetPasswordUsername");
                HttpContext.Session.Remove("ResetPasswordEmail");

                // Lưu thông báo success vào Session
                HttpContext.Session.SetString("SuccessMessage", "Đặt lại mật khẩu thành công! Vui lòng đăng nhập với mật khẩu mới.");

                return RedirectToAction("Login");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Có lỗi xảy ra: {ex.Message}");
                return View(model);
            }
        }

        // POST: Account/ResendResetOtp - Gửi lại OTP đặt lại mật khẩu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendResetOtp()
        {
            try
            {
                var username = HttpContext.Session.GetString("ResetPasswordUsername");
                var email = HttpContext.Session.GetString("ResetPasswordEmail");

                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(email))
                {
                    return Json(new { success = false, message = "Phiên làm việc đã hết hạn!" });
                }

                // Tạo OTP mới
                var otpCode = EmailService.GenerateOtpCode();
                var otpExpiry = DateTime.Now.AddMinutes(5);

                // Cập nhật Session
                HttpContext.Session.SetString("ResetPasswordOtp", otpCode);
                HttpContext.Session.SetString("ResetPasswordOtpExpiry", otpExpiry.ToString("o"));

                // Gửi email
                var emailSent = await _emailService.SendResetPasswordOtpAsync(email, username, otpCode);

                if (!emailSent)
                {
                    return Json(new { success = false, message = "Không thể gửi email. Vui lòng thử lại!" });
                }

                return Json(new { success = true, message = "Mã OTP mới đã được gửi đến email của bạn!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        // ==================== END FORGOT PASSWORD FLOW ====================

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
                ModelState.AddModelError("Email", "Email này đã được sử dụng bởi tài khoản khác.");
                return View("Profile", await GetProfileViewModel(userId.Value));
            }

            // Cập nhật thông tin
            user.FullName = model.FullName;
            user.Email = model.Email;
            user.Phone = model.Phone;
            user.Address = model.Address;

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi cập nhật thông tin. Vui lòng thử lại.");
                return View("Profile", await GetProfileViewModel(userId.Value));
            }

            return RedirectToAction("Profile");
        }
        
        private async Task<ProfileViewModel> GetProfileViewModel(int userId)
        {
            var user = await _context.Users.Include(u => u.Role).FirstOrDefaultAsync(u => u.UserID == userId);
            return new ProfileViewModel
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
        }

        // POST: Account/ChangePassword - Đổi mật khẩu
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
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
                ModelState.AddModelError("CurrentPassword", "Mật khẩu hiện tại không đúng.");
                return View("Profile", await GetProfileViewModel(userId.Value));
            }

            // Cập nhật mật khẩu mới với BCrypt
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.NewPassword);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (Exception)
            {
                ModelState.AddModelError(string.Empty, "Có lỗi xảy ra khi đổi mật khẩu. Vui lòng thử lại.");
                return View("Profile", await GetProfileViewModel(userId.Value));
            }

            HttpContext.Session.SetString("SuccessMessage", "Đổi mật khẩu thành công!");
            return RedirectToAction("Profile");
        }

        // ==================== GOOGLE LOGIN ====================

        // GET: Account/GoogleLogin
        [HttpGet]
        public IActionResult GoogleLogin(string? returnUrl = null)
        {
            var redirectUrl = Url.Action("GoogleResponse", "Account", new { ReturnUrl = returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        // GET: Account/GoogleResponse - Callback từ Google
        [HttpGet]
        public async Task<IActionResult> GoogleResponse(string? returnUrl = null)
        {
            try
            {
                var authenticateResult = await HttpContext.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

                if (!authenticateResult.Succeeded)
                {
                    ModelState.AddModelError(string.Empty, "Đăng nhập Google thất bại.");
                    return RedirectToAction("Login");
                }

                var claims = authenticateResult.Principal.Claims;
                var email = claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value;
                var name = claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value;
                var googleId = claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(email))
                {
                    ModelState.AddModelError(string.Empty, "Không thể lấy thông tin email từ Google.");
                    return RedirectToAction("Login");
                }

                // Kiểm tra user đã tồn tại chưa
                var existingUser = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => u.Email == email);

                if (existingUser != null)
                {
                    // User đã tồn tại - Đăng nhập luôn
                    HttpContext.Session.SetInt32("UserId", existingUser.UserID);
                    HttpContext.Session.SetString("Username", existingUser.Username);
                    HttpContext.Session.SetString("FullName", existingUser.FullName);
                    HttpContext.Session.SetString("RoleName", existingUser.Role.RoleName);

                    HttpContext.Session.SetString("SuccessMessage", $"Chào mừng {existingUser.FullName}! Đăng nhập thành công qua Google.");

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Home");
                }
                else
                {
                    // User chưa tồn tại - Tạo tài khoản mới
                    var customerRole = await _context.Roles.FirstOrDefaultAsync(r => r.RoleName == "Customer");
                    if (customerRole == null)
                    {
                        ModelState.AddModelError(string.Empty, "Không tìm thấy vai trò Customer.");
                        return RedirectToAction("Login");
                    }

                    // Tạo username từ email (lấy phần trước @)
                    var username = email.Split('@')[0];
                    var baseUsername = username;
                    var counter = 1;

                    // Đảm bảo username là duy nhất
                    while (await _context.Users.AnyAsync(u => u.Username == username))
                    {
                        username = $"{baseUsername}{counter}";
                        counter++;
                    }

                    var newUser = new User
                    {
                        Username = username,
                        Email = email,
                        FullName = name ?? email.Split('@')[0],
                        Phone = "",
                        Address = "",
                        PasswordHash = BCrypt.Net.BCrypt.HashPassword(Guid.NewGuid().ToString()), // Random password
                        RoleID = customerRole.RoleID,
                        CreatedAt = DateTime.Now
                    };

                    _context.Users.Add(newUser);
                    await _context.SaveChangesAsync();

                    // Tặng voucher chào mừng
                    try
                    {
                        await _voucherService.AssignWelcomeVoucherAsync(newUser.UserID);
                    }
                    catch (Exception)
                    {
                        // Log error nhưng không fail transaction
                    }

                    // Gửi email chào mừng
                    try
                    {
                        await _emailService.SendWelcomeEmailAsync(newUser.Email, newUser.FullName);
                    }
                    catch (Exception)
                    {
                        // Log error nhưng không fail transaction
                    }

                    // Đăng nhập luôn
                    HttpContext.Session.SetInt32("UserId", newUser.UserID);
                    HttpContext.Session.SetString("Username", newUser.Username);
                    HttpContext.Session.SetString("FullName", newUser.FullName);
                    HttpContext.Session.SetString("RoleName", customerRole.RoleName);

                    HttpContext.Session.SetString("SuccessMessage", $"Chào mừng {newUser.FullName}! Tài khoản của bạn đã được tạo thành công. Bạn đã nhận được voucher chào mừng WELCOME10 giảm giá 10% các mặt hàng");

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Home");
                }
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, $"Có lỗi xảy ra: {ex.Message}");
                return RedirectToAction("Login");
            }
        }

        // ==================== END GOOGLE LOGIN ====================
    }
}