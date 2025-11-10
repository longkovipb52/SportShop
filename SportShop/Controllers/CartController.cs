using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SportShop.Data;
using SportShop.Models;
using SportShop.Models.ViewModels;
using SportShop.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.ComponentModel.DataAnnotations;

namespace SportShop.Controllers
{
    // Thêm Route attribute ở cấp controller
    [Route("[controller]")]
    public class CartController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PayPalService _payPalService;
        private readonly MoMoService _moMoService;
        private readonly VnPayServiceNew _vnPayService;
        private readonly IConfiguration _configuration;
        private readonly VoucherService _voucherService;
        private readonly InteractionTrackingService _trackingService;

        public CartController(ApplicationDbContext context, PayPalService payPalService, MoMoService moMoService, VnPayServiceNew vnPayService, IConfiguration configuration, VoucherService voucherService, InteractionTrackingService trackingService)
        {
            _context = context;
            _payPalService = payPalService;
            _moMoService = moMoService;
            _vnPayService = vnPayService;
            _configuration = configuration;
            _voucherService = voucherService;
            _trackingService = trackingService;
        }

        // GET: Cart - Hiển thị trang giỏ hàng
        // Thêm Route attribute để đảm bảo trang giỏ hàng hoạt động đúng
        [Route("")]
        [Route("Index")]
        public async Task<IActionResult> Index()
        {
            var cartItems = await GetCartItemsAsync();
            return View(cartItems);
        }

