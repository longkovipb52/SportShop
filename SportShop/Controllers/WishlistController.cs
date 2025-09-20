using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Models;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SportShop.Controllers
{
    [Route("[controller]")]
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;

        public WishlistController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Wishlist - Hiển thị danh sách sản phẩm yêu thích
        public async Task<IActionResult> Index()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            
            if (!userId.HasValue)
            {
                // Nếu chưa đăng nhập, chuyển hướng đến trang đăng nhập
                TempData["ErrorMessage"] = "Vui lòng đăng nhập để xem danh sách yêu thích.";
                return RedirectToAction("Login", "Account");
            }

            // Lấy danh sách sản phẩm yêu thích của user
            var wishlistItems = await _context.Wishlists
                .Include(w => w.Product)
                    .ThenInclude(p => p.Category)
                .Include(w => w.Product)
                    .ThenInclude(p => p.Brand)
                .Include(w => w.Product)
                    .ThenInclude(p => p.Reviews)
                .Where(w => w.UserID == userId.Value)
                .OrderByDescending(w => w.CreatedAt)
                .ToListAsync();

            return View(wishlistItems);
        }

        // POST: Wishlist/Toggle - Thêm/xóa sản phẩm khỏi danh sách yêu thích
        [HttpPost("Toggle")]
        public async Task<IActionResult> Toggle(int productId)
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                
                if (!userId.HasValue)
                {
                    return Json(new { 
                        success = false, 
                        message = "Vui lòng đăng nhập để thêm sản phẩm vào danh sách yêu thích.",
                        requireLogin = true
                    });
                }

                // Kiểm tra sản phẩm có tồn tại không
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." });
                }

                // Kiểm tra xem sản phẩm đã có trong wishlist chưa
                var existingWishlistItem = await _context.Wishlists
                    .FirstOrDefaultAsync(w => w.UserID == userId.Value && w.ProductID == productId);

                if (existingWishlistItem != null)
                {
                    // Đã có trong wishlist, xóa khỏi wishlist
                    // Lưu ý: Không giảm TotalLikes khi xóa khỏi wishlist
                    _context.Wishlists.Remove(existingWishlistItem);
                    await _context.SaveChangesAsync();

                    return Json(new { 
                        success = true, 
                        message = "Đã xóa sản phẩm khỏi danh sách yêu thích.",
                        action = "removed",
                        inWishlist = false
                    });
                }
                else
                {
                    // Chưa có trong wishlist, thêm vào wishlist và tăng TotalLikes
                    var wishlistItem = new Wishlist
                    {
                        UserID = userId.Value,
                        ProductID = productId,
                        CreatedAt = DateTime.Now
                    };

                    _context.Wishlists.Add(wishlistItem);
                    
                    // Tăng TotalLikes cho sản phẩm
                    product.TotalLikes++;
                    _context.Products.Update(product);
                    
                    await _context.SaveChangesAsync();

                    return Json(new { 
                        success = true, 
                        message = "Đã thêm sản phẩm vào danh sách yêu thích.",
                        action = "added",
                        inWishlist = true
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Wishlist Toggle: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại." });
            }
        }

        // POST: Wishlist/Remove - Xóa sản phẩm khỏi danh sách yêu thích
        [HttpPost("Remove")]
        public async Task<IActionResult> Remove(int productId)
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                
                if (!userId.HasValue)
                {
                    return Json(new { 
                        success = false, 
                        message = "Vui lòng đăng nhập để thực hiện thao tác này.",
                        requireLogin = true
                    });
                }

                var wishlistItem = await _context.Wishlists
                    .FirstOrDefaultAsync(w => w.UserID == userId.Value && w.ProductID == productId);

                if (wishlistItem == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không có trong danh sách yêu thích." });
                }

                _context.Wishlists.Remove(wishlistItem);
                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = "Đã xóa sản phẩm khỏi danh sách yêu thích." 
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Wishlist Remove: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại." });
            }
        }

        // GET: Wishlist/Check - Kiểm tra sản phẩm có trong wishlist không
        [HttpGet("Check")]
        public async Task<IActionResult> Check(int productId)
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                
                if (!userId.HasValue)
                {
                    return Json(new { inWishlist = false });
                }

                var exists = await _context.Wishlists
                    .AnyAsync(w => w.UserID == userId.Value && w.ProductID == productId);

                return Json(new { inWishlist = exists });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Wishlist Check: {ex.Message}");
                return Json(new { inWishlist = false });
            }
        }

        // GET: Wishlist/Count - Lấy số lượng sản phẩm trong wishlist
        [HttpGet("Count")]
        public async Task<IActionResult> Count()
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                
                if (!userId.HasValue)
                {
                    return Json(new { count = 0 });
                }

                var count = await _context.Wishlists
                    .CountAsync(w => w.UserID == userId.Value);

                return Json(new { count = count });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Wishlist Count: {ex.Message}");
                return Json(new { count = 0 });
            }
        }

        // POST: Wishlist/AddToCart - Thêm sản phẩm từ wishlist vào giỏ hàng
        [HttpPost("AddToCart")]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1)
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                
                if (!userId.HasValue)
                {
                    return Json(new { 
                        success = false, 
                        message = "Vui lòng đăng nhập để thực hiện thao tác này.",
                        requireLogin = true
                    });
                }

                // Kiểm tra sản phẩm có tồn tại không
                var product = await _context.Products
                    .Include(p => p.Attributes)
                    .FirstOrDefaultAsync(p => p.ProductID == productId);
                    
                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." });
                }

                // Kiểm tra stock
                if (product.Stock < quantity)
                {
                    return Json(new { success = false, message = "Sản phẩm không đủ số lượng." });
                }

                // Kiểm tra xem sản phẩm đã có trong giỏ hàng chưa
                var existingCartItem = await _context.Carts
                    .FirstOrDefaultAsync(c => c.UserID == userId.Value && c.ProductID == productId && c.AttributeID == null);

                if (existingCartItem != null)
                {
                    // Đã có trong giỏ hàng, tăng số lượng
                    existingCartItem.Quantity += quantity;
                }
                else
                {
                    // Chưa có trong giỏ hàng, thêm mới
                    var cartItem = new Cart
                    {
                        UserID = userId.Value,
                        ProductID = productId,
                        Quantity = quantity,
                        CreatedAt = DateTime.Now
                    };
                    _context.Carts.Add(cartItem);
                }

                await _context.SaveChangesAsync();

                return Json(new { 
                    success = true, 
                    message = "Đã thêm sản phẩm vào giỏ hàng." 
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Wishlist AddToCart: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra. Vui lòng thử lại." });
            }
        }
    }
}
