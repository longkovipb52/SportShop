using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Models;
using SportShop.Models.ViewModels;
using SportShop.Services;
using System.Linq;
using System.Threading.Tasks;

namespace SportShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class OrdersController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly VoucherService _voucherService;
        private readonly EmailService _emailService;

        public OrdersController(ApplicationDbContext context, VoucherService voucherService, EmailService emailService)
        {
            _context = context;
            _voucherService = voucherService;
            _emailService = emailService;
        }

        // GET: Admin/Orders
        public async Task<IActionResult> Index(string searchString, string statusFilter, string sortOrder, int page = 1, int pageSize = 10)
        {
            ViewData["CurrentSort"] = sortOrder;
            ViewData["OrderDateSortParm"] = String.IsNullOrEmpty(sortOrder) ? "date_desc" : "";
            ViewData["AmountSortParm"] = sortOrder == "amount" ? "amount_desc" : "amount";
            ViewData["StatusSortParm"] = sortOrder == "status" ? "status_desc" : "status";
            ViewData["CurrentFilter"] = searchString;
            ViewData["CurrentStatusFilter"] = statusFilter;

            var orders = _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.Payments)
                .AsQueryable();

            // Search functionality
            if (!String.IsNullOrEmpty(searchString))
            {
                orders = orders.Where(o => o.ShippingName.Contains(searchString) ||
                                         o.ShippingPhone.Contains(searchString) ||
                                         o.OrderID.ToString().Contains(searchString) ||
                                         o.User.Email.Contains(searchString));
            }

            // Status filter
            if (!String.IsNullOrEmpty(statusFilter) && statusFilter != "all")
            {
                orders = orders.Where(o => o.Status == statusFilter);
            }

            // Sorting
            switch (sortOrder)
            {
                case "date_desc":
                    orders = orders.OrderByDescending(o => o.OrderDate);
                    break;
                case "amount":
                    orders = orders.OrderBy(o => o.TotalAmount);
                    break;
                case "amount_desc":
                    orders = orders.OrderByDescending(o => o.TotalAmount);
                    break;
                case "status":
                    orders = orders.OrderBy(o => o.Status);
                    break;
                case "status_desc":
                    orders = orders.OrderByDescending(o => o.Status);
                    break;
                default:
                    orders = orders.OrderBy(o => o.OrderDate);
                    break;
            }

            // Pagination
            var totalItems = await orders.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            var ordersList = await orders
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new OrderIndexViewModel
            {
                Orders = ordersList,
                SearchString = searchString,
                StatusFilter = statusFilter,
                SortOrder = sortOrder,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalItems = totalItems
            };

            // Get status options for filter dropdown
            ViewBag.StatusOptions = new List<object>
            {
                new { Value = "all", Text = "Tất cả trạng thái" },
                new { Value = "Chờ xử lý", Text = "Chờ xử lý" },
                new { Value = "Đã xác nhận", Text = "Đã xác nhận" },
                new { Value = "Đang xử lý", Text = "Đang xử lý" },
                new { Value = "Đang giao hàng", Text = "Đang giao hàng" },
                new { Value = "Hoàn thành", Text = "Hoàn thành" },
                new { Value = "Đã hủy", Text = "Đã hủy" }
            };

            // Get available status options for status select (excluding "all")
            ViewBag.StatusSelectOptions = new List<string>
            {
                "Chờ xử lý",
                "Đã xác nhận",
                "Đang xử lý", 
                "Đang giao hàng",
                "Hoàn thành",
                "Đã hủy"
            };

            return View(viewModel);
        }

        // GET: Admin/Orders/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Brand)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Attribute)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(m => m.OrderID == id);

            if (order == null)
            {
                return NotFound();
            }

            var viewModel = new OrderDetailsViewModel
            {
                Order = order,
                OrderItems = order.OrderItems.ToList(),
                Payment = order.Payments.FirstOrDefault()
            };

            // Get available status options for update
            ViewBag.StatusOptions = new List<string>
            {
                "Chờ xử lý",
                "Đã xác nhận",
                "Đang xử lý", 
                "Đang giao hàng",
                "Hoàn thành",
                "Đã hủy"
            };

            return View(viewModel);
        }

        // POST: Admin/Orders/UpdateStatus
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(int orderId, string status)
        {
            var order = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Attribute)
                .FirstOrDefaultAsync(o => o.OrderID == orderId);

            if (order == null)
            {
                return Json(new { success = false, message = "Không tìm thấy đơn hàng" });
            }

            var previousStatus = order.Status;
            order.Status = status;
            
            try
            {
                // If order is being marked as completed, reduce product stock
                if (status == "Hoàn thành" && previousStatus != "Hoàn thành")
                {
                    foreach (var orderItem in order.OrderItems)
                    {
                        var product = orderItem.Product;
                        if (product != null)
                        {
                            // Check if there's enough stock in PRODUCT
                            if (product.Stock < orderItem.Quantity)
                            {
                                return Json(new { 
                                    success = false, 
                                    message = $"Không đủ hàng tồn kho cho sản phẩm '{product.Name}'. Còn lại: {product.Stock}, cần: {orderItem.Quantity}" 
                                });
                            }
                            
                            // Check if there's enough stock in ATTRIBUTE (if exists)
                            if (orderItem.AttributeID.HasValue && orderItem.Attribute != null)
                            {
                                if (orderItem.Attribute.Stock < orderItem.Quantity)
                                {
                                    return Json(new { 
                                        success = false, 
                                        message = $"Không đủ hàng tồn kho cho thuộc tính (Size: {orderItem.Attribute.Size}, Color: {orderItem.Attribute.Color}). Còn lại: {orderItem.Attribute.Stock}, cần: {orderItem.Quantity}" 
                                    });
                                }
                                
                                // Reduce ATTRIBUTE stock
                                orderItem.Attribute.Stock -= orderItem.Quantity;
                                _context.ProductAttributes.Update(orderItem.Attribute);
                            }
                            
                            // Reduce PRODUCT stock (total)
                            product.Stock -= orderItem.Quantity;
                            _context.Products.Update(product);
                        }
                    }
                }
                // If order was completed but now changed to another status, restore stock
                else if (previousStatus == "Hoàn thành" && status != "Hoàn thành")
                {
                    foreach (var orderItem in order.OrderItems)
                    {
                        var product = orderItem.Product;
                        if (product != null)
                        {
                            // Restore ATTRIBUTE stock (if exists)
                            if (orderItem.AttributeID.HasValue && orderItem.Attribute != null)
                            {
                                orderItem.Attribute.Stock += orderItem.Quantity;
                                _context.ProductAttributes.Update(orderItem.Attribute);
                            }
                            
                            // Restore PRODUCT stock (total)
                            product.Stock += orderItem.Quantity;
                            _context.Products.Update(product);
                        }
                    }
                }

                await _context.SaveChangesAsync();
                
                // Gửi email thông báo cập nhật trạng thái đơn hàng
                try
                {
                    var orderWithUser = await _context.Orders
                        .Include(o => o.User)
                        .Include(o => o.OrderItems)
                            .ThenInclude(oi => oi.Product)
                        .FirstOrDefaultAsync(o => o.OrderID == orderId);

                    if (orderWithUser?.User?.Email != null)
                    {
                        var shippingAddress = orderWithUser.ShippingAddress;
                        
                        await _emailService.SendOrderStatusUpdateEmailAsync(
                            orderWithUser.User.Email,
                            orderWithUser.ShippingName,
                            orderId,
                            status,
                            shippingAddress,
                            orderWithUser.TotalAmount,
                            orderWithUser.OrderDate,
                            orderWithUser.OrderItems.ToList()
                        );
                    }
                }
                catch (Exception emailEx)
                {
                    // Log lỗi email nhưng không làm gián đoạn flow cập nhật trạng thái
                    Console.WriteLine($"Lỗi khi gửi email thông báo: {emailEx.Message}");
                }
                
                if (status == "Hoàn thành" && previousStatus != "Hoàn thành" && order.UserID > 0)
                {
                    try
                    {
                        // Tặng voucher sau đơn hàng đầu tiên
                        await _voucherService.AssignFirstOrderVoucherAsync(order.UserID);
                        
                        // Kiểm tra và tặng voucher milestone (1M, 5M, 10M)
                        await _voucherService.CheckAndAssignAllMilestonesAsync(order.UserID);
                    }
                    catch (Exception voucherEx)
                    {
                        // Log lỗi voucher nhưng không làm gián đoạn flow
                        Console.WriteLine($"Lỗi khi tặng voucher: {voucherEx.Message}");
                    }
                }
                
                return Json(new { success = true, message = "Cập nhật trạng thái thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Có lỗi xảy ra: " + ex.Message });
            }
        }

        // GET: Admin/Orders/Print/5
        public async Task<IActionResult> Print(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Brand)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Attribute)
                .Include(o => o.Payments)
                .FirstOrDefaultAsync(m => m.OrderID == id);

            if (order == null)
            {
                return NotFound();
            }

            return View(order);
        }

        // POST: Admin/Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var order = await _context.Orders.FindAsync(id);
            if (order != null)
            {
                // Delete related order items first
                var orderItems = await _context.OrderItems.Where(oi => oi.OrderID == id).ToListAsync();
                _context.OrderItems.RemoveRange(orderItems);

                // Delete related payments
                var payments = await _context.Payments.Where(p => p.OrderID == id).ToListAsync();
                _context.Payments.RemoveRange(payments);

                // Delete order
                _context.Orders.Remove(order);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Index));
        }

        // GET: Admin/Orders/Statistics
        public async Task<IActionResult> Statistics()
        {
            var stats = new
            {
                TotalOrders = await _context.Orders.CountAsync(),
                PendingOrders = await _context.Orders.CountAsync(o => o.Status == "Chờ xử lý"),
                CompletedOrders = await _context.Orders.CountAsync(o => o.Status == "Hoàn thành"),
                CancelledOrders = await _context.Orders.CountAsync(o => o.Status == "Đã hủy"),
                TotalRevenue = await _context.Orders
                    .Where(o => o.Status == "Hoàn thành")
                    .SumAsync(o => o.TotalAmount),
                MonthlyRevenue = await _context.Orders
                    .Where(o => o.Status == "Hoàn thành" && o.OrderDate.Month == DateTime.Now.Month)
                    .SumAsync(o => o.TotalAmount)
            };

            return Json(stats);
        }
    }
}