        // POST: Cart/AddToCart - API để thêm sản phẩm vào giỏ hàng
        [HttpPost]
        public async Task<IActionResult> AddToCart(int productId, int quantity = 1, int? attributeId = null)
        {
            try
            {
                Console.WriteLine($"AddToCart - ProductId: {productId}, Quantity: {quantity}, AttributeId: {attributeId}");
                
                if (attributeId.HasValue)
                {
                    Console.WriteLine($"AttributeId is not null: {attributeId.Value}");
                }
                else
                {
                    Console.WriteLine("AttributeId is null");
                }
                
                // Kiểm tra sản phẩm tồn tại
                var product = await _context.Products
                    .Include(p => p.Attributes)
                    .FirstOrDefaultAsync(p => p.ProductID == productId);
                    
                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." });
                }

                // In ra danh sách thuộc tính của sản phẩm để kiểm tra
                if (product.Attributes != null && product.Attributes.Any())
                {
                    foreach (var attr in product.Attributes)
                    {
                        Console.WriteLine($"Available Attribute: ID={attr.AttributeID}, Color={attr.Color}, Size={attr.Size}");
                    }
                }

                // Kiểm tra thuộc tính nếu được chỉ định
                if (attributeId.HasValue)
                {
                    var attribute = product.Attributes?.FirstOrDefault(a => a.AttributeID == attributeId.Value);
                    if (attribute == null)
                    {
                        return Json(new { success = false, message = "Thuộc tính sản phẩm không tồn tại." });
                    }

                    if (attribute.Stock < quantity)
                    {
                        return Json(new { success = false, message = "Sản phẩm không đủ số lượng." });
                    }
                    
                    Console.WriteLine($"Selected Attribute: ID={attribute.AttributeID}, Color={attribute.Color}, Size={attribute.Size}, Stock={attribute.Stock}");
                }
                else
                {
                    // Kiểm tra stock tổng của sản phẩm nếu không chọn thuộc tính
                    if (product.Stock < quantity)
                    {
                        return Json(new { success = false, message = "Sản phẩm không đủ số lượng." });
                    }
                }

                // Kiểm tra xem sản phẩm có thuộc tính không
                var productHasAttributes = product.Attributes != null && product.Attributes.Any();
                Console.WriteLine($"Product has attributes: {productHasAttributes}");

                if (productHasAttributes && !attributeId.HasValue)
                {
                    // Nếu sản phẩm có thuộc tính nhưng không chọn, chọn thuộc tính đầu tiên
                    var firstAttribute = product.Attributes?.FirstOrDefault();
                    if (firstAttribute != null)
                    {
                        attributeId = firstAttribute.AttributeID;
                        Console.WriteLine($"Auto-selected first attribute: {attributeId}");
                    }
                }

                // In ra thông tin attributeId cuối cùng
                Console.WriteLine($"Final attributeId: {attributeId}");

                // Kiểm tra người dùng đăng nhập hay chưa
                int? userId = HttpContext.Session.GetInt32("UserId");

                if (!userId.HasValue)
                {
                    // Người dùng chưa đăng nhập, yêu cầu đăng nhập
                    return Json(new { 
                        success = false, 
                        message = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng.", 
                        requireLogin = true 
                    });
                }

                // Người dùng đã đăng nhập, lưu vào database
                await AddToCartForUserAsync(userId.Value, productId, quantity, attributeId);
                Console.WriteLine($"Added to cart for user {userId.Value} with attributeId: {attributeId}");

                // Lấy số lượng sản phẩm trong giỏ hàng
                int cartCount = await GetCartCountAsync();

                return Json(new { success = true, message = "Đã thêm sản phẩm vào giỏ hàng.", cartCount });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddToCart: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        // POST: Cart/UpdateQuantity - Cập nhật số lượng sản phẩm
        [HttpPost]
        [Route("UpdateQuantity")]
        public async Task<IActionResult> UpdateQuantity([FromForm] int productId, [FromForm] int quantity, [FromForm] int? attributeId = null)
        {
            try
            {
                Console.WriteLine($"UpdateQuantity called: productId={productId}, quantity={quantity}, attributeId={attributeId}");
                
                if (quantity <= 0)
                {
                    return Json(new { success = false, message = "Số lượng phải lớn hơn 0." });
                }

                // Kiểm tra sản phẩm và stock
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." });
                }

                // Kiểm tra stock dựa vào thuộc tính hoặc sản phẩm tổng
                if (attributeId.HasValue)
                {
                    var attribute = await _context.ProductAttributes.FindAsync(attributeId.Value);
                    if (attribute == null || attribute.Stock < quantity)
                    {
                        return Json(new { success = false, message = "Không đủ số lượng sản phẩm." });
                    }
                }
                else if (product.Stock < quantity)
                {
                    return Json(new { success = false, message = "Không đủ số lượng sản phẩm." });
                }

                // Cập nhật số lượng
                int? userId = HttpContext.Session.GetInt32("UserId");
                if (userId.HasValue)
                {
                    // Cập nhật trong database
                    await UpdateCartQuantityForUserAsync(userId.Value, productId, quantity, attributeId);
                }
                else
                {
                    // Cập nhật trong session
                    UpdateCartQuantitySession(productId, quantity, attributeId);
                }

                var cartItems = await GetCartItemsAsync();
                decimal subtotal = cartItems.Sum(item => item.TotalPrice);
                int cartCount = cartItems.Sum(item => item.Quantity);

                return Json(new { 
                    success = true, 
                    message = "Đã cập nhật số lượng.",
                    subtotal = subtotal,
                    formattedSubtotal = string.Format("{0:N0}đ", subtotal),
                    cartCount = cartCount
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateQuantity: {ex}");
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        // POST: Cart/RemoveItem - Xóa sản phẩm khỏi giỏ hàng
        [HttpPost]
        [Route("RemoveItem")]
        public async Task<IActionResult> RemoveItem([FromForm] int productId, [FromForm] int? attributeId = null)
        {
            try
            {
                Console.WriteLine($"RemoveItem called: productId={productId}, attributeId={attributeId}");
                
                // Xóa sản phẩm
                int? userId = HttpContext.Session.GetInt32("UserId");
                if (userId.HasValue)
                {
                    // Xóa từ database
                    await RemoveCartItemForUserAsync(userId.Value, productId, attributeId);
                }
                else
                {
                    // Xóa từ session
                    RemoveCartItemSession(productId, attributeId);
                }

                // Track remove from cart event - await để đảm bảo hoàn thành
                try
                {
                    await _trackingService.TrackRemoveFromCartAsync(productId, attributeId);
                }
                catch (Exception trackEx)
                {
                    // Log lỗi tracking nhưng không làm gián đoạn flow chính
                    Console.WriteLine($"Tracking error: {trackEx.Message}");
                }

                // Đảm bảo lấy lại dữ liệu giỏ hàng mới nhất
                var cartItems = await GetCartItemsAsync();
                decimal subtotal = cartItems.Sum(item => item.TotalPrice);
                int cartCount = cartItems.Sum(item => item.Quantity);

                return Json(new { 
                    success = true, 
                    message = "Đã xóa sản phẩm khỏi giỏ hàng.", 
                    subtotal = subtotal,
                    formattedSubtotal = string.Format("{0:N0}đ", subtotal),
                    cartCount = cartCount
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in RemoveItem: {ex}");
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        // GET: Cart/Count - Lấy số lượng sản phẩm trong giỏ hàng
        [HttpGet]
        public async Task<IActionResult> GetCount()
        {
            int count = await GetCartCountAsync();
            return Json(count);
        }

        // Private Methods

        // Thêm sản phẩm vào giỏ hàng cho user đã đăng nhập
        private async Task AddToCartForUserAsync(int userId, int productId, int quantity, int? attributeId)
        {
            // Kiểm tra xem sản phẩm đã có trong giỏ hàng chưa
            var existingItem = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserID == userId && c.ProductID == productId && c.AttributeID == attributeId);

            if (existingItem != null)
            {
                // Đã có trong giỏ hàng, tăng số lượng
                existingItem.Quantity += quantity;
                _context.Carts.Update(existingItem);
            }
            else
            {
                // Chưa có trong giỏ hàng, thêm mới
                var cartItem = new Cart
                {
                    UserID = userId,
                    ProductID = productId,
                    Quantity = quantity,
                    AttributeID = attributeId,
                    CreatedAt = DateTime.Now
                };
                _context.Carts.Add(cartItem);
            }

            await _context.SaveChangesAsync();
        }

        // Cập nhật số lượng sản phẩm trong giỏ hàng cho user đã đăng nhập
        private async Task UpdateCartQuantityForUserAsync(int userId, int productId, int quantity, int? attributeId)
        {
            var cartItem = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserID == userId && c.ProductID == productId && c.AttributeID == attributeId);

            if (cartItem != null)
            {
                cartItem.Quantity = quantity;
                _context.Carts.Update(cartItem);
                await _context.SaveChangesAsync();
            }
        }

        // Xóa sản phẩm khỏi giỏ hàng cho user đã đăng nhập
        private async Task RemoveCartItemForUserAsync(int userId, int productId, int? attributeId)
        {
            var cartItem = await _context.Carts
                .FirstOrDefaultAsync(c => c.UserID == userId && c.ProductID == productId && c.AttributeID == attributeId);

            if (cartItem != null)
            {
                _context.Carts.Remove(cartItem);
                await _context.SaveChangesAsync();
            }
        }

        // Thêm sản phẩm vào giỏ hàng cho session (chưa đăng nhập)
        private void AddToCartSession(int productId, int quantity, int? attributeId)
        {
            var cartItems = GetCartItemsFromSession();

            // Kiểm tra sản phẩm đã có trong giỏ hàng chưa
            var existingItem = cartItems.FirstOrDefault(
                i => i.ProductId == productId && 
                     ((!i.AttributeId.HasValue && !attributeId.HasValue) || 
                      (i.AttributeId.HasValue && attributeId.HasValue && i.AttributeId.Value == attributeId.Value)));

            if (existingItem != null)
            {
                // Đã có trong giỏ hàng, tăng số lượng
                existingItem.Quantity += quantity;
            }
            else
            {
                // Chưa có trong giỏ hàng, thêm mới
                // Tạo SessionCartId duy nhất
                var nextId = cartItems.Any() ? cartItems.Max(i => i.SessionCartId) + 1 : 1;
                
                cartItems.Add(new CartSessionItem
                {
                    SessionCartId = nextId,
                    ProductId = productId,
                    Quantity = quantity,
                    AttributeId = attributeId
                });
            }

            // Lưu lại vào session
            SaveCartItemsToSession(cartItems);
        }

        // Cập nhật số lượng sản phẩm trong giỏ hàng cho session
        private void UpdateCartQuantitySession(int productId, int quantity, int? attributeId)
        {
            var cartItems = GetCartItemsFromSession();

            var existingItem = cartItems.FirstOrDefault(
                i => i.ProductId == productId && 
                     ((!i.AttributeId.HasValue && !attributeId.HasValue) || 
                      (i.AttributeId.HasValue && attributeId.HasValue && i.AttributeId.Value == attributeId.Value)));

            if (existingItem != null)
            {
                existingItem.Quantity = quantity;
                SaveCartItemsToSession(cartItems);
            }
        }

        // Xóa sản phẩm khỏi giỏ hàng trong session
        private void RemoveCartItemSession(int productId, int? attributeId)
        {
            var cartItems = GetCartItemsFromSession();

            var itemToRemove = cartItems.FirstOrDefault(
                i => i.ProductId == productId && 
                     ((!i.AttributeId.HasValue && !attributeId.HasValue) || 
                      (i.AttributeId.HasValue && attributeId.HasValue && i.AttributeId.Value == attributeId.Value)));

            if (itemToRemove != null)
            {
                cartItems.Remove(itemToRemove);
                SaveCartItemsToSession(cartItems);
            }
        }

        // Đọc giỏ hàng từ session
        private List<CartSessionItem> GetCartItemsFromSession()
        {
            var cartJson = HttpContext.Session.GetString("Cart");
            return string.IsNullOrEmpty(cartJson) 
                ? new List<CartSessionItem>() 
                : JsonSerializer.Deserialize<List<CartSessionItem>>(cartJson) ?? new List<CartSessionItem>();
        }

        // Lưu giỏ hàng vào session
        private void SaveCartItemsToSession(List<CartSessionItem> cartItems)
        {
            var cartJson = JsonSerializer.Serialize(cartItems);
            HttpContext.Session.SetString("Cart", cartJson);
        }

        // Lấy danh sách sản phẩm trong giỏ hàng (từ DB hoặc session)
        private async Task<List<CartViewModel>> GetCartItemsAsync()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            List<CartViewModel> cartItems = new List<CartViewModel>();

            if (userId.HasValue)
            {
                // Lấy từ database
                cartItems = await _context.Carts
                    .Where(c => c.UserID == userId.Value)
                    .Select(c => new CartViewModel
                    {
                        CartId = c.CartID,
                        ProductId = c.ProductID,
                        ProductName = c.Product.Name,
                        ImageUrl = c.Product.ImageURL,
                        Price = c.AttributeID.HasValue 
                            ? (c.Attribute != null ? (c.Attribute.Price ?? c.Product.Price) : c.Product.Price)
                            : c.Product.Price,
                        Quantity = c.Quantity,
                        AttributeId = c.AttributeID,
                        Color = c.AttributeID.HasValue && c.Attribute != null ? c.Attribute.Color ?? "" : "",
                        Size = c.AttributeID.HasValue && c.Attribute != null ? c.Attribute.Size ?? "" : "",
                        TotalPrice = c.AttributeID.HasValue 
                            ? (c.Attribute != null ? (c.Attribute.Price ?? c.Product.Price) : c.Product.Price) * c.Quantity 
                            : c.Product.Price * c.Quantity
                    })
                    .ToListAsync();
            }
            else
            {
                // Lấy từ session
                var sessionItems = GetCartItemsFromSession();
                
                if (sessionItems.Any())
                {
                    // Lấy danh sách sản phẩm từ database
                    var productIds = sessionItems.Select(i => i.ProductId).ToList();
                    var products = await _context.Products
                        .Where(p => productIds.Contains(p.ProductID))
                        .Include(p => p.Attributes)
                        .ToDictionaryAsync(p => p.ProductID);

                    // Lấy danh sách thuộc tính
                    var attributeIds = sessionItems
                        .Where(i => i.AttributeId.HasValue)
                        .Select(i => i.AttributeId!.Value).ToList();
                    
                    var attributes = attributeIds.Any() 
                        ? await _context.ProductAttributes
                            .Where(a => attributeIds.Contains(a.AttributeID))
                            .ToDictionaryAsync(a => a.AttributeID)
                        : new Dictionary<int, ProductAttribute>();

                    // Chuyển đổi thành CartViewModel
                    foreach (var item in sessionItems)
                    {
                        if (products.TryGetValue(item.ProductId, out var product))
                        {
                            ProductAttribute? attribute = null;
                            if (item.AttributeId.HasValue && attributes.TryGetValue(item.AttributeId.Value, out var attr))
                            {
                                attribute = attr;
                            }

                            cartItems.Add(new CartViewModel
                            {
                                CartId = item.SessionCartId, // Sử dụng SessionCartId làm CartId
                                ProductId = item.ProductId,
                                ProductName = product.Name,
                                ImageUrl = product.ImageURL,
                                Price = attribute != null ? (attribute.Price ?? product.Price) : product.Price,
                                Quantity = item.Quantity,
                                AttributeId = item.AttributeId,
                                Color = attribute?.Color ?? "",
                                Size = attribute?.Size ?? "",
                                TotalPrice = attribute != null 
                                    ? (attribute.Price ?? product.Price) * item.Quantity 
                                    : product.Price * item.Quantity
                            });
                        }
                    }
                }
            }

            return cartItems;
        }

        // Lấy số lượng sản phẩm trong giỏ hàng
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
                var cartItems = GetCartItemsFromSession();
                count = cartItems.Sum(i => i.Quantity);
            }

            return count;
        }

        // Public endpoint để lấy số lượng sản phẩm trong giỏ hàng
        [HttpGet("GetCartCount")]
        public async Task<IActionResult> GetCartCount()
        {
            try
            {
                int count = await GetCartCountAsync();
                return Json(new { count = count });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting cart count: {ex.Message}");
                return Json(new { count = 0 });
            }
        }

        // Sửa đổi phương thức hiện tại để đặt tên route cụ thể
        [HttpPost("AddToCartForm")]
        public async Task<IActionResult> AddToCartForm(int productId, int quantity = 1, int? attributeId = null)
        {
            try
            {
                Console.WriteLine($"AddToCartForm - ProductId: {productId}, Quantity: {quantity}, AttributeId: {attributeId}");
                
                if (attributeId.HasValue)
                {
                    Console.WriteLine($"AttributeId is not null: {attributeId.Value}");
                }
                else
                {
                    Console.WriteLine("AttributeId is null");
                }
                
                // Kiểm tra sản phẩm tồn tại
                var product = await _context.Products
                    .Include(p => p.Attributes)
                    .FirstOrDefaultAsync(p => p.ProductID == productId);
                    
                if (product == null)
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." });
                }

                // In ra danh sách thuộc tính của sản phẩm để kiểm tra
                if (product.Attributes != null && product.Attributes.Any())
                {
                    foreach (var attr in product.Attributes)
                    {
                        Console.WriteLine($"Available Attribute: ID={attr.AttributeID}, Color={attr.Color}, Size={attr.Size}");
                    }
                }

                // Kiểm tra thuộc tính nếu được chỉ định
                if (attributeId.HasValue)
                {
                    var attribute = product.Attributes?.FirstOrDefault(a => a.AttributeID == attributeId.Value);
                    if (attribute == null)
                    {
                        return Json(new { success = false, message = "Thuộc tính sản phẩm không tồn tại." });
                    }

                    if (attribute.Stock < quantity)
                    {
                        return Json(new { success = false, message = "Sản phẩm không đủ số lượng." });
                    }
                    
                    Console.WriteLine($"Selected Attribute: ID={attribute.AttributeID}, Color={attribute.Color}, Size={attribute.Size}, Stock={attribute.Stock}");
                }
                else
                {
                    // Kiểm tra stock tổng của sản phẩm nếu không chọn thuộc tính
                    if (product.Stock < quantity)
                    {
                        return Json(new { success = false, message = "Sản phẩm không đủ số lượng." });
                    }
                }

                // Kiểm tra người dùng đăng nhập hay chưa
                int? userId = HttpContext.Session.GetInt32("UserId");

                if (!userId.HasValue)
                {
                    // Người dùng chưa đăng nhập, yêu cầu đăng nhập
                    return Json(new { 
                        success = false, 
                        message = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng.", 
                        requireLogin = true 
                    });
                }

                // Người dùng đã đăng nhập, lưu vào database
                await AddToCartForUserAsync(userId.Value, productId, quantity, attributeId);
                Console.WriteLine($"Added to cart for user {userId.Value} with attributeId: {attributeId}");

                // Lấy số lượng sản phẩm trong giỏ hàng
                int cartCount = await GetCartCountAsync();

                return Json(new { success = true, message = "Đã thêm sản phẩm vào giỏ hàng.", cartCount });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddToCartForm: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        // Đổi tên phương thức này để tránh xung đột
        [HttpPost("AddToCartJson")]
        public async Task<IActionResult> AddToCartJson([FromBody] CartAddRequest request)
        {
            try
            {
                Console.WriteLine($"AddToCartJson called with: ProductId={request.ProductId}, Quantity={request.Quantity}, AttributeId={request.AttributeId}");
                
                // Kiểm tra giá trị
                if (request.ProductId <= 0)
                {
                    Console.WriteLine($"Invalid ProductId: {request.ProductId}");
                    return Json(new { success = false, message = "ProductId không hợp lệ." });
                }
                
                // Kiểm tra sản phẩm tồn tại
                var product = await _context.Products
                    .Include(p => p.Attributes)
                    .FirstOrDefaultAsync(p => p.ProductID == request.ProductId);
                    
                if (product == null)
                {
                    return Json(new { success = false, message = $"Sản phẩm với ID={request.ProductId} không tồn tại." });
                }

                // In ra danh sách thuộc tính của sản phẩm để kiểm tra
                if (product.Attributes != null)
                {
                    foreach (var attr in product.Attributes)
                    {
                        Console.WriteLine($"Available Attribute: ID={attr.AttributeID}, Color={attr.Color}, Size={attr.Size}");
                    }
                }

                // Kiểm tra thuộc tính nếu được chỉ định
                if (request.AttributeId.HasValue)
                {
                    var attribute = product.Attributes?.FirstOrDefault(a => a.AttributeID == request.AttributeId.Value);
                    if (attribute == null)
                    {
                        return Json(new { success = false, message = "Thuộc tính sản phẩm không tồn tại." });
                    }

                    if (attribute.Stock < request.Quantity)
                    {
                        return Json(new { success = false, message = "Sản phẩm không đủ số lượng." });
                    }
                    
                    Console.WriteLine($"Selected Attribute: ID={attribute.AttributeID}, Color={attribute.Color}, Size={attribute.Size}, Stock={attribute.Stock}");
                }
                else
                {
                    // Kiểm tra stock tổng của sản phẩm nếu không chọn thuộc tính
                    if (product.Stock < request.Quantity)
                    {
                        return Json(new { success = false, message = "Sản phẩm không đủ số lượng." });
                    }
                }

                // Kiểm tra người dùng đăng nhập hay chưa
                int? userId = HttpContext.Session.GetInt32("UserId");

                if (!userId.HasValue)
                {
                    // Người dùng chưa đăng nhập, yêu cầu đăng nhập
                    return Json(new { 
                        success = false, 
                        message = "Vui lòng đăng nhập để thêm sản phẩm vào giỏ hàng.", 
                        requireLogin = true 
                    });
                }

                // Người dùng đã đăng nhập, lưu vào database
                await AddToCartForUserAsync(userId.Value, request.ProductId, request.Quantity, request.AttributeId);
                Console.WriteLine($"Added to cart for user {userId.Value} with attributeId: {request.AttributeId}");

                // Track add to cart event - await để đảm bảo hoàn thành
                try
                {
                    await _trackingService.TrackAddToCartAsync(request.ProductId, request.Quantity, request.AttributeId);
                }
                catch (Exception trackEx)
                {
                    // Log lỗi tracking nhưng không làm gián đoạn flow chính
                    Console.WriteLine($"Tracking error: {trackEx.Message}");
                }

                // Lấy số lượng sản phẩm trong giỏ hàng
                int cartCount = await GetCartCountAsync();

                return Json(new { success = true, message = "Đã thêm sản phẩm vào giỏ hàng.", cartCount });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddToCartJson: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        // Định nghĩa class cho request
        public class CartAddRequest
        {
            [Required(ErrorMessage = "Thiếu thông tin sản phẩm")]
            [Range(1, int.MaxValue, ErrorMessage = "ID sản phẩm không hợp lệ")]
            public int ProductId { get; set; }

            [Required(ErrorMessage = "Thiếu số lượng sản phẩm")]
            [Range(1, 100, ErrorMessage = "Số lượng phải từ 1-100")]
            public int Quantity { get; set; }

            public int? AttributeId { get; set; }
        }

        // Thêm phương thức này để đảm bảo tương thích ngược với các request hiện có
        [HttpPost]
        [Route("AddToCart")]
        public async Task<IActionResult> AddToCartLegacy()
        {
            try
            {
                // Xác định xem request là form data hay JSON
                if (Request.HasFormContentType)
                {
                    // Form data
                    int productId = int.Parse(Request.Form["productId"].FirstOrDefault() ?? "0");
                    int quantity = int.Parse(Request.Form["quantity"].FirstOrDefault() ?? "1");
                    
                    int? attributeId = null;
                    if (int.TryParse(Request.Form["attributeId"].FirstOrDefault(), out int parsedAttributeId))
                    {
                        attributeId = parsedAttributeId;
                    }
                    
                    return await AddToCartForm(productId, quantity, attributeId);
                }
                else
                {
                    // JSON data - đọc request body
                    using var reader = new StreamReader(Request.Body);
                    var body = await reader.ReadToEndAsync();
                    var request = JsonSerializer.Deserialize<CartAddRequest>(body);
                    
                    if (request == null)
                    {
                        return Json(new { success = false, message = "Dữ liệu không hợp lệ" });
                    }
                    
                    // Sửa dòng này - truyền từng thuộc tính riêng lẻ thay vì cả đối tượng
                    return await AddToCart(request.ProductId, request.Quantity, request.AttributeId);
                    // Hoặc bạn có thể gọi
                    // return await AddToCartJson(request);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in AddToCartLegacy: {ex.Message}");
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        // Thêm action mới vào CartController
        [HttpPost("UpdateAttribute")]
        public async Task<IActionResult> UpdateAttribute([FromForm] int cartId, [FromForm] int productId, [FromForm] int attributeId)
        {
            try
            {
                Console.WriteLine($"UpdateAttribute called: cartId={cartId}, productId={productId}, attributeId={attributeId}");
                
                // Kiểm tra thuộc tính mới tồn tại
                var attribute = await _context.ProductAttributes
                    .Include(a => a.Product)
                    .FirstOrDefaultAsync(a => a.AttributeID == attributeId && a.ProductID == productId);
                
                if (attribute == null)
                {
                    Console.WriteLine($"Attribute not found: attributeId={attributeId}, productId={productId}");
                    
                    // Kiểm tra xem thuộc tính có tồn tại không
                    var attributeExists = await _context.ProductAttributes
                        .AnyAsync(a => a.AttributeID == attributeId);
                    
                    // Kiểm tra sản phẩm có tồn tại không
                    var productExists = await _context.Products
                        .AnyAsync(p => p.ProductID == productId);
                    
                    Console.WriteLine($"Attribute exists: {attributeExists}, Product exists: {productExists}");
                    
                    // Kiểm tra thuộc tính có thuộc về sản phẩm không
                    var attributeForProduct = await _context.ProductAttributes
                        .Where(a => a.ProductID == productId)
                        .Select(a => a.AttributeID)
                        .ToListAsync();
                        
                    Console.WriteLine($"Attributes for product {productId}: {string.Join(", ", attributeForProduct)}");
                    
                    return Json(new { success = false, message = "Thuộc tính không hợp lệ" });
                }

                // Kiểm tra số lượng tồn kho
                if (attribute.Stock <= 0)
                {
                    return Json(new { success = false, message = "Thuộc tính này đã hết hàng" });
                }

                int? userId = HttpContext.Session.GetInt32("UserId");
                if (userId.HasValue)
                {
                    // Người dùng đã đăng nhập, cập nhật trong database
                    var cartItem = await _context.Carts
                        .FirstOrDefaultAsync(c => c.CartID == cartId && c.UserID == userId.Value);

                    if (cartItem == null)
                    {
                        return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
                    }

                    // Kiểm tra xem đã có sản phẩm với thuộc tính mới trong giỏ hàng chưa
                    var existingItem = await _context.Carts
                        .FirstOrDefaultAsync(c => c.UserID == userId.Value && 
                                        c.ProductID == productId && 
                                        c.AttributeID == attributeId && 
                                        c.CartID != cartId);

                    if (existingItem != null)
                    {
                        // Đã có sản phẩm với thuộc tính mới, tăng số lượng và xóa mục hiện tại
                        existingItem.Quantity += cartItem.Quantity;
                        _context.Carts.Remove(cartItem);
                    }
                    else
                    {
                        // Cập nhật thuộc tính
                        cartItem.AttributeID = attributeId;
                    }

                    await _context.SaveChangesAsync();
                }
                else
                {
                    // Người dùng chưa đăng nhập, cập nhật trong session
                    var cartJson = HttpContext.Session.GetString("Cart");
                    var cart = string.IsNullOrEmpty(cartJson) 
                        ? new List<CartSessionItem>() 
                        : JsonSerializer.Deserialize<List<CartSessionItem>>(cartJson) ?? new List<CartSessionItem>();
                    
                    // Tìm sản phẩm cần cập nhật bằng cách sử dụng cartId
                    var itemToUpdate = cart?.FirstOrDefault(i => i.SessionCartId == cartId);
                    if (itemToUpdate == null)
                    {
                        // Fallback: tìm bằng productId nếu không tìm thấy bằng cartId
                        itemToUpdate = cart?.FirstOrDefault(i => i.ProductId == productId);
                        if (itemToUpdate == null)
                        {
                            return Json(new { success = false, message = "Không tìm thấy sản phẩm trong giỏ hàng" });
                        }
                    }

                    // Kiểm tra xem đã có sản phẩm với thuộc tính mới trong giỏ hàng chưa
                    var existingItem = cart?.FirstOrDefault(i => 
                        i.ProductId == productId && 
                        i.AttributeId == attributeId && 
                        i.SessionCartId != itemToUpdate.SessionCartId);

                    if (existingItem != null && cart != null)
                    {
                        // Đã có sản phẩm với thuộc tính mới, gộp số lượng và xóa mục hiện tại
                        existingItem.Quantity += itemToUpdate.Quantity;
                        cart.Remove(itemToUpdate);
                        
                        Console.WriteLine($"Merged session cart items: existing quantity = {existingItem.Quantity - itemToUpdate.Quantity}, added quantity = {itemToUpdate.Quantity}, new total = {existingItem.Quantity}");
                    }
                    else
                    {
                        // Cập nhật thuộc tính cho sản phẩm hiện tại
                        itemToUpdate.AttributeId = attributeId;
                        Console.WriteLine($"Updated session cart item attribute: productId={productId}, new attributeId={attributeId}");
                    }

                    HttpContext.Session.SetString("Cart", JsonSerializer.Serialize(cart));
                }

                return Json(new { 
                    success = true, 
                    message = "Đã cập nhật thuộc tính sản phẩm", 
                    color = attribute.Color,
                    size = attribute.Size,
                    price = attribute.Price ?? attribute.Product.Price,
                    formattedPrice = string.Format("{0:N0}đ", attribute.Price ?? attribute.Product.Price)
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in UpdateAttribute: {ex}");
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }

        // Thêm API endpoint để lấy thuộc tính sản phẩm
        [HttpGet("GetProductAttributes")]
        public async Task<IActionResult> GetProductAttributes(int productId)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Attributes)
                    .FirstOrDefaultAsync(p => p.ProductID == productId);

                if (product == null)
                {
                    return Json(new { success = false, message = "Không tìm thấy sản phẩm" });
                }

                var attributes = product.Attributes.Select(a => new
                {
                    id = a.AttributeID,
                    color = a.Color,
                    size = a.Size,
                    stock = a.Stock,
                    price = a.Price ?? product.Price,
                    formattedPrice = string.Format("{0:N0}đ", a.Price ?? product.Price),
                    imageUrl = !string.IsNullOrEmpty(a.ImageURL) ? $"/upload/product/{a.ImageURL}" : $"/upload/product/{product.ImageURL}"
                }).ToList();

                return Json(new { success = true, attributes, productName = product.Name });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Lỗi: {ex.Message}" });
            }
        }
        // Chuyển đến trang thanh toán
        [Route("Checkout")]
        public async Task<IActionResult> Checkout()
        {
            var cartItems = await GetCartItemsAsync();
            
            if (!cartItems.Any())
            {
                TempData["Error"] = "Giỏ hàng của bạn đang trống";
                return RedirectToAction("Index");
            }

            var model = new CheckoutViewModel
            {
                CartItems = cartItems.Select(c => new Models.ViewModels.CartViewModel
                {
                    CartId = c.CartId,
                    ProductId = c.ProductId,
                    ProductName = c.ProductName ?? "",
                    ImageUrl = c.ImageUrl ?? "",
                    Price = c.Price,
                    Quantity = c.Quantity,
                    AttributeId = c.AttributeId,
                    Color = c.Color ?? "",
                    Size = c.Size ?? "",
                    TotalPrice = c.TotalPrice
                }).ToList(),
                Subtotal = cartItems.Sum(item => item.TotalPrice),
                ShippingFee = 0, // Loại bỏ phí vận chuyển theo yêu cầu
                Tax = 0 // Có thể thêm thuế nếu cần
            };

            // Get user ID for voucher processing
            int? userId = HttpContext.Session.GetInt32("UserId");

            // Apply voucher from session if exists
            var selectedUserVoucherID = HttpContext.Session.GetInt32("SelectedUserVoucherID");
            if (selectedUserVoucherID.HasValue && userId.HasValue)
            {
                var validation = await _voucherService.ValidateAndCalculateByUserVoucherAsync(selectedUserVoucherID.Value, userId.Value, model.Subtotal);
                if (validation.Success && validation.UserVoucher != null)
                {
                    model.SelectedUserVoucherID = selectedUserVoucherID.Value;
                    model.VoucherCode = validation.Voucher?.Code;
                    model.DiscountAmount = validation.DiscountAmount;
                }
                else
                {
                    // remove invalid voucher from session
                    HttpContext.Session.Remove("SelectedUserVoucherID");
                }
            }

            model.Total = model.Subtotal + model.ShippingFee + model.Tax - model.DiscountAmount;

            // Nếu người dùng đã đăng nhập, lấy thông tin mặc định và voucher
            if (userId.HasValue)
            {
                var user = await _context.Users.FindAsync(userId.Value);
                if (user != null)
                {
                    model.ShippingName = user.FullName ?? "";
                    model.ShippingPhone = user.Phone ?? "";
                    model.ShippingAddress = user.Address ?? "";
                    model.Email = user.Email ?? "";
                }

                // Load available vouchers for user
                model.AvailableVouchers = await _voucherService.GetAvailableVouchersForUserAsync(userId.Value);
            }

            return View(model);
        }

        [HttpPost]
        [Route("ApplyUserVoucher")]
        public async Task<IActionResult> ApplyUserVoucher([FromForm] int userVoucherID)
        {
            try
            {
                int? userId = HttpContext.Session.GetInt32("UserId");
                if (!userId.HasValue)
                {
                    return Json(new { success = false, message = "Vui lòng đăng nhập để sử dụng voucher." });
                }

                var cartItems = await GetCartItemsAsync();
                var subtotal = cartItems.Sum(i => i.TotalPrice);
                var validation = await _voucherService.ValidateAndCalculateByUserVoucherAsync(userVoucherID, userId.Value, subtotal);

                if (!validation.Success)
                {
                    return Json(new { success = false, message = validation.Message });
                }

                // Store UserVoucherID in session
                HttpContext.Session.SetInt32("SelectedUserVoucherID", userVoucherID);

                var total = subtotal - validation.DiscountAmount;

                return Json(new
                {
                    success = true,
                    message = validation.Message,
                    discount = validation.DiscountAmount,
                    formattedDiscount = string.Format("{0:N0}đ", validation.DiscountAmount),
                    subtotal,
                    formattedSubtotal = string.Format("{0:N0}đ", subtotal),
                    shippingFee = 0,
                    formattedShipping = "Miễn phí",
                    total,
                    formattedTotal = string.Format("{0:N0}đ", total),
                    voucherCode = validation.Voucher?.Code
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        [HttpPost]
        [Route("RemoveUserVoucher")]
        public async Task<IActionResult> RemoveUserVoucher()
        {
            HttpContext.Session.Remove("SelectedUserVoucherID");
            var cartItems = await GetCartItemsAsync();
            var subtotal = cartItems.Sum(i => i.TotalPrice);
            var total = subtotal;
            return Json(new
            {
                success = true,
                message = "Đã bỏ áp dụng voucher.",
                subtotal,
                formattedSubtotal = string.Format("{0:N0}đ", subtotal),
                shippingFee = 0,
                formattedShipping = "Miễn phí",
                total,
                formattedTotal = string.Format("{0:N0}đ", total)
            });
        }

        [HttpPost]
        [Route("ApplyVoucher")]
        public async Task<IActionResult> ApplyVoucher([FromForm] string code)
        {
            try
            {
                var cartItems = await GetCartItemsAsync();
                var subtotal = cartItems.Sum(i => i.TotalPrice);
                var validation = await _voucherService.ValidateAndCalculateAsync(code, subtotal);

                if (!validation.Success)
                {
                    return Json(new { success = false, message = validation.Message });
                }

                // Store code in session
                HttpContext.Session.SetString("VoucherCode", validation.Voucher!.Code);

                var total = subtotal - validation.DiscountAmount;

                return Json(new
                {
                    success = true,
                    message = validation.Message,
                    discount = validation.DiscountAmount,
                    formattedDiscount = string.Format("{0:N0}đ", validation.DiscountAmount),
                    subtotal,
                    formattedSubtotal = string.Format("{0:N0}đ", subtotal),
                    shippingFee = 0,
                    formattedShipping = "Miễn phí",
                    total,
                    formattedTotal = string.Format("{0:N0}đ", total)
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"Có lỗi xảy ra: {ex.Message}" });
            }
        }

        [HttpPost]
        [Route("RemoveVoucher")]
        public async Task<IActionResult> RemoveVoucher()
        {
            HttpContext.Session.Remove("VoucherCode");
            var cartItems = await GetCartItemsAsync();
            var subtotal = cartItems.Sum(i => i.TotalPrice);
            var total = subtotal;
            return Json(new
            {
                success = true,
                message = "Đã bỏ áp dụng voucher.",
                subtotal,
                formattedSubtotal = string.Format("{0:N0}đ", subtotal),
                shippingFee = 0,
                formattedShipping = "Miễn phí",
                total,
                formattedTotal = string.Format("{0:N0}đ", total)
            });
        }

        // Xử lý thanh toán
        [HttpPost]
        [Route("ProcessCheckout")]
        public async Task<IActionResult> ProcessCheckout([FromForm] CheckoutViewModel model)
        {
            if (!ModelState.IsValid)
            {
                // Lấy lại dữ liệu giỏ hàng nếu validation thất bại
                var cartItems = await GetCartItemsAsync();
                model.CartItems = cartItems.Select(c => new Models.ViewModels.CartViewModel
                {
                    CartId = c.CartId,
                    ProductId = c.ProductId,
                    ProductName = c.ProductName ?? "",
                    ImageUrl = c.ImageUrl ?? "",
                    Price = c.Price,
                    Quantity = c.Quantity,
                    AttributeId = c.AttributeId,
                    Color = c.Color ?? "",
                    Size = c.Size ?? "",
                    TotalPrice = c.TotalPrice
                }).ToList();
                model.Subtotal = cartItems.Sum(item => item.TotalPrice);
                model.ShippingFee = CalculateShippingFee(model.Subtotal);
                model.Tax = 0;
                model.Total = model.Subtotal + model.ShippingFee + model.Tax;
                
                return View("Checkout", model);
            }

            try
            {
                var cartItems = await GetCartItemsAsync();
                if (!cartItems.Any())
                {
                    TempData["Error"] = "Giỏ hàng của bạn đang trống";
                    return RedirectToAction("Index");
                }

                // Tính toán lại tổng tiền từ dữ liệu thực tế
                decimal subtotal = cartItems.Sum(item => item.TotalPrice);
                decimal tax = 0;
                decimal discountAmount = 0m;
                int? voucherId = null;
                int? userVoucherId = null;

                // Check for user voucher first
                int? userId = HttpContext.Session.GetInt32("UserId");
                var selectedUserVoucherID = HttpContext.Session.GetInt32("SelectedUserVoucherID");
                
                if (selectedUserVoucherID.HasValue && userId.HasValue)
                {
                    var validation = await _voucherService.ValidateAndCalculateByUserVoucherAsync(selectedUserVoucherID.Value, userId.Value, subtotal);
                    if (validation.Success && validation.Voucher != null && validation.UserVoucher != null)
                    {
                        discountAmount = validation.DiscountAmount;
                        voucherId = validation.Voucher.VoucherID;
                        userVoucherId = validation.UserVoucher.UserVoucherID;
                    }
                    else
                    {
                        // clear invalid
                        HttpContext.Session.Remove("SelectedUserVoucherID");
                    }
                }

                decimal totalAmount = subtotal + tax - discountAmount;

                // Debug log
                Console.WriteLine($"Debug - Subtotal: {subtotal:C}, Discount: {discountAmount:C}, TotalAmount: {totalAmount:C}");
                Console.WriteLine($"Debug - PaymentMethod: {model.PaymentMethod}");

                // Tạo đơn hàng
                var order = new Order
                {
                    UserID = userId ?? 0, 
                    OrderDate = DateTime.Now,
                    TotalAmount = totalAmount,
                    Status = "Chờ xử lý",
                    ShippingName = model.ShippingName ?? "",
                    ShippingAddress = model.ShippingAddress ?? "",
                    ShippingPhone = model.ShippingPhone ?? "",
                    Note = model.Note ?? "",
                    VoucherID = voucherId,
                    DiscountAmount = discountAmount
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // clear voucher after order placed
                HttpContext.Session.Remove("VoucherCode");

                // Tạo chi tiết đơn hàng
                foreach (var item in cartItems)
                {
                    var orderItem = new OrderItem
                    {
                        OrderID = order.OrderID,
                        ProductID = item.ProductId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price,
                        AttributeID = item.AttributeId
                    };
                    _context.OrderItems.Add(orderItem);
                }

                // Tạo thông tin thanh toán
                string paymentStatus;
                string paymentMethod = model.PaymentMethod ?? "COD";
                
                // Xác định trạng thái thanh toán dựa trên phương thức
                if (paymentMethod == "COD")
                {
                    paymentStatus = "Chưa thanh toán";
                }
                else if (paymentMethod == "PayPal")
                {
                    // Lấy thông tin PayPal từ form data
                    var paypalOrderId = Request.Form["PayPalOrderId"].ToString();
                    var paypalPayerId = Request.Form["PayPalPayerId"].ToString();
                    var paypalPaymentStatus = Request.Form["PayPalPaymentStatus"].ToString();
                    
                    Console.WriteLine($"PayPal Payment - OrderId: {paypalOrderId}, PayerId: {paypalPayerId}, Status: {paypalPaymentStatus}");
                    
                    // PayPal được coi là đã thanh toán nếu có thông tin hợp lệ
                    if (!string.IsNullOrEmpty(paypalOrderId) && paypalPaymentStatus == "COMPLETED")
                    {
                        paymentStatus = "Đã thanh toán";
                    }
                    else
                    {
                        paymentStatus = "Chưa thanh toán";
                    }
                }
                else if (paymentMethod == "MoMo")
                {
                    paymentStatus = "Đã thanh toán"; // MoMo sẽ được cập nhật khi callback
                }
                else
                {
                    paymentStatus = "Chưa thanh toán"; // Mặc định cho các phương thức khác
                }

                var payment = new Payment
                {
                    OrderID = order.OrderID,
                    Method = paymentMethod,
                    Status = paymentStatus,
                    PaymentDate = DateTime.Now,
                    Amount = totalAmount // Sử dụng giá trị tính toán lại
                };
                _context.Payments.Add(payment);

                await _context.SaveChangesAsync();

                // Mark voucher as used if applicable
                if (userVoucherId.HasValue && userId.HasValue)
                {
                    await _voucherService.MarkVoucherAsUsedAsync(userVoucherId.Value, userId.Value);
                    HttpContext.Session.Remove("SelectedUserVoucherID");
                }

                // Track purchase event with order details
                try
                {
                    await _trackingService.TrackPurchaseAsync(order.OrderID, totalAmount, cartItems.Select(c => c.ProductId).ToArray());
                }
                catch (Exception)
                {
                    // Tracking failure should not affect the main flow
                }

                // Xóa giỏ hàng sau khi đặt hàng thành công
                await ClearCartAsync();

                // Chuyển đến trang cảm ơn
                return RedirectToAction("OrderSuccess", new { orderId = order.OrderID });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in ProcessCheckout: {ex}");
                TempData["Error"] = "Có lỗi xảy ra khi xử lý đơn hàng. Vui lòng thử lại.";
                return RedirectToAction("Checkout");
            }
        }

        // Trang cảm ơn sau khi đặt hàng thành công
        [Route("OrderSuccess")]
        public async Task<IActionResult> OrderSuccess(int orderId)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            if (order == null)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(order);
        }

        // Tính phí vận chuyển
        private decimal CalculateShippingFee(decimal subtotal)
        {
            if (subtotal >= 1000000) // Miễn phí ship cho đơn hàng trên 1 triệu
                return 0;
            else if (subtotal >= 500000) // Giảm phí ship cho đơn hàng trên 500k
                return 15000;
            else
                return 30000; // Phí ship tiêu chuẩn
        }

        // Xóa giỏ hàng sau khi đặt hàng
        private async Task ClearCartAsync()
        {
            int? userId = HttpContext.Session.GetInt32("UserId");
            if (userId.HasValue)
            {
                var cartItems = await _context.Carts.Where(c => c.UserID == userId.Value).ToListAsync();
                _context.Carts.RemoveRange(cartItems);
                await _context.SaveChangesAsync();
            }
            else
            {
                HttpContext.Session.Remove("Cart");
            }
        }

        // PayPal Payment Methods
        
        // Tạo PayPal Payment
        [HttpPost]
        [Route("CreatePayPalPayment")]
        public async Task<IActionResult> CreatePayPalPayment([FromForm] CheckoutViewModel model)
        {
            Console.WriteLine($"[CartController] CreatePayPalPayment called");
            try
            {
                var cartItems = await GetCartItemsAsync();
                Console.WriteLine($"[CartController] Cart items count: {cartItems.Count}");
                if (!cartItems.Any())
                {
                    TempData["Error"] = "Giỏ hàng trống";
                    return RedirectToAction("Checkout");
                }

                // Tính tổng tiền VND (áp dụng voucher, loại bỏ phí vận chuyển)
                decimal subtotal = cartItems.Sum(item => item.TotalPrice);
                decimal discountAmount = 0m;
                int? voucherId = null;
                int? userVoucherId = null;
                
                // Check for user voucher first (priority)
                int? userId = HttpContext.Session.GetInt32("UserId");
                var selectedUserVoucherID = HttpContext.Session.GetInt32("SelectedUserVoucherID");
                
                if (selectedUserVoucherID.HasValue && userId.HasValue)
                {
                    var validation = await _voucherService.ValidateAndCalculateByUserVoucherAsync(selectedUserVoucherID.Value, userId.Value, subtotal);
                    if (validation.Success && validation.Voucher != null && validation.UserVoucher != null)
                    {
                        discountAmount = validation.DiscountAmount;
                        voucherId = validation.Voucher.VoucherID;
                        userVoucherId = validation.UserVoucher.UserVoucherID;
                    }
                    else
                    {
                        HttpContext.Session.Remove("SelectedUserVoucherID");
                    }
                }
                else
                {
                    // Fallback to voucher code if no user voucher
                    var voucherCode = HttpContext.Session.GetString("VoucherCode");
                    if (!string.IsNullOrWhiteSpace(voucherCode))
                    {
                        var validation = await _voucherService.ValidateAndCalculateAsync(voucherCode, subtotal);
                        if (validation.Success && validation.Voucher != null)
                        {
                            discountAmount = validation.DiscountAmount;
                            voucherId = validation.Voucher.VoucherID;
                        }
                    }
                }
                
                decimal totalAmountVND = subtotal - discountAmount;
                if (totalAmountVND < 0) totalAmountVND = 0;
                Console.WriteLine($"[CartController] Subtotal: {subtotal} | Discount: {discountAmount} | Total VND: {totalAmountVND}");

                // Chuyển đổi sang USD (1 USD = 25,000 VND)
                decimal totalAmountUSD = totalAmountVND / 25000;
                Console.WriteLine($"[CartController] Total USD: {totalAmountUSD}");

                // Lưu thông tin checkout vào session để sử dụng sau
                HttpContext.Session.SetString("CheckoutData", JsonSerializer.Serialize(model));
                HttpContext.Session.SetString("TotalAmountVND", totalAmountVND.ToString());
                HttpContext.Session.SetString("VoucherIdPayPal", voucherId?.ToString() ?? "");
                HttpContext.Session.SetString("UserVoucherIdPayPal", userVoucherId?.ToString() ?? "");
                HttpContext.Session.SetString("DiscountAmountPayPal", discountAmount.ToString());

                Console.WriteLine($"[CartController] Calling PayPal service...");
                // Tạo PayPal order
                var paypalOrder = await _payPalService.CreateOrderAsync(totalAmountUSD, "USD");
                Console.WriteLine($"[CartController] PayPal order created: {paypalOrder?.id}");

                // Tìm approval URL
                var approvalUrl = paypalOrder.links?.FirstOrDefault(l => l.rel == "approve")?.href;

                if (string.IsNullOrEmpty(approvalUrl))
                {
                    TempData["Error"] = "Không thể tạo link thanh toán PayPal";
                    return RedirectToAction("Checkout");
                }

                // Redirect đến PayPal để thanh toán
                return Redirect(approvalUrl);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PayPal Error: {ex.Message}");
                TempData["Error"] = "Lỗi khi tạo thanh toán PayPal: " + ex.Message;
                return RedirectToAction("Checkout");
            }
        }

        // PayPal Return - Khi thanh toán thành công
        [Route("PayPalReturn")]
        public async Task<IActionResult> PayPalReturn(string paymentId, string token, string PayerID)
        {
            try
            {
                // Capture payment
                var captureResult = await _payPalService.CaptureOrderAsync(token);

                if (captureResult.status == "COMPLETED")
                {
                    // Lấy thông tin checkout từ session
                    var checkoutDataJson = HttpContext.Session.GetString("CheckoutData");
                    var totalAmountVNDStr = HttpContext.Session.GetString("TotalAmountVND");

                    if (string.IsNullOrEmpty(checkoutDataJson) || string.IsNullOrEmpty(totalAmountVNDStr))
                    {
                        TempData["Error"] = "Phiên làm việc đã hết hạn. Vui lòng thử lại.";
                        return RedirectToAction("Checkout");
                    }

                    var model = JsonSerializer.Deserialize<CheckoutViewModel>(checkoutDataJson);
                    var totalAmountVND = decimal.Parse(totalAmountVNDStr);

                    // Tạo đơn hàng
                    var orderId = await CreateOrderFromPayPal(model, totalAmountVND, token, PayerID);

                    // Xóa session data
                    HttpContext.Session.Remove("CheckoutData");
                    HttpContext.Session.Remove("TotalAmountVND");

                    // Xóa giỏ hàng
                    await ClearCartAsync();

                    TempData["Success"] = "Thanh toán PayPal thành công!";
                    return RedirectToAction("OrderSuccess", new { orderId = orderId });
                }
                else
                {
                    TempData["Error"] = "Thanh toán PayPal không thành công.";
                    return RedirectToAction("Checkout");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"PayPal Return Error: {ex.Message}");
                TempData["Error"] = "Có lỗi xảy ra khi xử lý thanh toán PayPal.";
                return RedirectToAction("Checkout");
            }
        }

        // PayPal Cancel - Khi hủy thanh toán
        [Route("PayPalCancel")]
        public IActionResult PayPalCancel()
        {
            TempData["Error"] = "Bạn đã hủy thanh toán PayPal.";
            return RedirectToAction("Checkout");
        }

        // Tạo đơn hàng từ PayPal payment
        private async Task<int> CreateOrderFromPayPal(CheckoutViewModel model, decimal totalAmount, string paypalOrderId, string paypalPayerId)
        {
            var cartItems = await GetCartItemsAsync();
            var userId = HttpContext.Session.GetInt32("UserId");
            
            // Get voucher info from session (already validated)
            decimal discountAmount = 0m;
            int? voucherId = null;
            int? userVoucherId = null;
            
            var voucherIdStr = HttpContext.Session.GetString("VoucherIdPayPal");
            var userVoucherIdStr = HttpContext.Session.GetString("UserVoucherIdPayPal");
            var discountAmountStr = HttpContext.Session.GetString("DiscountAmountPayPal");
            
            if (!string.IsNullOrEmpty(voucherIdStr) && int.TryParse(voucherIdStr, out int parsedVoucherId))
            {
                voucherId = parsedVoucherId;
            }
            if (!string.IsNullOrEmpty(userVoucherIdStr) && int.TryParse(userVoucherIdStr, out int parsedUserVoucherId))
            {
                userVoucherId = parsedUserVoucherId;
            }
            if (!string.IsNullOrEmpty(discountAmountStr) && decimal.TryParse(discountAmountStr, out decimal parsedDiscount))
            {
                discountAmount = parsedDiscount;
            }

            // Tạo đơn hàng
            var order = new Order
            {
                UserID = userId ?? 0,
                OrderDate = DateTime.Now,
                TotalAmount = totalAmount,
                Status = "Chờ xử lý",
                ShippingName = model.ShippingName ?? "",
                ShippingAddress = model.ShippingAddress ?? "",
                ShippingPhone = model.ShippingPhone ?? "",
                Note = model.Note ?? "",
                VoucherID = voucherId,
                DiscountAmount = discountAmount
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Tạo chi tiết đơn hàng
            foreach (var item in cartItems)
            {
                var orderItem = new OrderItem
                {
                    OrderID = order.OrderID,
                    ProductID = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price,
                    AttributeID = item.AttributeId
                };
                _context.OrderItems.Add(orderItem);
            }

            // Tạo thông tin thanh toán
            var payment = new Payment
            {
                OrderID = order.OrderID,
                Method = "PayPal",
                Status = "Đã thanh toán",
                PaymentDate = DateTime.Now,
                Amount = totalAmount
            };
            _context.Payments.Add(payment);

            await _context.SaveChangesAsync();
            
            // Mark user voucher as used if applicable
            if (userVoucherId.HasValue && userId.HasValue)
            {
                await _voucherService.MarkVoucherAsUsedAsync(userVoucherId.Value, userId.Value);
            }
            
            // Clear voucher sessions
            HttpContext.Session.Remove("VoucherCode");
            HttpContext.Session.Remove("SelectedUserVoucherID");
            HttpContext.Session.Remove("VoucherIdPayPal");
            HttpContext.Session.Remove("UserVoucherIdPayPal");
            HttpContext.Session.Remove("DiscountAmountPayPal");

            return order.OrderID;
        }

        // MoMo Payment Methods

        // Tạo MoMo Payment
        [HttpPost]
        [Route("CreateMoMoPayment")]
        public async Task<IActionResult> CreateMoMoPayment([FromForm] CheckoutViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Thông tin thanh toán không hợp lệ";
                    return RedirectToAction("Checkout");
                }

                // Lưu thông tin checkout vào session để sử dụng sau khi thanh toán
                var checkoutJson = System.Text.Json.JsonSerializer.Serialize(model);
                HttpContext.Session.SetString("CheckoutInfo", checkoutJson);

                var cartItems = await GetCartItemsAsync();
                if (!cartItems.Any())
                {
                    TempData["Error"] = "Giỏ hàng trống";
                    return RedirectToAction("Index");
                }

                // Tính tổng tiền (áp dụng voucher, loại bỏ phí vận chuyển)
                var subtotal = cartItems.Sum(item => item.TotalPrice);
                decimal discountAmount = 0m;
                int? voucherId = null;
                int? userVoucherId = null;
                
                // Check for user voucher first (priority)
                int? userId = HttpContext.Session.GetInt32("UserId");
                var selectedUserVoucherID = HttpContext.Session.GetInt32("SelectedUserVoucherID");
                
                if (selectedUserVoucherID.HasValue && userId.HasValue)
                {
                    var validation = await _voucherService.ValidateAndCalculateByUserVoucherAsync(selectedUserVoucherID.Value, userId.Value, subtotal);
                    if (validation.Success && validation.Voucher != null && validation.UserVoucher != null)
                    {
                        discountAmount = validation.DiscountAmount;
                        voucherId = validation.Voucher.VoucherID;
                        userVoucherId = validation.UserVoucher.UserVoucherID;
                    }
                    else
                    {
                        HttpContext.Session.Remove("SelectedUserVoucherID");
                    }
                }
                else
                {
                    // Fallback to voucher code if no user voucher
                    var voucherCode = HttpContext.Session.GetString("VoucherCode");
                    if (!string.IsNullOrWhiteSpace(voucherCode))
                    {
                        var validation = await _voucherService.ValidateAndCalculateAsync(voucherCode, subtotal);
                        if (validation.Success && validation.Voucher != null)
                        {
                            discountAmount = validation.DiscountAmount;
                            voucherId = validation.Voucher.VoucherID;
                        }
                    }
                }
                
                var totalAmount = subtotal - discountAmount;
                if (totalAmount < 0) totalAmount = 0;

                // Tạo order ID duy nhất
                var orderId = $"ORDER_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString("N")[..8]}";
                var orderInfo = $"Thanh toán đơn hàng {orderId}";

                // Chuyển đổi VND sang đơn vị MoMo (VND)
                var amountLong = (long)totalAmount;

                // Tạo payment request với MoMo
                var momoResponse = await _moMoService.CreatePaymentAsync(orderId, amountLong, orderInfo);

                // Debug: Log response để kiểm tra
                Console.WriteLine($"MoMo Response - ResultCode: {momoResponse.ResultCode}");
                Console.WriteLine($"MoMo Response - Message: {momoResponse.Message}");
                Console.WriteLine($"MoMo Response - QrCodeUrl: {momoResponse.QrCodeUrl}");
                Console.WriteLine($"MoMo Response - PayUrl: {momoResponse.PayUrl}");

                if (momoResponse.ResultCode == 0)
                {
                    // Lưu thông tin payment vào session để xác minh sau này
                    HttpContext.Session.SetString("MoMoOrderId", orderId);
                    HttpContext.Session.SetString("MoMoAmount", amountLong.ToString());
                    HttpContext.Session.SetString("VoucherIdMoMo", voucherId?.ToString() ?? "");
                    HttpContext.Session.SetString("UserVoucherIdMoMo", userVoucherId?.ToString() ?? "");
                    HttpContext.Session.SetString("DiscountAmountMoMo", discountAmount.ToString());

                    // Redirect trực tiếp đến trang thanh toán MoMo
                    return Redirect(momoResponse.PayUrl);
                }
                else
                {
                    TempData["Error"] = $"Không thể tạo thanh toán MoMo: {momoResponse.Message}";
                    return RedirectToAction("Checkout");
                }
            }
            catch
            {
                TempData["Error"] = "Có lỗi xảy ra khi tạo thanh toán MoMo";
                return RedirectToAction("Checkout");
            }
        }

        // MoMo Return - Khi thanh toán thành công/thất bại
        [Route("MoMoReturn")]
        public async Task<IActionResult> MoMoReturn()
        {
            try
            {
                // Lấy tất cả parameters từ query string
                var parameters = Request.Query.ToDictionary(x => x.Key, x => x.Value.ToString());

                if (!parameters.ContainsKey("signature"))
                {
                    TempData["Error"] = "Phản hồi từ MoMo không hợp lệ";
                    return RedirectToAction("Index");
                }

                // Tách signature ra khỏi parameters để verify
                var signature = parameters["signature"];
                var verifyParams = parameters.Where(p => p.Key != "signature").ToDictionary(x => x.Key, x => x.Value);

                // Verify signature
                if (!_moMoService.VerifySignature(signature, verifyParams))
                {
                    Console.WriteLine("[MoMoReturn] Signature không hợp lệ");
                    TempData["Error"] = "Chữ ký MoMo không hợp lệ";
                    return RedirectToAction("Index");
                }

                var resultCode = int.Parse(parameters.GetValueOrDefault("resultCode", "-1"));
                var orderId = parameters.GetValueOrDefault("orderId", "");
                var transId = parameters.GetValueOrDefault("transId", "");

                if (resultCode == 0) // Thành công
                {
                    // Lấy thông tin checkout từ session
                    var checkoutJson = HttpContext.Session.GetString("CheckoutInfo");
                    
                    if (string.IsNullOrEmpty(checkoutJson))
                    {
                        TempData["Error"] = "Không tìm thấy thông tin đơn hàng";
                        return RedirectToAction("Index");
                    }

                    var checkoutInfo = System.Text.Json.JsonSerializer.Deserialize<CheckoutViewModel>(checkoutJson);
                    if (checkoutInfo == null)
                    {
                        TempData["Error"] = "Thông tin đơn hàng không hợp lệ";
                        return RedirectToAction("Index");
                    }

                    // Tạo đơn hàng
                    var amount = decimal.Parse(parameters.GetValueOrDefault("amount", "0"));
                    
                    var newOrderId = await CreateOrderFromMoMo(checkoutInfo, amount, orderId, transId);

                    // Xóa thông tin session
                    HttpContext.Session.Remove("CheckoutInfo");
                    HttpContext.Session.Remove("MoMoOrderId");
                    HttpContext.Session.Remove("MoMoAmount");

                    TempData["Success"] = "Thanh toán MoMo thành công!";
                    return RedirectToAction("OrderSuccess", new { orderId = newOrderId });
                }
                else
                {
                    var message = parameters.GetValueOrDefault("message", "Thanh toán thất bại");
                    TempData["Error"] = $"Thanh toán MoMo thất bại: {message}";
                    return RedirectToAction("Checkout");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[MoMoReturn] Lỗi exception: {ex.Message}");
                TempData["Error"] = "Có lỗi xảy ra khi xử lý phản hồi từ MoMo";
                return RedirectToAction("Index");
            }
        }

        // MoMo IPN - Webhook notification từ MoMo
        [HttpPost]
        [Route("MoMoIPN")]
        public async Task<IActionResult> MoMoIPN()
        {
            try
            {
                // Đọc raw body
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();
                
                if (string.IsNullOrEmpty(body))
                {
                    return BadRequest();
                }

                // Parse JSON
                var ipnData = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, object>>(body);
                if (ipnData == null)
                {
                    return BadRequest();
                }

                var parameters = ipnData.ToDictionary(x => x.Key, x => x.Value?.ToString() ?? "");

                // Verify signature
                if (!parameters.ContainsKey("signature"))
                {
                    return BadRequest();
                }

                var signature = parameters["signature"];
                var verifyParams = parameters.Where(p => p.Key != "signature").ToDictionary(x => x.Key, x => x.Value);

                if (!_moMoService.VerifySignature(signature, verifyParams))
                {
                    return BadRequest();
                }

                var resultCode = int.Parse(parameters.GetValueOrDefault("resultCode", "-1"));
                
                if (resultCode == 0)
                {
                    // Cập nhật trạng thái đơn hàng nếu cần
                    var orderId = parameters.GetValueOrDefault("orderId", "");
                    // Có thể thêm logic cập nhật database ở đây
                }

                return Ok();
            }
            catch
            {
                return BadRequest();
            }
        }

        // Tạo đơn hàng từ MoMo payment
        private async Task<int> CreateOrderFromMoMo(CheckoutViewModel model, decimal totalAmount, string momoOrderId, string transId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            var user = userId.HasValue ? await _context.Users.FindAsync(userId.Value) : null;
            
            // Get voucher info from session (already validated)
            decimal discountAmount = 0m;
            int? voucherId = null;
            int? userVoucherId = null;
            
            var voucherIdStr = HttpContext.Session.GetString("VoucherIdMoMo");
            var userVoucherIdStr = HttpContext.Session.GetString("UserVoucherIdMoMo");
            var discountAmountStr = HttpContext.Session.GetString("DiscountAmountMoMo");
            
            if (!string.IsNullOrEmpty(voucherIdStr) && int.TryParse(voucherIdStr, out int parsedVoucherId))
            {
                voucherId = parsedVoucherId;
            }
            if (!string.IsNullOrEmpty(userVoucherIdStr) && int.TryParse(userVoucherIdStr, out int parsedUserVoucherId))
            {
                userVoucherId = parsedUserVoucherId;
            }
            if (!string.IsNullOrEmpty(discountAmountStr) && decimal.TryParse(discountAmountStr, out decimal parsedDiscount))
            {
                discountAmount = parsedDiscount;
            }

            var order = new Order
            {
                UserID = userId ?? 0,
                OrderDate = DateTime.Now,
                Status = "Chờ xử lý", 
                TotalAmount = totalAmount,
                ShippingName = model.ShippingName ?? (user?.FullName ?? ""),
                ShippingAddress = model.ShippingAddress ?? (user?.Address ?? ""),
                ShippingPhone = model.ShippingPhone ?? (user?.Phone ?? ""),
                Note = model.Note ?? "",
                VoucherID = voucherId,
                DiscountAmount = discountAmount
            };

            _context.Orders.Add(order);
            await _context.SaveChangesAsync();

            // Lấy cart items để tạo order items
            var cartItems = await GetCartItemsAsync();
            
            foreach (var item in cartItems)
            {
                var orderItem = new OrderItem
                {
                    OrderID = order.OrderID,
                    ProductID = item.ProductId,
                    AttributeID = item.AttributeId,
                    Quantity = item.Quantity,
                    UnitPrice = item.Price
                };
                _context.OrderItems.Add(orderItem);
            }

            // Tạo payment record
            var payment = new Payment
            {
                OrderID = order.OrderID,
                Method = "MoMo",
                Amount = totalAmount,
                Status = "Đã thanh toán", 
                PaymentDate = DateTime.Now
            };
            _context.Payments.Add(payment);

            await _context.SaveChangesAsync();

            // Mark user voucher as used if applicable
            if (userVoucherId.HasValue && userId.HasValue)
            {
                await _voucherService.MarkVoucherAsUsedAsync(userVoucherId.Value, userId.Value);
            }
            
            // Clear voucher sessions
            HttpContext.Session.Remove("VoucherCode");
            HttpContext.Session.Remove("SelectedUserVoucherID");
            HttpContext.Session.Remove("VoucherIdMoMo");
            HttpContext.Session.Remove("UserVoucherIdMoMo");
            HttpContext.Session.Remove("DiscountAmountMoMo");

            // Xóa giỏ hàng
            await ClearCartAsync();

            return order.OrderID;
        }

        // Check MoMo payment status via AJAX
        [HttpGet]
        [Route("Cart/CheckMoMoPaymentStatus")]
        public async Task<IActionResult> CheckMoMoPaymentStatus(string orderId)
        {
            Console.WriteLine($"[CheckMoMoPaymentStatus] Called with orderId: {orderId}");
            try
            {
                // Kiểm tra xem có session data không
                var sessionOrderId = HttpContext.Session.GetString("MoMoOrderId");
                if (string.IsNullOrEmpty(sessionOrderId) || sessionOrderId != orderId)
                {
                    return Json(new { success = false, failed = true, message = "Session hết hạn" });
                }

                // Kiểm tra xem có order nào đã được tạo với MoMo payment chưa
                // Tìm order mới nhất có payment MoMo
                var existingOrder = await _context.Orders
                    .Include(o => o.Payments)
                    .Where(o => o.Payments.Any(p => p.Method == "MoMo"))
                    .OrderByDescending(o => o.OrderDate)
                    .FirstOrDefaultAsync();

                if (existingOrder != null)
                {
                    // Xóa session
                    HttpContext.Session.Remove("MoMoOrderId");
                    HttpContext.Session.Remove("MoMoAmount");
                    HttpContext.Session.Remove("CheckoutInfo");
                    
                    return Json(new { success = true, orderIdNew = existingOrder.OrderID });
                }

                // Chưa có order, vẫn đang chờ thanh toán
                return Json(new { success = false, failed = false });
            }
            catch
            {
                return Json(new { success = false, failed = true, message = "Lỗi kiểm tra trạng thái thanh toán" });
            }
        }

        // VnPay Payment Methods

        // Tạo VnPay Payment
        [HttpPost]
        [Route("CreateVnPayPayment")]
        public async Task<IActionResult> CreateVnPayPayment([FromForm] CheckoutViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    TempData["Error"] = "Thông tin thanh toán không hợp lệ";
                    return RedirectToAction("Checkout");
                }

                var cartItems = await GetCartItemsAsync();
                if (!cartItems.Any())
                {
                    TempData["Error"] = "Giỏ hàng trống";
                    return RedirectToAction("Index");
                }

                // Tính tổng tiền (áp dụng voucher, loại bỏ phí vận chuyển)
                decimal subtotal = cartItems.Sum(item => item.TotalPrice);
                decimal discountAmount = 0m;
                int? voucherId = null;
                int? userVoucherId = null;
                
                // Check for user voucher first (priority)
                int? userId = HttpContext.Session.GetInt32("UserId");
                var selectedUserVoucherID = HttpContext.Session.GetInt32("SelectedUserVoucherID");
                
                if (selectedUserVoucherID.HasValue && userId.HasValue)
                {
                    var validation = await _voucherService.ValidateAndCalculateByUserVoucherAsync(selectedUserVoucherID.Value, userId.Value, subtotal);
                    if (validation.Success && validation.Voucher != null && validation.UserVoucher != null)
                    {
                        discountAmount = validation.DiscountAmount;
                        voucherId = validation.Voucher.VoucherID;
                        userVoucherId = validation.UserVoucher.UserVoucherID;
                    }
                    else
                    {
                        HttpContext.Session.Remove("SelectedUserVoucherID");
                    }
                }
                else
                {
                    // Fallback to voucher code if no user voucher
                    var voucherCode = HttpContext.Session.GetString("VoucherCode");
                    if (!string.IsNullOrWhiteSpace(voucherCode))
                    {
                        var validation = await _voucherService.ValidateAndCalculateAsync(voucherCode, subtotal);
                        if (validation.Success && validation.Voucher != null)
                        {
                            discountAmount = validation.DiscountAmount;
                            voucherId = validation.Voucher.VoucherID;
                        }
                    }
                }
                
                decimal totalAmount = subtotal - discountAmount;
                if (totalAmount < 0) totalAmount = 0;

                // Debug: Log cart calculation
                Console.WriteLine($"Cart calculation:");
                Console.WriteLine($"  Cart items count: {cartItems.Count}");
                foreach (var item in cartItems)
                {
                    Console.WriteLine($"  Item: {item.ProductName} - Price: {item.Price} - Qty: {item.Quantity} - Total: {item.TotalPrice}");
                }
                Console.WriteLine($"  Subtotal: {subtotal}");
                Console.WriteLine($"  Discount: {discountAmount}");
                Console.WriteLine($"  Total amount: {totalAmount}");

                // Tạo orderId unique
                var orderId = $"VNPAY_{DateTime.Now:yyyyMMddHHmmss}_{Guid.NewGuid().ToString()[..8]}";
                var orderInfo = $"Payment_for_order_{orderId}"; // Loại bỏ dấu cách và ký tự tiếng Việt
                var ipAddress = GetClientIPAddress();

                // Lưu thông tin checkout vào session
                HttpContext.Session.SetString("CheckoutInfo", JsonSerializer.Serialize(model));
                HttpContext.Session.SetString("VnPayOrderId", orderId);
                HttpContext.Session.SetString("TotalAmount", totalAmount.ToString());
                HttpContext.Session.SetString("VoucherIdVnPay", voucherId?.ToString() ?? "");
                HttpContext.Session.SetString("UserVoucherIdVnPay", userVoucherId?.ToString() ?? "");
                HttpContext.Session.SetString("DiscountAmountVnPay", discountAmount.ToString());

                // Tạo URL thanh toán VNPay
                var paymentUrl = _vnPayService.CreatePaymentUrl(orderId, totalAmount, orderInfo, ipAddress);
                
                return Redirect(paymentUrl);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Có lỗi xảy ra khi tạo thanh toán VNPay";
                return RedirectToAction("Checkout");
            }
        }

        // VnPay Return - Callback từ VNPay
        [Route("VnPayReturn")]
        public async Task<IActionResult> VnPayReturn()
        {
            try
            {
                var vnpayData = Request.Query;
                Console.WriteLine($"VNPay Return received: {Request.QueryString}");

                // Kiểm tra chữ ký từ VNPay
                var hashSecret = _configuration["Vnpay:HashSecret"];
                if (string.IsNullOrEmpty(hashSecret))
                {
                    Console.WriteLine("VNPay HashSecret not configured");
                    TempData["Error"] = "Cấu hình VNPay không đúng";
                    return RedirectToAction("Index");
                }

                bool isValidSignature = _vnPayService.ValidateSignature(vnpayData, hashSecret);

                if (!isValidSignature)
                {
                    Console.WriteLine("VNPay signature validation failed");
                    TempData["Error"] = "Chữ ký không hợp lệ từ VNPay";
                    return RedirectToAction("Index");
                }

                // Lấy thông tin thanh toán từ VNPay
                var responseCode = vnpayData["vnp_ResponseCode"].FirstOrDefault();
                var transactionStatus = vnpayData["vnp_TransactionStatus"].FirstOrDefault();
                var orderId = vnpayData["vnp_TxnRef"].FirstOrDefault();
                var amount = vnpayData["vnp_Amount"].FirstOrDefault();
                var transactionId = vnpayData["vnp_TransactionNo"].FirstOrDefault();

                Console.WriteLine($"VNPay Response - Code: {responseCode}, Status: {transactionStatus}, OrderId: {orderId}, Amount: {amount}");

                // Kiểm tra thanh toán thành công
                if (responseCode == "00" && transactionStatus == "00")
                {
                    // Lấy thông tin checkout từ session
                    var checkoutJson = HttpContext.Session.GetString("CheckoutInfo");
                    if (string.IsNullOrEmpty(checkoutJson))
                    {
                        Console.WriteLine("No checkout info found in session");
                        TempData["Error"] = "Không tìm thấy thông tin đơn hàng";
                        return RedirectToAction("Index");
                    }

                    var checkoutInfo = System.Text.Json.JsonSerializer.Deserialize<CheckoutViewModel>(checkoutJson);
                    if (checkoutInfo == null)
                    {
                        Console.WriteLine("Failed to deserialize checkout info");
                        TempData["Error"] = "Thông tin đơn hàng không hợp lệ";
                        return RedirectToAction("Index");
                    }

                    if (string.IsNullOrEmpty(amount))
                    {
                        Console.WriteLine("Amount is null or empty");
                        TempData["Error"] = "Số tiền thanh toán không hợp lệ";
                        return RedirectToAction("Index");
                    }

                    var totalAmount = decimal.Parse(amount) / 100; // VNPay trả về amount * 100

                    // Tạo đơn hàng
                    var newOrderId = await CreateOrderFromVnPay(checkoutInfo, totalAmount, transactionId ?? "");

                    if (newOrderId > 0)
                    {
                        // Xóa giỏ hàng sau khi đặt hàng thành công
                        await ClearCartAsync();

                        // Xóa session checkout
                        HttpContext.Session.Remove("CheckoutInfo");

                        // Redirect đến trang thành công
                        return RedirectToAction("OrderSuccess", new { orderId = newOrderId });
                    }
                    else
                    {
                        Console.WriteLine("Failed to create order from VNPay payment");
                        TempData["Error"] = "Có lỗi khi tạo đơn hàng từ thanh toán VNPay";
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    Console.WriteLine($"VNPay payment failed - Code: {responseCode}, Status: {transactionStatus}");
                    TempData["Error"] = $"Thanh toán VNPay thất bại. Mã lỗi: {responseCode}";
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"VNPay Return Error: {ex.Message}");
                TempData["Error"] = "Có lỗi xảy ra khi xử lý thanh toán VNPay";
                return RedirectToAction("Index");
            }
        }

        // VnPay IPN - Instant Payment Notification từ VNPay
        [Route("VnPayIpn")]
        [HttpGet]
        public IActionResult VnPayIpn()
        {
            try
            {
                var vnpayData = Request.Query;
                
                // TODO: Implement ProcessVnPayIpn properly later
                Console.WriteLine($"VNPay IPN received: {Request.QueryString}");

                // Return success for now
                return Json(new { RspCode = "00", Message = "Confirm Success" });
            }
            catch (Exception)
            {
                Console.WriteLine($"VNPay IPN Error");
                return Json(new { RspCode = "99", Message = "Input data required" });
            }
        }

        // Tạo đơn hàng từ VnPay payment
        private async Task<int> CreateOrderFromVnPay(CheckoutViewModel model, decimal totalAmount, string transactionId)
        {
            try
            {
                var userId = HttpContext.Session.GetInt32("UserId");
                var user = userId.HasValue ? await _context.Users.FindAsync(userId.Value) : null;
                
                // Get voucher info from session (already validated)
                decimal discountAmount = 0m;
                int? voucherId = null;
                int? userVoucherId = null;
                
                var voucherIdStr = HttpContext.Session.GetString("VoucherIdVnPay");
                var userVoucherIdStr = HttpContext.Session.GetString("UserVoucherIdVnPay");
                var discountAmountStr = HttpContext.Session.GetString("DiscountAmountVnPay");
                
                if (!string.IsNullOrEmpty(voucherIdStr) && int.TryParse(voucherIdStr, out int parsedVoucherId))
                {
                    voucherId = parsedVoucherId;
                }
                if (!string.IsNullOrEmpty(userVoucherIdStr) && int.TryParse(userVoucherIdStr, out int parsedUserVoucherId))
                {
                    userVoucherId = parsedUserVoucherId;
                }
                if (!string.IsNullOrEmpty(discountAmountStr) && decimal.TryParse(discountAmountStr, out decimal parsedDiscount))
                {
                    discountAmount = parsedDiscount;
                }

                var order = new Order
                {
                    UserID = userId ?? 0,
                    OrderDate = DateTime.Now,
                    Status = "Chờ xử lý", // Theo yêu cầu
                    TotalAmount = totalAmount,
                    ShippingName = model.ShippingName ?? (user?.FullName ?? ""),
                    ShippingAddress = model.ShippingAddress ?? (user?.Address ?? ""),
                    ShippingPhone = model.ShippingPhone ?? (user?.Phone ?? ""),
                    Note = model.Note ?? "",
                    VoucherID = voucherId,
                    DiscountAmount = discountAmount
                };

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Tạo OrderItems từ giỏ hàng
                var cartItems = await GetCartItemsAsync();
                foreach (var item in cartItems)
                {
                    var orderItem = new OrderItem
                    {
                        OrderID = order.OrderID,
                        ProductID = item.ProductId,
                        AttributeID = item.AttributeId,
                        Quantity = item.Quantity,
                        UnitPrice = item.Price
                    };
                    _context.OrderItems.Add(orderItem);
                }

                // Tạo Payment record
                var payment = new Payment
                {
                    OrderID = order.OrderID,
                    Method = "VNPay",
                    Status = "Đã thanh toán", // Theo yêu cầu
                    Amount = totalAmount,
                    PaymentDate = DateTime.Now
                };
                _context.Payments.Add(payment);

                await _context.SaveChangesAsync();
                
                // Mark user voucher as used if applicable
                if (userVoucherId.HasValue && userId.HasValue)
                {
                    await _voucherService.MarkVoucherAsUsedAsync(userVoucherId.Value, userId.Value);
                }
                
                // Clear voucher sessions
                HttpContext.Session.Remove("VoucherCode");
                HttpContext.Session.Remove("SelectedUserVoucherID");
                HttpContext.Session.Remove("VoucherIdVnPay");
                HttpContext.Session.Remove("UserVoucherIdVnPay");
                HttpContext.Session.Remove("DiscountAmountVnPay");

                return order.OrderID;
            }
            catch (Exception ex)
            {
                return 0;
            }
        }

        // Helper method để lấy IP address
        private string GetClientIPAddress()
        {
            string ipAddress = Request.Headers["X-Forwarded-For"].FirstOrDefault() ?? "";
            if (string.IsNullOrEmpty(ipAddress) || (ipAddress.ToLower() == "unknown"))
                ipAddress = Request.Headers["X-Real-IP"].FirstOrDefault() ?? "";
            if (string.IsNullOrEmpty(ipAddress) || (ipAddress.ToLower() == "unknown"))
                ipAddress = Request.Headers["HTTP_X_FORWARDED_FOR"].FirstOrDefault() ?? "";
            if (string.IsNullOrEmpty(ipAddress) || (ipAddress.ToLower() == "unknown"))
            {
                var remoteIp = Request.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "";
                // Convert IPv6 localhost to IPv4
                if (remoteIp == "::1")
                    ipAddress = "127.0.0.1";
                else
                    ipAddress = remoteIp;
            }
            if (string.IsNullOrEmpty(ipAddress) || (ipAddress.ToLower() == "unknown"))
                ipAddress = "127.0.0.1";
            return ipAddress;
        }
        

        
    }

    // Lớp hỗ trợ lưu thông tin giỏ hàng trong session
    public class CartSessionItem
    {
        public int SessionCartId { get; set; }
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public int? AttributeId { get; set; }
    }

    // ViewModel hiển thị thông tin giỏ hàng
    public class CartViewModel
    {
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int? AttributeId { get; set; }
        public string Color { get; set; } = "";
        public string Size { get; set; } = "";
        public decimal TotalPrice { get; set; }
    }
}