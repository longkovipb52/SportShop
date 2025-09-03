using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Controllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace SportShop.ViewComponents
{
    public class CartCountViewComponent : ViewComponent
    {
        private readonly ApplicationDbContext _context;

        public CartCountViewComponent(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            int count = await GetCartCountAsync();
            return View(count);
        }

        private async Task<int> GetCartCountAsync()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            int count = 0;

            if (userId.HasValue)
            {
                // Lấy số lượng từ database
                count = await _context.Carts
                    .Where(c => c.UserID == userId.Value)
                    .SumAsync(c => c.Quantity);
            }
            else
            {
                // Lấy số lượng từ session
                var cartJson = HttpContext.Session.GetString("Cart");
                if (!string.IsNullOrEmpty(cartJson))
                {
                    var cartItems = JsonSerializer.Deserialize<List<CartSessionItem>>(cartJson);
                    count = cartItems.Sum(i => i.Quantity);
                }
            }

            return count;
        }
    }
}