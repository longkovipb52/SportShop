using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Models;
using SportShop.Areas.Admin.Models;

namespace SportShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class CustomersController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomersController(ApplicationDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Customers
        public async Task<IActionResult> Index(int page = 1, int pageSize = 10, string search = "")
        {
            ViewData["Title"] = "Quản lý khách hàng";
            
            var query = _context.Users
                .Include(u => u.Role)
                .Include(u => u.Orders)
                .Where(u => u.Role.RoleName == "Customer"); // Chỉ lấy khách hàng

            // Tìm kiếm
            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(u => u.Username.Contains(search) ||
                                   u.FullName.Contains(search) ||
                                   u.Email.Contains(search) ||
                                   u.Phone.Contains(search));
            }

            // Đếm tổng số bản ghi
            var totalCustomers = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalCustomers / (double)pageSize);

            // Phân trang
            var customers = await query
                .OrderByDescending(u => u.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new CustomerViewModel
                {
                    UserID = u.UserID,
                    Username = u.Username,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.Phone,
                    Address = u.Address,
                    CreatedAt = u.CreatedAt,
                    TotalOrders = u.Orders.Count(),
                    TotalSpent = u.Orders.Where(o => o.Status == "Hoàn thành").Sum(o => o.TotalAmount)
                })
                .ToListAsync();

            var viewModel = new CustomerListViewModel
            {
                Customers = customers,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                Search = search,
                TotalCustomers = totalCustomers
            };

            return View(viewModel);
        }

        // GET: Admin/Customers/Details/5
        public async Task<IActionResult> Details(int id)
        {
            ViewData["Title"] = "Chi tiết khách hàng";
            
            var customer = await _context.Users
                .Include(u => u.Role)
                .Include(u => u.Orders)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                .FirstOrDefaultAsync(u => u.UserID == id);

            if (customer == null || customer.Role.RoleName != "Customer")
            {
                return NotFound();
            }

            var viewModel = new CustomerDetailViewModel
            {
                UserID = customer.UserID,
                Username = customer.Username,
                FullName = customer.FullName,
                Email = customer.Email,
                Phone = customer.Phone,
                Address = customer.Address,
                CreatedAt = customer.CreatedAt,
                TotalOrders = customer.Orders.Count(),
                TotalSpent = customer.Orders.Where(o => o.Status == "Hoàn thành").Sum(o => o.TotalAmount),
                RecentOrders = customer.Orders
                    .OrderByDescending(o => o.OrderDate)
                    .Take(5)
                    .Select(o => new OrderSummaryViewModel
                    {
                        OrderID = o.OrderID,
                        OrderDate = o.OrderDate,
                        TotalAmount = o.TotalAmount,
                        Status = o.Status,
                        StatusDisplay = o.Status
                    })
                    .ToList()
            };

            return View(viewModel);
        }

        // GET: Admin/Customers/PurchaseHistory/5
        public async Task<IActionResult> PurchaseHistory(int id, int page = 1, int pageSize = 10)
        {
            var customer = await _context.Users
                .Include(u => u.Role)
                .FirstOrDefaultAsync(u => u.UserID == id);

            if (customer == null || customer.Role.RoleName != "Customer")
            {
                return NotFound();
            }

            ViewData["Title"] = $"Lịch sử mua hàng - {customer.FullName}";
            ViewBag.Customer = customer;

            var query = _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Attribute)
                .Where(o => o.UserID == id);

            // Đếm tổng số đơn hàng
            var totalOrders = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(totalOrders / (double)pageSize);

            // Phân trang
            var orders = await query
                .OrderByDescending(o => o.OrderDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            var viewModel = new PurchaseHistoryViewModel
            {
                Customer = new CustomerViewModel
                {
                    UserID = customer.UserID,
                    Username = customer.Username,
                    FullName = customer.FullName,
                    Email = customer.Email,
                    Phone = customer.Phone,
                    Address = customer.Address,
                    CreatedAt = customer.CreatedAt
                },
                Orders = orders,
                CurrentPage = page,
                TotalPages = totalPages,
                PageSize = pageSize,
                TotalOrders = totalOrders
            };

            return View(viewModel);
        }

        // GET: Admin/Customers/OrderDetails/5
        public async Task<IActionResult> OrderDetails(int id)
        {
            var order = await _context.Orders
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Brand)
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Attribute)
                .FirstOrDefaultAsync(o => o.OrderID == id);

            if (order == null)
            {
                return NotFound();
            }

            ViewData["Title"] = $"Chi tiết đơn hàng #{order.OrderID}";
            
            return View(order);
        }
    }
}