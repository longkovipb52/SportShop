using Microsoft.EntityFrameworkCore;
using Mscc.GenerativeAI;
using SportShop.Data;
using System.Text;

namespace SportShop.Services
{
    public class ChatbotService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _model;
        private readonly int _maxTokens;
        private readonly double _temperature;

        public ChatbotService(ApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _apiKey = _configuration["Gemini:ApiKey"] ?? "";
            _model = _configuration["Gemini:Model"] ?? "gemini-1.5-flash";
            _maxTokens = int.Parse(_configuration["Gemini:MaxTokens"] ?? "500");
            _temperature = double.Parse(_configuration["Gemini:Temperature"] ?? "0.7");
        }

        public async Task<string> GetResponseAsync(string userMessage, int? userId = null)
        {
            try
            {
                // 1. Lấy context từ database
                var contextData = await GetDatabaseContextAsync(userMessage, userId);

                // 2. Tạo system prompt với context
                var systemPrompt = BuildSystemPrompt(contextData);

                // 3. Gọi Gemini API
                var gemini = new GoogleAI(apiKey: _apiKey);
                var model = gemini.GenerativeModel(model: _model);

                // Kết hợp system prompt và user message
                var fullPrompt = $"{systemPrompt}\n\nUser: {userMessage}\n\nAssistant:";

                var response = await model.GenerateContent(fullPrompt);

                if (response != null && !string.IsNullOrEmpty(response.Text))
                {
                    return response.Text;
                }
                else
                {
                    Console.WriteLine("Gemini Error: Empty response");
                    return "Xin lỗi, tôi đang gặp chút vấn đề. Vui lòng thử lại sau hoặc liên hệ hotline (028) 3835 4266. 😊";
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Chatbot Error: {ex.Message}");
                return "Xin lỗi, tôi đang gặp chút vấn đề. Vui lòng thử lại sau hoặc liên hệ hotline (028) 3835 4266 để được hỗ trợ trực tiếp. 😊";
            }
        }

        private async Task<DatabaseContext> GetDatabaseContextAsync(string userMessage, int? userId)
        {
            var context = new DatabaseContext();

            // Phân tích ý định của user
            var intent = AnalyzeIntent(userMessage);

            switch (intent)
            {
                case "price_highest":
                    context.Products = await GetProductsByPriceAsync(isHighest: true);
                    break;

                case "price_lowest":
                    context.Products = await GetProductsByPriceAsync(isHighest: false);
                    break;

                case "product_search":
                    context.Products = await GetRelevantProductsAsync(userMessage);
                    context.Categories = await _context.Categories.Select(c => c.Name).ToListAsync();
                    context.Brands = await _context.Brands.Select(b => b.Name).ToListAsync();
                    break;

                case "order_status":
                    if (userId.HasValue)
                    {
                        context.UserOrders = await GetUserOrdersAsync(userId.Value);
                    }
                    break;

                case "voucher_info":
                    context.AvailableVouchers = await GetAvailableVouchersAsync(userId);
                    break;

                case "product_info":
                    context.Products = await GetRelevantProductsAsync(userMessage);
                    break;

                default:
                    // General info
                    context.Categories = await _context.Categories.Select(c => c.Name).ToListAsync();
                    context.Brands = await _context.Brands.Select(b => b.Name).ToListAsync();
                    context.TopProducts = await GetTopProductsAsync();
                    break;
            }

            return context;
        }

        private string AnalyzeIntent(string message)
        {
            message = message.ToLower();

            // Kiểm tra câu hỏi về giá cao nhất/thấp nhất
            if ((message.Contains("giá") && (message.Contains("cao nhất") || message.Contains("đắt nhất") || 
                message.Contains("cao") || message.Contains("đắt"))) ||
                (message.Contains("sản phẩm") && (message.Contains("cao nhất") || message.Contains("đắt nhất"))))
                return "price_highest";

            if ((message.Contains("giá") && (message.Contains("thấp nhất") || message.Contains("rẻ nhất") || 
                message.Contains("thấp") || message.Contains("rẻ"))) ||
                (message.Contains("sản phẩm") && (message.Contains("thấp nhất") || message.Contains("rẻ nhất"))))
                return "price_lowest";

            if (message.Contains("tìm") || message.Contains("mua") || message.Contains("sản phẩm") || 
                message.Contains("giày") || message.Contains("áo") || message.Contains("quần"))
                return "product_search";

            if (message.Contains("đơn hàng") || message.Contains("order") || message.Contains("theo dõi") ||
                message.Contains("giao hàng") || message.Contains("ship"))
                return "order_status";

            if (message.Contains("voucher") || message.Contains("mã giảm giá") || message.Contains("khuyến mãi") ||
                message.Contains("giảm giá") || message.Contains("coupon"))
                return "voucher_info";

            if (message.Contains("giá") || message.Contains("chi tiết") || message.Contains("thông tin") ||
                message.Contains("size") || message.Contains("màu"))
                return "product_info";

            return "general";
        }

        private async Task<List<ProductInfo>> GetRelevantProductsAsync(string query)
        {
            query = query.ToLower();
            
            var products = await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => p.Name.ToLower().Contains(query) || 
                           (p.Description != null && p.Description.ToLower().Contains(query)) ||
                           p.Category.Name.ToLower().Contains(query) ||
                           (p.Brand != null && p.Brand.Name.ToLower().Contains(query)))
                .OrderByDescending(p => p.TotalLikes)
                .Take(5)
                .Select(p => new ProductInfo
                {
                    Name = p.Name,
                    Price = p.Price,
                    Description = p.Description ?? "",
                    Category = p.Category.Name,
                    Brand = p.Brand != null ? p.Brand.Name : "Chưa xác định",
                    Stock = p.Stock,
                    ProductID = p.ProductID
                })
                .ToListAsync();

            return products;
        }

