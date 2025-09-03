using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        public CartController(ApplicationDbContext context, PayPalService payPalService)
        {
            _context = context;
            _payPalService = payPalService;
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
                ShippingFee = CalculateShippingFee(cartItems.Sum(item => item.TotalPrice)),
                Tax = 0 // Có thể thêm thuế nếu cần
            };
            
            model.Total = model.Subtotal + model.ShippingFee + model.Tax;

            // Nếu người dùng đã đăng nhập, lấy thông tin mặc định
            int? userId = HttpContext.Session.GetInt32("UserId");
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
            }

            return View(model);
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
                decimal shippingFee = CalculateShippingFee(subtotal);
                decimal tax = 0;
                decimal totalAmount = subtotal + shippingFee + tax;

                // Debug log
                Console.WriteLine($"Debug - Subtotal: {subtotal:C}, ShippingFee: {shippingFee:C}, TotalAmount: {totalAmount:C}");
                Console.WriteLine($"Debug - PaymentMethod: {model.PaymentMethod}");

                // Tạo đơn hàng
                var userId = HttpContext.Session.GetInt32("UserId");
                var order = new Order
                {
                    UserID = userId ?? 0, // Guest user if not logged in
                    OrderDate = DateTime.Now,
                    TotalAmount = totalAmount, // Sử dụng giá trị tính toán lại
                    Status = "Pending",
                    ShippingName = model.ShippingName ?? "",
                    ShippingAddress = model.ShippingAddress ?? "",
                    ShippingPhone = model.ShippingPhone ?? "",
                    Note = model.Note ?? ""
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
                string paymentStatus = "Pending";
                if (model.PaymentMethod == "PayPal")
                {
                    // Lấy thông tin PayPal từ form data
                    var paypalOrderId = Request.Form["PayPalOrderId"].ToString();
                    var paypalPayerId = Request.Form["PayPalPayerId"].ToString();
                    var paypalPaymentStatus = Request.Form["PayPalPaymentStatus"].ToString();
                    
                    Console.WriteLine($"PayPal Payment - OrderId: {paypalOrderId}, PayerId: {paypalPayerId}, Status: {paypalPaymentStatus}");
                    
                    // Nếu PayPal payment thành công
                    if (!string.IsNullOrEmpty(paypalOrderId) && paypalPaymentStatus == "COMPLETED")
                    {
                        paymentStatus = "Completed";
                        order.Status = "Confirmed"; // Đơn hàng đã xác nhận khi thanh toán PayPal thành công
                    }
                    else
                    {
                        paymentStatus = "Failed";
                    }
                }
                else if (model.PaymentMethod == "CreditCard")
                {
                    paymentStatus = "Processing";
                }

                var payment = new Payment
                {
                    OrderID = order.OrderID,
                    Method = model.PaymentMethod ?? "COD",
                    Status = paymentStatus,
                    PaymentDate = DateTime.Now,
                    Amount = totalAmount // Sử dụng giá trị tính toán lại
                };
                _context.Payments.Add(payment);

                // Cập nhật trạng thái đơn hàng nếu cần
                if (paymentStatus == "Completed")
                {
                    order.Status = "Confirmed";
                    _context.Orders.Update(order);
                }

                await _context.SaveChangesAsync();

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

                // Tính tổng tiền VND
                decimal totalVND = cartItems.Sum(item => item.TotalPrice);
                decimal shippingFee = CalculateShippingFee(totalVND);
                decimal totalAmountVND = totalVND + shippingFee;
                Console.WriteLine($"[CartController] Total VND: {totalAmountVND}");

                // Chuyển đổi sang USD (1 USD = 25,000 VND)
                decimal totalAmountUSD = totalAmountVND / 25000;
                Console.WriteLine($"[CartController] Total USD: {totalAmountUSD}");

                // Lưu thông tin checkout vào session để sử dụng sau
                HttpContext.Session.SetString("CheckoutData", JsonSerializer.Serialize(model));
                HttpContext.Session.SetString("TotalAmountVND", totalAmountVND.ToString());

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

            // Tạo đơn hàng
            var order = new Order
            {
                UserID = userId ?? 0,
                OrderDate = DateTime.Now,
                TotalAmount = totalAmount,
                Status = "Confirmed", // PayPal đã thanh toán thành công
                ShippingName = model.ShippingName ?? "",
                ShippingAddress = model.ShippingAddress ?? "",
                ShippingPhone = model.ShippingPhone ?? "",
                Note = model.Note ?? ""
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
                Status = "Completed",
                PaymentDate = DateTime.Now,
                Amount = totalAmount
            };
            _context.Payments.Add(payment);

            await _context.SaveChangesAsync();

            return order.OrderID;
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