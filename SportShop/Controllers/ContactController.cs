using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Models;
using SportShop.Models.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SportShop.Controllers
{
    public class ContactController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ContactController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            ViewData["CustomCSS"] = "contact.css";
            
            // Tạo ViewModel với thông tin mặc định
            var model = new ContactFormViewModel
            {
                Name = "",
                Email = "",
                Title = "",
                Phone = "",
                Message = ""
            };
            
            // Nếu người dùng đã đăng nhập, điền sẵn thông tin
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                var user = await _context.Users.FindAsync(userId.Value);
                if (user != null)
                {
                    model.Name = user.FullName ?? "";
                    model.Email = user.Email ?? "";
                    model.Phone = user.Phone ?? "";
                }
            }
            
            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> Send(ContactFormViewModel model)
        {
            try
            {
                if (ModelState.IsValid)
                {
                    // Lấy UserID từ Session nếu người dùng đã đăng nhập
                    int? userId = HttpContext.Session.GetInt32("UserId");

                    var contact = new Contact
                    {
                        UserID = userId,
                        Name = model.Name,
                        Email = model.Email,
                        Title = model.Title,
                        Phone = model.Phone,
                        Message = model.Message,
                        Status = "New",
                        CreatedAt = DateTime.Now
                    };

                    _context.Add(contact);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi sớm nhất có thể.";
                    return RedirectToAction("Index");
                }
                else
                {
                    // Nếu có lỗi validation, hiển thị lỗi
                    var errors = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage));
                    TempData["ErrorMessage"] = $"Vui lòng kiểm tra lại thông tin: {errors}";
                    
                    // Trả về view với model để giữ lại dữ liệu đã nhập
                    ViewData["CustomCSS"] = "contact.css";
                    return View(model);
                }
            }
            catch (Exception)
            {
                TempData["ErrorMessage"] = "Có lỗi xảy ra khi gửi tin nhắn. Vui lòng thử lại sau.";
                return RedirectToAction("Index");
            }
        }
    }
}