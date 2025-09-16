using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Models;
using SportShop.Models.ViewModels;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace SportShop.Controllers
{
    public class OrderHistoryController : Controller
    {
        private readonly ApplicationDbContext _context;

        public OrderHistoryController(ApplicationDbContext context)
        {
            _context = context;
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