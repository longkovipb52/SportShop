using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Areas.Admin.Models;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;

namespace SportShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var viewModel = new DashboardOverviewViewModel();

            // Get date ranges
            var today = DateTime.Today;
            var startOfWeek = today.AddDays(-(int)today.DayOfWeek);
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfYear = new DateTime(today.Year, 1, 1);

            try
            {
                // Revenue Statistics
                var orders = await _context.Orders
                    .Where(o => o.Status != "Cancelled")
                    .ToListAsync();

                viewModel.TodayRevenue = orders
                    .Where(o => o.OrderDate.Date == today)
                    .Sum(o => o.TotalAmount);

                viewModel.WeekRevenue = orders
                    .Where(o => o.OrderDate >= startOfWeek)
                    .Sum(o => o.TotalAmount);

                viewModel.MonthRevenue = orders
                    .Where(o => o.OrderDate >= startOfMonth)
                    .Sum(o => o.TotalAmount);

                viewModel.YearRevenue = orders
                    .Where(o => o.OrderDate >= startOfYear)
                    .Sum(o => o.TotalAmount);

                // Order Statistics
                viewModel.TodayOrders = orders.Count(o => o.OrderDate.Date == today);
                viewModel.ProcessingOrders = orders.Count(o => o.Status == "Processing" || o.Status == "Chờ xử lý");
                viewModel.CompletedOrders = orders.Count(o => o.Status == "Completed" || o.Status == "Confirmed");
                viewModel.TotalOrders = orders.Count;

                // Customer Statistics
                var users = await _context.Users.ToListAsync();
                
                viewModel.NewCustomersToday = users.Count(u => u.CreatedAt.Date == today);
                viewModel.NewCustomersThisWeek = users.Count(u => u.CreatedAt >= startOfWeek);
                viewModel.NewCustomersThisMonth = users.Count(u => u.CreatedAt >= startOfMonth);
                viewModel.TotalCustomers = users.Count;

                // Top Selling Products
                viewModel.TopSellingProducts = await GetTopSellingProductsAsync();

                // Top Categories
                viewModel.TopCategories = await GetTopCategoriesAsync();

                // Revenue Chart Data (Last 7 days)
                viewModel.RevenueChartData = await GetRevenueChartDataAsync();

                // Growth Percentages
                var lastMonthRevenue = orders
                    .Where(o => o.OrderDate >= startOfMonth.AddMonths(-1) && o.OrderDate < startOfMonth)
                    .Sum(o => o.TotalAmount);

                viewModel.RevenueGrowthPercent = lastMonthRevenue > 0 
                    ? Math.Round(((viewModel.MonthRevenue - lastMonthRevenue) / lastMonthRevenue) * 100, 1)
                    : 0;

                var lastMonthOrders = orders.Count(o => o.OrderDate >= startOfMonth.AddMonths(-1) && o.OrderDate < startOfMonth);
                var thisMonthOrders = orders.Count(o => o.OrderDate >= startOfMonth);
                
                viewModel.OrderGrowthPercent = lastMonthOrders > 0 
                    ? Math.Round(((decimal)(thisMonthOrders - lastMonthOrders) / lastMonthOrders) * 100, 1)
                    : 0;

                var lastMonthCustomers = users.Count(u => u.CreatedAt >= startOfMonth.AddMonths(-1) && u.CreatedAt < startOfMonth);
                
                viewModel.CustomerGrowthPercent = lastMonthCustomers > 0 
                    ? Math.Round(((decimal)(viewModel.NewCustomersThisMonth - lastMonthCustomers) / lastMonthCustomers) * 100, 1)
                    : 0;

                // Admin Notifications
                viewModel.Notifications = await GetAdminNotificationsAsync();
            }
            catch (Exception ex)
            {
                // Log error and set default values
                Console.WriteLine($"Error loading dashboard data: {ex.Message}");
                viewModel.Notifications.Add(new AdminNotification
                {
                    Icon = "fas fa-exclamation-triangle",
                    Title = "Lỗi tải dữ liệu",
                    Message = "Có lỗi xảy ra khi tải dữ liệu dashboard",
                    Type = "error",
                    CreatedAt = DateTime.Now
                });
            }

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> GetChartData(string period = "week")
        {
            try
            {
                var chartData = new List<RevenueChartData>();

                switch (period.ToLower())
                {
                    case "week":
                        chartData = await GetWeeklyChartDataAsync();
                        break;
                    case "month":
                        chartData = await GetMonthlyChartDataAsync();
                        break;
                    case "year":
                        chartData = await GetYearlyChartDataAsync();
                        break;
                    default:
                        chartData = await GetWeeklyChartDataAsync();
                        break;
                }

                // Log for debugging
                Console.WriteLine($"GetChartData - Period: {period}, Data count: {chartData.Count}");
                
                // Ensure we always return valid data
                if (chartData.Count == 0)
                {
                    // Return dummy data if no real data exists
                    chartData = GetDummyChartData(period);
                }

                return Json(chartData);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting chart data: {ex.Message}");
                // Return dummy data in case of error
                return Json(GetDummyChartData(period));
            }
        }

        private List<RevenueChartData> GetDummyChartData(string period)
        {
            var dummyData = new List<RevenueChartData>();
            
            switch (period.ToLower())
            {
                case "week":
                    for (int i = 0; i < 7; i++)
                    {
                        var date = DateTime.Today.AddDays(-6 + i);
                        dummyData.Add(new RevenueChartData
                        {
                            Label = date.ToString("dd/MM"),
                            Value = 0,
                            Date = date
                        });
                    }
                    break;
                case "month":
                    for (int i = 0; i < 4; i++)
                    {
                        var startDate = DateTime.Today.AddDays(-21 + (i * 7));
                        var endDate = startDate.AddDays(6);
                        dummyData.Add(new RevenueChartData
                        {
                            Label = $"{startDate:dd/MM}-{endDate:dd/MM}",
                            Value = 0,
                            Date = startDate
                        });
                    }
                    break;
                case "year":
                    for (int i = 0; i < 12; i++)
                    {
                        var month = DateTime.Today.AddMonths(-11 + i);
                        dummyData.Add(new RevenueChartData
                        {
                            Label = month.ToString("MM/yyyy"),
                            Value = 0,
                            Date = month
                        });
                    }
                    break;
            }
            
            return dummyData;
        }

        private async Task<List<TopSellingProduct>> GetTopSellingProductsAsync()
        {
            try
            {
                var topProducts = await _context.OrderItems
                    .Include(oi => oi.Product)
                        .ThenInclude(p => p.Category)
                    .Include(oi => oi.Product)
                        .ThenInclude(p => p.Brand)
                    .GroupBy(oi => oi.ProductID)
                    .Select(g => new TopSellingProduct
                    {
                        ProductID = g.Key,
                        Name = g.First().Product.Name,
                        ImageURL = g.First().Product.ImageURL ?? "",
                        TotalSold = g.Sum(oi => oi.Quantity),
                        Revenue = g.Sum(oi => oi.Quantity * oi.UnitPrice),
                        CategoryName = g.First().Product.Category.Name,
                        BrandName = g.First().Product.Brand != null ? g.First().Product.Brand.Name : "N/A"
                    })
                    .OrderByDescending(p => p.TotalSold)
                    .Take(5)
                    .ToListAsync();

                return topProducts;
            }
            catch
            {
                return new List<TopSellingProduct>();
            }
        }

        private async Task<List<TopCategory>> GetTopCategoriesAsync()
        {
            try
            {
                var categories = await _context.Categories
                    .Include(c => c.Products)
                    .ToListAsync();

                var topCategories = new List<TopCategory>();

                foreach (var category in categories)
                {
                    var soldItems = await _context.OrderItems
                        .Include(oi => oi.Product)
                        .Where(oi => oi.Product.CategoryID == category.CategoryID)
                        .ToListAsync();

                    var totalSold = soldItems.Sum(oi => oi.Quantity);
                    var revenue = soldItems.Sum(oi => oi.Quantity * oi.UnitPrice);

                    topCategories.Add(new TopCategory
                    {
                        CategoryID = category.CategoryID,
                        Name = category.Name,
                        ImageURL = category.ImageURL ?? "",
                        TotalProducts = category.Products.Count,
                        TotalSold = totalSold,
                        Revenue = revenue
                    });
                }

                var totalRevenue = topCategories.Sum(c => c.Revenue);
                foreach (var category in topCategories)
                {
                    category.Percentage = totalRevenue > 0 ? Math.Round((category.Revenue / totalRevenue) * 100, 1) : 0;
                }

                return topCategories
                    .OrderByDescending(c => c.Revenue)
                    .Take(5)
                    .ToList();
            }
            catch
            {
                return new List<TopCategory>();
            }
        }

        private async Task<List<RevenueChartData>> GetRevenueChartDataAsync()
        {
            try
            {
                var last7Days = Enumerable.Range(0, 7)
                    .Select(i => DateTime.Today.AddDays(-6 + i))
                    .ToList();

                var chartData = new List<RevenueChartData>();

                foreach (var date in last7Days)
                {
                    var dayRevenue = await _context.Orders
                        .Where(o => o.OrderDate.Date == date && o.Status != "Cancelled")
                        .SumAsync(o => o.TotalAmount);

                    chartData.Add(new RevenueChartData
                    {
                        Label = date.ToString("dd/MM"),
                        Value = dayRevenue,
                        Date = date
                    });
                }

                return chartData;
            }
            catch
            {
                return new List<RevenueChartData>();
            }
        }

        private async Task<List<AdminNotification>> GetAdminNotificationsAsync()
        {
            var notifications = new List<AdminNotification>();

            try
            {
                // New orders notification
                var newOrdersCount = await _context.Orders
                    .CountAsync(o => o.OrderDate.Date == DateTime.Today && 
                                    (o.Status == "Processing" || o.Status == "Chờ xử lý"));

                if (newOrdersCount > 0)
                {
                    notifications.Add(new AdminNotification
                    {
                        Icon = "fas fa-shopping-cart",
                        Title = "Đơn hàng mới",
                        Message = $"Có {newOrdersCount} đơn hàng mới cần xử lý hôm nay",
                        Type = "info",
                        CreatedAt = DateTime.Now,
                        Url = "/Admin/Orders"
                    });
                }

                // Low stock notification
                var lowStockProducts = await _context.Products
                    .Where(p => p.Stock <= 5 && p.Stock > 0)
                    .CountAsync();

                if (lowStockProducts > 0)
                {
                    notifications.Add(new AdminNotification
                    {
                        Icon = "fas fa-exclamation-triangle",
                        Title = "Sắp hết hàng",
                        Message = $"{lowStockProducts} sản phẩm sắp hết hàng",
                        Type = "warning",
                        CreatedAt = DateTime.Now,
                        Url = "/Admin/Products"
                    });
                }

                // Out of stock notification
                var outOfStockProducts = await _context.Products
                    .CountAsync(p => p.Stock <= 0);

                if (outOfStockProducts > 0)
                {
                    notifications.Add(new AdminNotification
                    {
                        Icon = "fas fa-times-circle",
                        Title = "Hết hàng",
                        Message = $"{outOfStockProducts} sản phẩm đã hết hàng",
                        Type = "error",
                        CreatedAt = DateTime.Now,
                        Url = "/Admin/Products"
                    });
                }

                // New contacts notification
                var newContacts = await _context.Contacts
                    .CountAsync(c => c.Status == "New");

                if (newContacts > 0)
                {
                    notifications.Add(new AdminNotification
                    {
                        Icon = "fas fa-envelope",
                        Title = "Liên hệ mới",
                        Message = $"Có {newContacts} liên hệ mới chưa phản hồi",
                        Type = "info",
                        CreatedAt = DateTime.Now,
                        Url = "/Admin/Contacts"
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting notifications: {ex.Message}");
            }

            return notifications.OrderByDescending(n => n.CreatedAt).Take(10).ToList();
        }

        private async Task<List<RevenueChartData>> GetWeeklyChartDataAsync()
        {
            try
            {
                var last7Days = Enumerable.Range(0, 7)
                    .Select(i => DateTime.Today.AddDays(-6 + i))
                    .ToList();

                var chartData = new List<RevenueChartData>();

                foreach (var date in last7Days)
                {
                    var dayRevenue = await _context.Orders
                        .Where(o => o.OrderDate.Date == date && o.Status != "Cancelled")
                        .SumAsync(o => o.TotalAmount);

                    chartData.Add(new RevenueChartData
                    {
                        Label = date.ToString("dd/MM"),
                        Value = dayRevenue,
                        Date = date
                    });
                }

                Console.WriteLine($"GetWeeklyChartDataAsync: Generated {chartData.Count} data points");
                return chartData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetWeeklyChartDataAsync: {ex.Message}");
                return new List<RevenueChartData>();
            }
        }

        private async Task<List<RevenueChartData>> GetMonthlyChartDataAsync()
        {
            try
            {
                var chartData = new List<RevenueChartData>();
                
                // Generate last 4 weeks instead of complex grouping
                for (int i = 0; i < 4; i++)
                {
                    var weekStart = DateTime.Today.AddDays(-21 + (i * 7));
                    var weekEnd = weekStart.AddDays(6);

                    var weekRevenue = await _context.Orders
                        .Where(o => o.OrderDate.Date >= weekStart && 
                                   o.OrderDate.Date <= weekEnd && 
                                   o.Status != "Cancelled")
                        .SumAsync(o => o.TotalAmount);

                    chartData.Add(new RevenueChartData
                    {
                        Label = $"{weekStart:dd/MM}-{weekEnd:dd/MM}",
                        Value = weekRevenue,
                        Date = weekStart
                    });
                }

                return chartData.OrderBy(c => c.Date).ToList();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetMonthlyChartDataAsync: {ex.Message}");
                return new List<RevenueChartData>();
            }
        }

        private async Task<List<RevenueChartData>> GetYearlyChartDataAsync()
        {
            try
            {
                var last12Months = Enumerable.Range(0, 12)
                    .Select(i => DateTime.Today.AddMonths(-11 + i))
                    .Select(d => new DateTime(d.Year, d.Month, 1))
                    .ToList();

                var chartData = new List<RevenueChartData>();

                foreach (var month in last12Months)
                {
                    var nextMonth = month.AddMonths(1);
                    
                    var monthRevenue = await _context.Orders
                        .Where(o => o.OrderDate >= month && 
                                   o.OrderDate < nextMonth && 
                                   o.Status != "Cancelled")
                        .SumAsync(o => o.TotalAmount);

                    chartData.Add(new RevenueChartData
                    {
                        Label = month.ToString("MM/yyyy"),
                        Value = monthRevenue,
                        Date = month
                    });
                }

                Console.WriteLine($"GetYearlyChartDataAsync: Generated {chartData.Count} data points");
                return chartData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetYearlyChartDataAsync: {ex.Message}");
                return new List<RevenueChartData>();
            }
        }
    }
}