        private async Task<List<ProductInfo>> GetProductsByPriceAsync(bool isHighest)
        {
            var query = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand);

            var products = isHighest 
                ? await query.OrderByDescending(p => p.Price).Take(10).ToListAsync()
                : await query.OrderBy(p => p.Price).Take(10).ToListAsync();

            return products.Select(p => new ProductInfo
            {
                Name = p.Name,
                Price = p.Price,
                Description = p.Description ?? "",
                Category = p.Category.Name,
                Brand = p.Brand != null ? p.Brand.Name : "Chưa xác định",
                Stock = p.Stock,
                ProductID = p.ProductID
            }).ToList();
        }

        private async Task<List<OrderInfo>> GetUserOrdersAsync(int userId)
        {
            var orders = await _context.Orders
                .Where(o => o.UserID == userId)
                .OrderByDescending(o => o.OrderDate)
                .Take(5)
                .Select(o => new OrderInfo
                {
                    OrderID = o.OrderID,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status ?? "Chờ xử lý"
                })
                .ToListAsync();

            return orders;
        }

        private async Task<List<VoucherInfo>> GetAvailableVouchersAsync(int? userId)
        {
            var now = DateTime.Now;
            var vouchers = await _context.Vouchers
                .Where(v => v.IsActive && v.StartDate <= now && v.EndDate >= now)
                .Take(5)
                .Select(v => new VoucherInfo
                {
                    Code = v.Code,
                    DiscountType = v.DiscountType,
                    DiscountValue = v.DiscountValue,
                    MinOrderAmount = v.MinOrderAmount ?? 0,
                    EndDate = v.EndDate
                })
                .ToListAsync();

            return vouchers;
        }

        private async Task<List<ProductInfo>> GetTopProductsAsync()
        {
            return await _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .OrderByDescending(p => p.TotalLikes)
                .Take(5)
                .Select(p => new ProductInfo
                {
                    Name = p.Name,
                    Price = p.Price,
                    Category = p.Category.Name,
                    Brand = p.Brand != null ? p.Brand.Name : "Chưa xác định",
                    ProductID = p.ProductID
                })
                .ToListAsync();
        }

        private string BuildSystemPrompt(DatabaseContext context)
        {
            var sb = new StringBuilder();
            
            sb.AppendLine("Bạn là trợ lý AI của LoLoSport - cửa hàng thể thao trực tuyến hàng đầu Việt Nam.");
            sb.AppendLine("Nhiệm vụ của bạn là hỗ trợ khách hàng một cách thân thiện, chuyên nghiệp và chính xác.");
            sb.AppendLine();
            sb.AppendLine("THÔNG TIN CỬA HÀNG:");
            sb.AppendLine("- Tên: LoLoSport");
            sb.AppendLine("- Hotline: (028) 3835 4266");
            sb.AppendLine("- Email: support@lolosport.com");
            sb.AppendLine("- Địa chỉ: 227 Nguyễn Văn Cừ, Quận 5, TP.HCM");
            sb.AppendLine("- Giờ làm việc: 8:00 - 22:00 hằng ngày");
            sb.AppendLine();
            sb.AppendLine("CHÍNH SÁCH:");
            sb.AppendLine("- Miễn phí ship đơn hàng từ 500.000đ");
            sb.AppendLine("- Đổi trả miễn phí trong 30 ngày");
            sb.AppendLine("- Bảo hành 6-12 tháng tùy sản phẩm");
            sb.AppendLine("- Thanh toán: COD, PayPal, MoMo, VNPay");
            sb.AppendLine();

            if (context.Categories.Any())
            {
                sb.AppendLine($"DANH MỤC SẢN PHẨM: {string.Join(", ", context.Categories)}");
            }

            if (context.Brands.Any())
            {
                sb.AppendLine($"THƯƠNG HIỆU: {string.Join(", ", context.Brands)}");
            }

            if (context.Products.Any())
            {
                sb.AppendLine();
                sb.AppendLine("SẢN PHẨM LIÊN QUAN:");
                foreach (var product in context.Products)
                {
                    sb.AppendLine($"- {product.Name}");
                    sb.AppendLine($"  + Giá: {product.Price:N0}đ");
                    sb.AppendLine($"  + Danh mục: {product.Category}");
                    sb.AppendLine($"  + Thương hiệu: {product.Brand}");
                    sb.AppendLine($"  + Tình trạng: {(product.Stock > 0 ? $"Còn {product.Stock} sản phẩm" : "Hết hàng")}");
                    sb.AppendLine($"  + Link: /Product/Details/{product.ProductID}");
                    if (!string.IsNullOrEmpty(product.Description) && product.Description.Length > 0)
                    {
                        var desc = product.Description.Length > 100 
                            ? product.Description.Substring(0, 100) + "..." 
                            : product.Description;
                        sb.AppendLine($"  + Mô tả: {desc}");
                    }
                }
            }

            if (context.TopProducts.Any() && !context.Products.Any())
            {
                sb.AppendLine();
                sb.AppendLine("SẢN PHẨM HOT HIỆN TẠI:");
                foreach (var product in context.TopProducts)
                {
                    sb.AppendLine($"- {product.Name} ({product.Category} - {product.Brand}) - {product.Price:N0}đ");
                    sb.AppendLine($"  Link: /Product/Details/{product.ProductID}");
                }
            }

            if (context.UserOrders.Any())
            {
                sb.AppendLine();
                sb.AppendLine("ĐƠN HÀNG CỦA KHÁCH:");
                foreach (var order in context.UserOrders)
                {
                    sb.AppendLine($"- Đơn #{order.OrderID}: {order.TotalAmount:N0}đ - {order.Status} ({order.OrderDate:dd/MM/yyyy})");
                }
            }

            if (context.AvailableVouchers.Any())
            {
                sb.AppendLine();
                sb.AppendLine("VOUCHER KHUYẾN MÃI:");
                foreach (var voucher in context.AvailableVouchers)
                {
                    var discount = voucher.DiscountType == "Percentage" 
                        ? $"Giảm {voucher.DiscountValue}%" 
                        : $"Giảm {voucher.DiscountValue:N0}đ";
                    var minOrder = voucher.MinOrderAmount > 0 
                        ? $"cho đơn từ {voucher.MinOrderAmount:N0}đ" 
                        : "";
                    sb.AppendLine($"- Mã {voucher.Code}: {discount} {minOrder} (HSD: {voucher.EndDate:dd/MM/yyyy})");
                }
            }

            sb.AppendLine();
            sb.AppendLine("QUY TẮC TRẢ LỜI:");
            sb.AppendLine("1. Luôn lịch sự, thân thiện và nhiệt tình");
            sb.AppendLine("2. Trả lời ngắn gọn, súc tích (tối đa 200 từ)");
            sb.AppendLine("3. Sử dụng emoji phù hợp để thân thiện hơn");
            sb.AppendLine("4. Nếu có sản phẩm phù hợp, gợi ý cụ thể với link");
            sb.AppendLine("5. Nếu không chắc chắn, đề nghị liên hệ hotline");
            sb.AppendLine("6. Luôn kết thúc bằng câu hỏi để tiếp tục hội thoại");
            sb.AppendLine("7. Định dạng link sản phẩm: [Xem chi tiết](/Product/Details/{ProductID})");

            return sb.ToString();
        }
    }

    // Helper classes
    public class DatabaseContext
    {
        public List<string> Categories { get; set; } = new();
        public List<string> Brands { get; set; } = new();
        public List<ProductInfo> Products { get; set; } = new();
        public List<ProductInfo> TopProducts { get; set; } = new();
        public List<OrderInfo> UserOrders { get; set; } = new();
        public List<VoucherInfo> AvailableVouchers { get; set; } = new();
    }

    public class ProductInfo
    {
        public int ProductID { get; set; }
        public string Name { get; set; } = "";
        public decimal Price { get; set; }
        public string Description { get; set; } = "";
        public string Category { get; set; } = "";
        public string Brand { get; set; } = "";
        public int Stock { get; set; }
    }

    public class OrderInfo
    {
        public int OrderID { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "";
    }

    public class VoucherInfo
    {
        public string Code { get; set; } = "";
        public string DiscountType { get; set; } = "";
        public decimal DiscountValue { get; set; }
        public decimal MinOrderAmount { get; set; }
        public DateTime EndDate { get; set; }
    }
}
