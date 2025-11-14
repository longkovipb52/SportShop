using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Models;
using SportShop.Services;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Text.Json;

namespace SportShop.Controllers
{
    [Route("[controller]")]
    public class WishlistController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly InteractionTrackingService _trackingService;

        public WishlistController(ApplicationDbContext context, InteractionTrackingService trackingService)
        {
            _context = context;
            _trackingService = trackingService;
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
        public async Task<IActionResult> Toggle()
        {
            try
            {
                Console.WriteLine($"=== Wishlist Toggle Debug ===");
                Console.WriteLine($"Content-Type: {Request.ContentType}");
                Console.WriteLine($"Has Form Content: {Request.HasFormContentType}");
                
                int productId = 0;
                
                // First try to get from form data (trang sản phẩm)
                if (Request.HasFormContentType && Request.Form.ContainsKey("productId"))
                {
                    if (int.TryParse(Request.Form["productId"], out productId))
                    {
                        Console.WriteLine($"ProductId from form: {productId}");
                    }
                }
                // Try query string as fallback
                else if (Request.Query.ContainsKey("productId"))
                {
                    if (int.TryParse(Request.Query["productId"], out productId))
                    {
                        Console.WriteLine($"ProductId from query: {productId}");
                    }
                }
                // Then try to get from JSON body (trang chủ)
                else if (Request.ContentType?.Contains("application/json") == true)
                {
                    try
                    {
                        using var reader = new StreamReader(Request.Body);
                        var body = await reader.ReadToEndAsync();
                        Console.WriteLine($"Request body: {body}");
                        
                        if (!string.IsNullOrEmpty(body))
                        {
                            var jsonDoc = System.Text.Json.JsonDocument.Parse(body);
                            if (jsonDoc.RootElement.TryGetProperty("productId", out var productIdElement))
                            {
                                productId = productIdElement.GetInt32();
                                Console.WriteLine($"ProductId from JSON: {productId}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error parsing JSON: {ex.Message}");
                    }
                }
                
                if (productId <= 0)
                {
                    Console.WriteLine("Invalid productId");
                    return Json(new { success = false, message = "Dữ liệu không hợp lệ." });
                }
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
                    // Sử dụng ExecuteSqlRaw để tránh concurrency exception
                    try
                    {
                        var affectedRows = await _context.Database.ExecuteSqlRawAsync(
                            "DELETE FROM Wishlist WHERE UserID = {0} AND ProductID = {1}", 
                            userId.Value, productId);
                            
                        Console.WriteLine($"Deleted {affectedRows} wishlist rows for Product {productId}, User {userId.Value}");
                    }
                    catch (Exception dbEx)
                    {
                        Console.WriteLine($"Database error during wishlist deletion: {dbEx.Message}");
                        // Tiếp tục coi như thành công vì có thể đã bị xóa
                    }

                    // Track remove from wishlist event - await để đảm bảo hoàn thành
                    try
                    {
                        await _trackingService.TrackRemoveFromWishlistAsync(productId);
                    }
                    catch (Exception trackEx)
                    {
                        // Log lỗi tracking nhưng không làm gián đoạn flow chính
                        Console.WriteLine($"Tracking error: {trackEx.Message}");
                    }

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
                    // Kiểm tra lại lần nữa để tránh race condition
                    var doubleCheckWishlistItem = await _context.Wishlists
                        .FirstOrDefaultAsync(w => w.UserID == userId.Value && w.ProductID == productId);
                        
                    if (doubleCheckWishlistItem != null)
                    {
                        // Đã được thêm bởi request khác
                        return Json(new { 
                            success = true, 
                            message = "Sản phẩm đã có trong danh sách yêu thích.",
                            action = "already_exists",
                            inWishlist = true
                        });
                    }

                    var wishlistItem = new Wishlist
                    {
                        UserID = userId.Value,
                        ProductID = productId,
                        CreatedAt = DateTime.Now
                    };

                    _context.Wishlists.Add(wishlistItem);
                    
                    // Reload product để có state mới nhất trước khi update
                    await _context.Entry(product).ReloadAsync();
                    product.TotalLikes++;
                    _context.Products.Update(product);
                    
                    try
                    {
                        await _context.SaveChangesAsync();
                        Console.WriteLine($"Successfully added wishlist item for Product {productId}, User {userId.Value}");
                    }
                    catch (DbUpdateException ex)
                    {
                        Console.WriteLine($"Error adding to wishlist: {ex.Message}");
                        // Có thể do unique constraint violation (đã có wishlist item)
                        return Json(new { 
                            success = true, 
                            message = "Sản phẩm đã có trong danh sách yêu thích.",
                            action = "already_exists",
                            inWishlist = true
                        });
                    }

                    // Track add to wishlist event - await để đảm bảo hoàn thành
                    try
                    {
                        await _trackingService.TrackAddToWishlistAsync(productId);
                    }
                    catch (Exception trackEx)
                    {
                        // Log lỗi tracking nhưng không làm gián đoạn flow chính
                        Console.WriteLine($"Tracking error: {trackEx.Message}");
                    }

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
                Console.WriteLine($"Exception StackTrace: {ex.StackTrace}");
                Console.WriteLine($"Exception Details: {ex}");
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

                // Kiểm tra xem sản phẩm có trong wishlist không
                var exists = await _context.Wishlists
                    .AnyAsync(w => w.UserID == userId.Value && w.ProductID == productId);

                if (!exists)
                {
                    return Json(new { success = false, message = "Sản phẩm không có trong danh sách yêu thích." });
                }

                // Sử dụng ExecuteSqlRaw để tránh concurrency exception
                try
                {
                    var affectedRows = await _context.Database.ExecuteSqlRawAsync(
                        "DELETE FROM Wishlist WHERE UserID = {0} AND ProductID = {1}", 
                        userId.Value, productId);
                        
                    Console.WriteLine($"Deleted {affectedRows} wishlist rows for Product {productId}, User {userId.Value}");
                }
                catch (Exception dbEx)
                {
                    Console.WriteLine($"Database error during wishlist deletion: {dbEx.Message}");
                    // Tiếp tục coi như thành công vì có thể đã bị xóa
                }

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
                    return Json(new { success = true, inWishlist = false });
                }

                var exists = await _context.Wishlists
                    .AnyAsync(w => w.UserID == userId.Value && w.ProductID == productId);

                return Json(new { success = true, inWishlist = exists });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Wishlist Check: {ex.Message}");
                return Json(new { success = false, inWishlist = false });
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
                    return Json(new { success = true, count = 0 });
                }

                var count = await _context.Wishlists
                    .CountAsync(w => w.UserID == userId.Value);

                return Json(new { success = true, count = count });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in Wishlist Count: {ex.Message}");
                return Json(new { success = false, count = 0 });
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

        // POST: Wishlist/CheckMultiple - Batch check wishlist status for multiple products
        [HttpPost]
        [Route("CheckMultiple")]
        public async Task<IActionResult> CheckMultiple([FromBody] CheckMultipleRequest request)
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                
                if (!userId.HasValue)
                {
                    return Json(new { success = true, wishlistStatus = new {} });
                }

                if (request?.ProductIds == null || !request.ProductIds.Any())
                {
                    return Json(new { success = true, wishlistStatus = new {} });
                }

                // Get all wishlist items for this user and the requested products
                var wishlistItems = await _context.Wishlists
                    .Where(w => w.UserID == userId.Value && request.ProductIds.Contains(w.ProductID))
                    .Select(w => w.ProductID)
                    .ToListAsync();

                // Create response dictionary
                var wishlistStatus = request.ProductIds.ToDictionary(
                    productId => productId.ToString(),
                    productId => wishlistItems.Contains(productId)
                );

                return Json(new { 
                    success = true, 
                    wishlistStatus = wishlistStatus 
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in CheckMultiple: {ex.Message}");
                return Json(new { success = false, message = "Có lỗi xảy ra khi kiểm tra danh sách yêu thích." });
            }
        }
    }

    // Helper class for CheckMultiple request
    public class CheckMultipleRequest
    {
        public int[] ProductIds { get; set; } = new int[0];
    }
}
