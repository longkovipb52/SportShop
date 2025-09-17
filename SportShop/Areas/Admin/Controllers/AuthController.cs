using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Models;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using BCrypt.Net;
using System.ComponentModel.DataAnnotations;

namespace SportShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class AuthController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AuthController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            // If already logged in as admin, redirect to dashboard
            if (User.Identity?.IsAuthenticated == true && User.IsInRole("Admin"))
            {
                return RedirectToAction("Index", "Dashboard");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(AdminLoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (ModelState.IsValid)
            {
                // Find user by username or email
                var user = await _context.Users
                    .Include(u => u.Role)
                    .FirstOrDefaultAsync(u => 
                        (u.Username == model.Username || u.Email == model.Username) 
                        && u.RoleID == 1); // Only admin users

                if (user != null && BCrypt.Net.BCrypt.Verify(model.Password, user.PasswordHash))
                {
                    // Create claims for authentication
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                        new Claim(ClaimTypes.Name, user.Username),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.GivenName, user.FullName),
                        new Claim(ClaimTypes.Role, "Admin")
                    };

                    var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                    var authProperties = new AuthenticationProperties
                    {
                        IsPersistent = model.RememberMe,
                        ExpiresUtc = model.RememberMe ? DateTimeOffset.UtcNow.AddDays(30) : DateTimeOffset.UtcNow.AddHours(2)
                    };

                    await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                        new ClaimsPrincipal(claimsIdentity), authProperties);

                    // Redirect to return URL or dashboard
                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    
                    return RedirectToAction("Index", "Dashboard");
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Tên đăng nhập/email hoặc mật khẩu không đúng.");
                }
            }

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        public IActionResult AccessDenied()
        {
            return View();
        }
    }

    // ViewModel for admin login
    public class AdminLoginViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập hoặc email.")]
        [Display(Name = "Tên đăng nhập hoặc Email")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [DataType(DataType.Password)]
        [Display(Name = "Mật khẩu")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool RememberMe { get; set; }
    }
}
