using Microsoft.AspNetCore.Mvc;
using SportShop.Data;
using SportShop.Models;
using System;
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

        [HttpPost]
        public async Task<IActionResult> Send(Contact contactModel, string Subject)
        {
            if (ModelState.IsValid)
            {
                var contact = new Contact
                {
                    Name = contactModel.Name,
                    Email = contactModel.Email,
                    Message = $"Tiêu đề: {Subject}\n\n{contactModel.Message}",
                    Status = "New",
                    CreatedAt = DateTime.Now
                };

                _context.Add(contact);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cảm ơn bạn đã liên hệ! Chúng tôi sẽ phản hồi sớm nhất có thể.";
                return RedirectToAction("Index", "Home");
            }
            
            return RedirectToAction("Index", "Home");
        }
    }
}