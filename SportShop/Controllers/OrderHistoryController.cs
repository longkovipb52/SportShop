using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Models;
using SportShop.Models.ViewModels;
using SportShop.Services;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SportShop.Controllers
{
    public class OrderHistoryController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly InteractionTrackingService _trackingService;
        private readonly VoucherService _voucherService; // 🆕 THÊM MỚI

        public OrderHistoryController(
            ApplicationDbContext context, 
            InteractionTrackingService trackingService,
            VoucherService voucherService) // 🆕 THÊM MỚI
        {
            _context = context;
            _trackingService = trackingService;
            _voucherService = voucherService; // 🆕 THÊM MỚI
        }

        // GET: OrderHistory - Hiển thị lịch sử đơn hàng
        public async Task<IActionResult> Index(string? status = null, string? search = null, int page = 1, int pageSize = 10)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Get all orders for the user first
            var allOrders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Brand)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Attribute)
                .Include(o => o.Payments)
                .Where(o => o.UserID == userId.Value)
                .ToListAsync();

            // Apply client-side filtering
            var filteredOrders = allOrders.AsEnumerable();

            // Filter by status
            if (!string.IsNullOrEmpty(status))
            {
                var normalizedStatus = NormalizeStatus(status);
                filteredOrders = filteredOrders.Where(o => NormalizeStatus(o.Status) == normalizedStatus);
            }

            // Search by order ID or product name
            if (!string.IsNullOrEmpty(search))
            {
                filteredOrders = filteredOrders.Where(o => 
                    o.OrderID.ToString().Contains(search) ||
                    o.OrderItems.Any(oi => oi.Product.Name.Contains(search)));
            }

            var totalOrders = filteredOrders.Count();
            var totalPages = (int)Math.Ceiling((double)totalOrders / pageSize);

            var orders = filteredOrders
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            var orderSummaries = orders.Select(o => {
                var statusInfo = GetStatusInfo(o.Status);
                return new OrderSummaryViewModel
                {
                    OrderID = o.OrderID,
                    OrderDate = o.OrderDate,
                    TotalAmount = o.TotalAmount,
                    Status = o.Status,
                    StatusDisplay = statusInfo.DisplayName,
                    StatusClass = statusInfo.ColorClass,
                    ShippingName = o.ShippingName,
                    ItemCount = o.OrderItems.Count,
                    PaymentMethod = o.Payments.FirstOrDefault()?.Method ?? "Chưa thanh toán",
                    PaymentStatus = o.Payments.FirstOrDefault()?.Status ?? "Pending",
                    Items = o.OrderItems.Take(3).Select(oi => new OrderItemSummaryViewModel
                    {
                        ProductID = oi.ProductID,
                        ProductName = oi.Product.Name,
                        ProductImage = oi.Product.ImageURL,
                        Quantity = oi.Quantity,
                        UnitPrice = oi.UnitPrice,
                        Size = oi.Attribute?.Size,
                        Color = oi.Attribute?.Color
                    }).ToList()
                };
            }).ToList();

            // Calculate statistics using the same data
            var totalAmount = allOrders.Sum(o => o.TotalAmount);

            // Calculate statistics by status
            var statistics = new OrderStatusStatisticsViewModel
            {
                PendingCount = allOrders.Count(o => NormalizeStatus(o.Status) == "Pending"),
                ProcessingCount = allOrders.Count(o => NormalizeStatus(o.Status) == "Processing"),
                ShippingCount = allOrders.Count(o => NormalizeStatus(o.Status) == "Shipping"),
                CompletedCount = allOrders.Count(o => NormalizeStatus(o.Status) == "Completed"),
                CancelledCount = allOrders.Count(o => NormalizeStatus(o.Status) == "Cancelled"),
                TotalSpent = totalAmount
            };

            var viewModel = new OrderHistoryViewModel
            {
                Orders = orderSummaries,
                TotalOrders = totalOrders,
                TotalAmount = totalAmount,
                StatusFilter = status,
                SearchTerm = search,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize
            };

            ViewBag.Statistics = statistics;
            return View(viewModel);
        }

        // GET: OrderHistory/Detail/{id} - Hiển thị chi tiết đơn hàng
        public async Task<IActionResult> Detail(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Brand)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Attribute)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderID == id && o.UserID == userId.Value);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng hoặc bạn không có quyền truy cập.";
                return RedirectToAction("Index");
            }

            var payment = order.Payments.FirstOrDefault();

            // Create timeline based on order status
            var timeline = CreateOrderTimeline(order.Status, order.OrderDate, payment?.PaymentDate);
            var statusInfo = GetStatusInfo(order.Status);

            var viewModel = new OrderDetailViewModel
            {
                OrderID = order.OrderID,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                StatusDisplay = statusInfo.DisplayName,
                StatusClass = statusInfo.ColorClass,
                Note = order.Note ?? "",
                ShippingName = order.ShippingName,
                ShippingAddress = order.ShippingAddress,
                ShippingPhone = order.ShippingPhone,
                PaymentMethod = payment?.Method ?? "Chưa thanh toán",
                PaymentStatus = payment?.Status ?? "Pending",
                PaymentDate = payment?.PaymentDate,
                Items = order.OrderItems.Select(oi => new OrderItemDetailViewModel
                {
                    ProductID = oi.ProductID,
                    ProductName = oi.Product.Name,
                    ProductImage = oi.Product.ImageURL,
                    ProductDescription = oi.Product.Description,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    Size = oi.Attribute?.Size,
                    Color = oi.Attribute?.Color,
                    BrandName = oi.Product.Brand?.Name
                }).ToList(),
                Timeline = timeline
            };

            return View(viewModel);
        }

        // GET: OrderHistory/Print/{id} - In đơn hàng
        public async Task<IActionResult> Print(int id)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Brand)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Attribute)
                .Include(o => o.Payments)
                .Include(o => o.User)
                .FirstOrDefaultAsync(o => o.OrderID == id && o.UserID == userId.Value);

            if (order == null)
            {
                return NotFound();
            }

            var payment = order.Payments.FirstOrDefault();
            
            // Create timeline based on order status
            var timeline = CreateOrderTimeline(order.Status, order.OrderDate, payment?.PaymentDate);
            var statusInfo = GetStatusInfo(order.Status);

            var viewModel = new OrderDetailViewModel
            {
                OrderID = order.OrderID,
                OrderDate = order.OrderDate,
                TotalAmount = order.TotalAmount,
                Status = order.Status,
                StatusDisplay = statusInfo.DisplayName,
                StatusClass = statusInfo.ColorClass,
                Note = order.Note ?? "",
                ShippingName = order.ShippingName,
                ShippingAddress = order.ShippingAddress,
                ShippingPhone = order.ShippingPhone,
                PaymentMethod = payment?.Method ?? "Chưa thanh toán",
                PaymentStatus = payment?.Status ?? "Pending",
                PaymentDate = payment?.PaymentDate,
                Items = order.OrderItems.Select(oi => new OrderItemDetailViewModel
                {
                    ProductID = oi.ProductID,
                    ProductName = oi.Product.Name,
                    ProductImage = oi.Product.ImageURL,
                    ProductDescription = oi.Product.Description,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice,
                    Size = oi.Attribute?.Size,
                    Color = oi.Attribute?.Color,
                    BrandName = oi.Product.Brand?.Name
                }).ToList(),
                Timeline = timeline
            };

            ViewBag.CustomerName = order.User.FullName;
            ViewBag.CustomerEmail = order.User.Email;

            return View(viewModel);
        }

        // POST: OrderHistory/SubmitReview - Submit review for product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(int orderId, int productId, int rating, string comment)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để đánh giá" });
            }

            // Verify order belongs to user and is completed
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderID == orderId && o.UserID == userId.Value);

            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
            }

            if (NormalizeStatus(order.Status) != "Completed")
            {
                return Json(new { success = false, message = "Chỉ có thể đánh giá đơn hàng đã hoàn thành" });
            }

            // Verify product in order
            var orderItem = order.OrderItems.FirstOrDefault(oi => oi.ProductID == productId);
            if (orderItem == null)
            {
                return Json(new { success = false, message = "Sản phẩm không có trong đơn hàng" });
            }

            // Check if already reviewed
            var existingReview = await _context.Reviews
                .FirstOrDefaultAsync(r => r.UserID == userId.Value && r.ProductID == productId && r.CreatedAt > order.OrderDate);

            if (existingReview != null)
            {
                return Json(new { success = false, message = "Bạn đã đánh giá sản phẩm này rồi" });
            }

            // Validate rating
            if (rating < 1 || rating > 5)
            {
                return Json(new { success = false, message = "Đánh giá phải từ 1 đến 5 sao" });
            }

            // Create review
            var review = new Review
            {
                ProductID = productId,
                UserID = userId.Value,
                Rating = rating,
                Comment = comment ?? "",
                CreatedAt = DateTime.Now,
                Status = "Pending"
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            // Track write review event
            try
            {
                await _trackingService.TrackWriteReviewAsync(productId, rating);
            }
            catch (Exception)
            {
                // Tracking failure should not affect the main flow
            }

            // 🆕 THÊM MỚI: Tặng voucher khi viết đánh giá
            bool voucherAssigned = false;
            string voucherMessage = "";
            
            try
            {
                voucherAssigned = await _voucherService.AssignReviewVoucherAsync(userId.Value, productId, rating);
                
                if (voucherAssigned)
                {
                    var voucherType = rating >= 5 ? "15%" : rating >= 4 ? "10%" : "5%";
                    voucherMessage = $" Bạn đã nhận được voucher giảm {voucherType} để sử dụng cho lần mua tiếp theo!";
                }
            }
            catch (Exception)
            {
                // Voucher assignment failure should not affect the review submission
            }

            return Json(new { 
                success = true, 
                message = $"Cảm ơn bạn đã đánh giá sản phẩm!{voucherMessage}",
                voucherAssigned = voucherAssigned
            });
        }

        // GET: OrderHistory/CheckReviewed - Check if product has been reviewed
        [HttpGet]
        public async Task<IActionResult> CheckReviewed(int orderId, int productId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { reviewed = false });
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderID == orderId && o.UserID == userId.Value);

            if (order == null)
            {
                return Json(new { reviewed = false });
            }

            var hasReview = await _context.Reviews
                .AnyAsync(r => r.UserID == userId.Value && r.ProductID == productId && r.CreatedAt > order.OrderDate);

            return Json(new { reviewed = hasReview });
        }

        // GET: OrderHistory/GetReview - Get existing review details
        [HttpGet]
        public async Task<IActionResult> GetReview(int orderId, int productId)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập" });
            }

            var order = await _context.Orders
                .FirstOrDefaultAsync(o => o.OrderID == orderId && o.UserID == userId.Value);

            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
            }

            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.UserID == userId.Value && r.ProductID == productId && r.CreatedAt > order.OrderDate);

            if (review == null)
            {
                return Json(new { success = false, message = "Chưa có đánh giá" });
            }

            // Check if review can be edited (within 24 hours)
            var reviewCreatedAt = review.CreatedAt ?? DateTime.Now;
            var hoursSinceReview = (DateTime.Now - reviewCreatedAt).TotalHours;
            var canEdit = hoursSinceReview <= 24;

            return Json(new { 
                success = true, 
                reviewId = review.ReviewID,
                rating = review.Rating, 
                comment = review.Comment,
                createdAt = review.CreatedAt,
                canEdit = canEdit,
                hoursRemaining = canEdit ? Math.Max(0, 24 - hoursSinceReview) : 0
            });
        }

        // POST: OrderHistory/UpdateReview - Update existing review
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateReview(int reviewId, int rating, string comment)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để cập nhật đánh giá" });
            }

            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.ReviewID == reviewId && r.UserID == userId.Value);

            if (review == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đánh giá" });
            }

            // Check if review can still be edited (within 24 hours)
            var reviewCreatedAt = review.CreatedAt ?? DateTime.Now;
            var hoursSinceReview = (DateTime.Now - reviewCreatedAt).TotalHours;
            if (hoursSinceReview > 24)
            {
                return Json(new { success = false, message = "Đã hết thời gian chỉnh sửa đánh giá (24 giờ)" });
            }

            // Validate rating
            if (rating < 1 || rating > 5)
            {
                return Json(new { success = false, message = "Đánh giá phải từ 1 đến 5 sao" });
            }

            // Check if rating was improved and worthy of voucher
            bool ratingImproved = rating > (review.Rating ?? 0);
            
            // Update review
            review.Rating = rating;
            review.Comment = comment ?? "";

            await _context.SaveChangesAsync();

            // Track write review event
            try
            {
                await _trackingService.TrackWriteReviewAsync(review.ProductID, rating);
            }
            catch (Exception)
            {
                // Tracking failure should not affect the main flow
            }

            // 🆕 Tặng voucher nếu rating được cải thiện
            bool voucherAssigned = false;
            string voucherMessage = "";
            
            if (ratingImproved)
            {
                try
                {
                    voucherAssigned = await _voucherService.AssignReviewVoucherAsync(userId.Value, review.ProductID, rating);
                    
                    if (voucherAssigned)
                    {
                        var voucherType = rating >= 5 ? "15%" : rating >= 4 ? "10%" : "5%";
                        voucherMessage = $" Bạn đã nhận được voucher giảm {voucherType} nhờ cải thiện đánh giá!";
                    }
                }
                catch (Exception)
                {
                    // Voucher assignment failure should not affect the review update
                }
            }

            return Json(new { 
                success = true, 
                message = $"Cập nhật đánh giá thành công!{voucherMessage}",
                voucherAssigned = voucherAssigned 
            });
        }

        // POST: OrderHistory/CancelOrder - Cancel order
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelOrder(int orderId, string cancelReason)
        {
            var userId = HttpContext.Session.GetInt32("UserId");
            if (userId == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập để hủy đơn hàng" });
            }

            var order = await _context.Orders
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(o => o.OrderID == orderId && o.UserID == userId.Value);

            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
            }

            // Normalize status for comparison
            var normalizedStatus = NormalizeStatus(order.Status);

            // Only allow cancellation for Pending orders
            if (normalizedStatus != "Pending")
            {
                return Json(new { success = false, message = "Chỉ có thể hủy đơn hàng đang ở trạng thái 'Chờ xử lý'" });
            }

            // Check if payment has been made (online payment)
            var payment = order.Payments.FirstOrDefault();
            if (payment != null && payment.Status == "Completed")
            {
                return Json(new { success = false, message = "Không thể hủy đơn hàng đã thanh toán online. Vui lòng liên hệ hỗ trợ để được hoàn tiền." });
            }

            // Update order status to Cancelled
            order.Status = "Cancelled";
            order.Note = string.IsNullOrEmpty(order.Note) 
                ? $"Đã hủy bởi khách hàng. Lý do: {cancelReason}" 
                : $"{order.Note}\nĐã hủy bởi khách hàng. Lý do: {cancelReason}";

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "Hủy đơn hàng thành công!" });
        }

        private List<OrderTimelineViewModel> CreateOrderTimeline(string currentStatus, DateTime orderDate, DateTime? paymentDate)
        {
            var timeline = new List<OrderTimelineViewModel>();
            
            // Normalize the current status
            var normalizedStatus = NormalizeStatus(currentStatus);

            // Define order statuses and their sequence
            var statusFlow = new[]
            {
                new { Status = "Pending", Description = "Đơn hàng đã được tạo", Icon = "fas fa-shopping-cart" },
                new { Status = "Processing", Description = "Đang xử lý đơn hàng", Icon = "fas fa-cogs" },
                new { Status = "Shipping", Description = "Đang giao hàng", Icon = "fas fa-truck" },
                new { Status = "Completed", Description = "Đã giao hàng thành công", Icon = "fas fa-check-circle" }
            };

            var currentStatusIndex = Array.FindIndex(statusFlow, s => s.Status == normalizedStatus);
            
            // Handle cancelled status
            if (normalizedStatus == "Cancelled")
            {
                timeline.Add(new OrderTimelineViewModel
                {
                    Status = "Pending",
                    Description = "Đơn hàng đã được tạo",
                    Date = orderDate,
                    IsCompleted = true,
                    IsCurrent = false
                });
                
                timeline.Add(new OrderTimelineViewModel
                {
                    Status = "Cancelled",
                    Description = "Đơn hàng đã bị hủy",
                    Date = orderDate.AddHours(1),
                    IsCompleted = true,
                    IsCurrent = true
                });
                
                return timeline;
            }

            for (int i = 0; i < statusFlow.Length; i++)
            {
                var status = statusFlow[i];
                var estimatedDate = orderDate.AddDays(i);
                
                timeline.Add(new OrderTimelineViewModel
                {
                    Status = status.Status,
                    Description = status.Description,
                    Date = i <= currentStatusIndex ? estimatedDate : DateTime.MinValue,
                    IsCompleted = i <= currentStatusIndex,
                    IsCurrent = i == currentStatusIndex
                });
            }

            return timeline;
        }

        // Helper method to normalize status (convert Vietnamese to English for internal processing)
        private string NormalizeStatus(string status)
        {
            if (string.IsNullOrEmpty(status)) return "Pending";

            return status.ToLower() switch
            {
                "pending" or "chờ xử lý" or "cho xu ly" => "Pending",
                "processing" or "đang xử lý" or "dang xu ly" => "Processing", 
                "shipping" or "đang giao hàng" or "dang giao hang" => "Shipping",
                "completed" or "hoàn thành" or "hoan thanh" or "đã giao" or "da giao" => "Completed",
                "cancelled" or "đã hủy" or "da huy" or "hủy" or "huy" => "Cancelled",
                _ => status // Return original if no match
            };
        }

        // Helper method to get status display name and color
        private (string DisplayName, string ColorClass) GetStatusInfo(string status)
        {
            var normalizedStatus = NormalizeStatus(status);
            return normalizedStatus switch
            {
                "Pending" => ("Chờ xử lý", "status-pending"),
                "Processing" => ("Đang xử lý", "status-processing"),
                "Shipping" => ("Đang giao", "status-shipping"),
                "Completed" => ("Hoàn thành", "status-completed"),
                "Cancelled" => ("Đã hủy", "status-cancelled"),
                _ => ("Không xác định", "status-unknown")
            };
        }

    }
}