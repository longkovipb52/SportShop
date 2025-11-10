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
            // Debug logging
            Console.WriteLine($"HomeController.Index called - Request Path: {Request.Path}");
            Console.WriteLine($"Request Headers: {string.Join(", ", Request.Headers.Select(h => $"{h.Key}: {h.Value}"))}");
            
            // Lấy 8 danh mục nổi bật với thông tin sản phẩm
            var categories = await _context.Categories
                .Include(c => c.Products)
                .Take(8)
                .ToListAsync();

            // Lấy 8 sản phẩm nổi bật
            var products = await _context.Products
                .Include(p => p.Attributes)
                .OrderByDescending(p => p.CreatedAt)
                .Take(8) // Lấy 8 sản phẩm thay vì 4
                .ToListAsync();

            // Lấy top 5 thương hiệu yêu thích nhất (có nhiều sản phẩm nhất)
            var brands = await _context.Brands
                .Include(b => b.Products)
                .OrderByDescending(b => b.Products.Count())
                .Take(5)
                .ToListAsync();

            // Lấy sản phẩm theo từng thương hiệu (5 sản phẩm mỗi thương hiệu)
            var productsByBrand = new Dictionary<Brand, IEnumerable<Product>>();
            foreach (var brand in brands)
            {
                var brandProducts = await _context.Products
                    .Include(p => p.Attributes)
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Where(p => p.BrandID == brand.BrandID)
                    .OrderByDescending(p => p.CreatedAt)
                    .Take(5)
                    .ToListAsync();
                
                productsByBrand[brand] = brandProducts;
            }

            // Tạo HomeViewModel
            var viewModel = new HomeViewModel
            {
                FeaturedCategories = categories,
                FeaturedProducts = products,
                FeaturedBrands = brands,
                ProductsByBrand = productsByBrand
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
