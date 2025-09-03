using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Models;
using SportShop.Models.ViewModels;

namespace SportShop.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // Lấy 3 danh mục nổi bật với thông tin sản phẩm
            var categories = await _context.Categories
                .Include(c => c.Products)
                .Take(3)
                .ToListAsync();

            // Lấy 8 sản phẩm nổi bật
            var products = await _context.Products
                .Include(p => p.Attributes)
                .OrderByDescending(p => p.CreatedAt)
                .Take(8) // Lấy 8 sản phẩm thay vì 4
                .ToListAsync();

            // Lấy tất cả thương hiệu
            var brands = await _context.Brands.ToListAsync();

            // Tạo HomeViewModel
            var viewModel = new HomeViewModel
            {
                FeaturedCategories = categories,
                FeaturedProducts = products,
                FeaturedBrands = brands
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
