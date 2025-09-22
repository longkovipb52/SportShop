using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Models;
using SportShop.Models.DTOs;
using System.Globalization;

namespace SportShop.Areas.Admin.Controllers
{
    [Area("Admin")]
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index(ReportFilterDTO filter)
        {
            // Set default filter values
            if (!filter.StartDate.HasValue)
                filter.StartDate = DateTime.Now.AddMonths(-1);
            if (!filter.EndDate.HasValue)
                filter.EndDate = DateTime.Now;

            var viewModel = new ReportsIndexViewModel
            {
                Filter = filter,
                Overview = await GetDashboardOverview(),
                Categories = await _context.Categories.ToListAsync(),
                Brands = await _context.Brands.ToListAsync()
            };

            // Get data based on filter
            viewModel.RevenueData = await GetRevenueReport(filter);
            viewModel.TopProducts = await GetTopProductsReport(filter);
            viewModel.TopCustomers = await GetTopCustomersReport(filter);
            viewModel.PaymentMethods = await GetPaymentMethodReport(filter);

            // Prepare chart data
            viewModel.RevenueChartData = PrepareRevenueChartData(viewModel.RevenueData);
            viewModel.ProductChartData = PrepareProductChartData(viewModel.TopProducts);
            viewModel.PaymentChartData = PreparePaymentChartData(viewModel.PaymentMethods);

            return View(viewModel);
        }

        #region Dashboard Overview
        private async Task<DashboardOverviewDTO> GetDashboardOverview()
        {
            var today = DateTime.Today;
            var startOfMonth = new DateTime(today.Year, today.Month, 1);
            var startOfYear = new DateTime(today.Year, 1, 1);

            var overview = new DashboardOverviewDTO();

            // Revenue calculations
            overview.TodayRevenue = await _context.Orders
                .Where(o => o.OrderDate.Date == today && o.Status == "Hoàn thành")
                .SumAsync(o => o.TotalAmount);

            overview.MonthRevenue = await _context.Orders
                .Where(o => o.OrderDate >= startOfMonth && o.Status == "Hoàn thành")
                .SumAsync(o => o.TotalAmount);

            overview.YearRevenue = await _context.Orders
                .Where(o => o.OrderDate >= startOfYear && o.Status == "Hoàn thành")
                .SumAsync(o => o.TotalAmount);

            // Order counts
            overview.TodayOrders = await _context.Orders
                .Where(o => o.OrderDate.Date == today)
                .CountAsync();

            overview.MonthOrders = await _context.Orders
                .Where(o => o.OrderDate >= startOfMonth)
                .CountAsync();

            overview.YearOrders = await _context.Orders
                .Where(o => o.OrderDate >= startOfYear)
                .CountAsync();

            // Customer statistics
            overview.TotalCustomers = await _context.Users
                .Where(u => u.RoleID == 2) // Customer role
                .CountAsync();

            overview.NewCustomersThisMonth = await _context.Users
                .Where(u => u.RoleID == 2 && u.CreatedAt.HasValue && u.CreatedAt.Value >= startOfMonth)
                .CountAsync();

            // Product statistics
            overview.TotalProducts = await _context.Products.CountAsync();
            overview.LowStockProducts = await _context.Products
                .Where(p => p.Stock < 10)
                .CountAsync();

            // Calculate average order value
            if (overview.MonthOrders > 0)
            {
                overview.AverageOrderValue = overview.MonthRevenue / overview.MonthOrders;
            }

            // Calculate conversion rate (simplified)
            var totalVisits = overview.TotalCustomers * 5; // Estimate
            if (totalVisits > 0)
            {
                overview.ConversionRate = (decimal)overview.MonthOrders / totalVisits * 100;
            }

            return overview;
        }
        #endregion

        #region Revenue Report
        private async Task<List<RevenueReportDTO>> GetRevenueReport(ReportFilterDTO filter)
        {
            var query = _context.Orders
                .Where(o => o.Status == "Hoàn thành" && 
                           o.OrderDate >= filter.StartDate && 
                           o.OrderDate <= filter.EndDate);

            var revenueData = new List<RevenueReportDTO>();

            switch (filter.Period.ToLower())
            {
                case "day":
                    revenueData = await GetDailyRevenue(query, filter);
                    break;
                case "week":
                    revenueData = await GetWeeklyRevenue(query, filter);
                    break;
                case "month":
                    revenueData = await GetMonthlyRevenue(query, filter);
                    break;
                case "quarter":
                    revenueData = await GetQuarterlyRevenue(query, filter);
                    break;
                case "year":
                    revenueData = await GetYearlyRevenue(query, filter);
                    break;
                default:
                    revenueData = await GetMonthlyRevenue(query, filter);
                    break;
            }

            // Calculate growth rates
            for (int i = 0; i < revenueData.Count; i++)
            {
                if (i > 0)
                {
                    var current = revenueData[i].Revenue;
                    var previous = revenueData[i - 1].Revenue;
                    if (previous > 0)
                    {
                        revenueData[i].GrowthRate = ((current - previous) / previous) * 100;
                    }
                }
            }

            return revenueData.OrderBy(r => r.Date).ToList();
        }

        private async Task<List<RevenueReportDTO>> GetDailyRevenue(IQueryable<Order> query, ReportFilterDTO filter)
        {
            // First get the raw grouped data from database
            var groupedData = await query
                .GroupBy(o => o.OrderDate.Date)
                .Select(g => new 
                {
                    Date = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count(),
                    TotalAmount = g.Sum(o => o.TotalAmount),
                    OrderCountForAvg = g.Count()
                })
                .ToListAsync();

            // Then process on client side
            return groupedData.Select(g => new RevenueReportDTO
            {
                Date = g.Date,
                Period = g.Date.ToString("dd/MM/yyyy"),
                Revenue = g.Revenue,
                OrderCount = g.OrderCount,
                AverageOrderValue = g.OrderCountForAvg > 0 ? g.TotalAmount / g.OrderCountForAvg : 0
            })
            .OrderBy(r => r.Date)
            .ToList();
        }

        private async Task<List<RevenueReportDTO>> GetWeeklyRevenue(IQueryable<Order> query, ReportFilterDTO filter)
        {
            // Get all orders and process on client side
            var orders = await query.ToListAsync();
            
            return orders
                .GroupBy(o => GetWeekOfYear(o.OrderDate))
                .Select(g => new RevenueReportDTO
                {
                    Date = g.First().OrderDate.Date,
                    Period = $"Tuần {g.Key}",
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count(),
                    AverageOrderValue = g.Count() > 0 ? g.Sum(o => o.TotalAmount) / g.Count() : 0
                })
                .OrderBy(r => r.Date)
                .ToList();
        }

        private async Task<List<RevenueReportDTO>> GetMonthlyRevenue(IQueryable<Order> query, ReportFilterDTO filter)
        {
            // First get the raw grouped data from database
            var groupedData = await query
                .GroupBy(o => new { o.OrderDate.Year, o.OrderDate.Month })
                .Select(g => new 
                {
                    Year = g.Key.Year,
                    Month = g.Key.Month,
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count(),
                    TotalAmount = g.Sum(o => o.TotalAmount),
                    OrderCountForAvg = g.Count()
                })
                .ToListAsync();

            // Then process on client side
            return groupedData.Select(g => new RevenueReportDTO
            {
                Date = new DateTime(g.Year, g.Month, 1),
                Period = $"{g.Month:00}/{g.Year}",
                Revenue = g.Revenue,
                OrderCount = g.OrderCount,
                AverageOrderValue = g.OrderCountForAvg > 0 ? g.TotalAmount / g.OrderCountForAvg : 0
            })
            .OrderBy(r => r.Date)
            .ToList();
        }

        private async Task<List<RevenueReportDTO>> GetQuarterlyRevenue(IQueryable<Order> query, ReportFilterDTO filter)
        {
            // Get all orders and process on client side
            var orders = await query.ToListAsync();
            
            return orders
                .GroupBy(o => new { o.OrderDate.Year, Quarter = GetQuarter(o.OrderDate) })
                .Select(g => new RevenueReportDTO
                {
                    Date = new DateTime(g.Key.Year, (g.Key.Quarter - 1) * 3 + 1, 1),
                    Period = $"Q{g.Key.Quarter}/{g.Key.Year}",
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count(),
                    AverageOrderValue = g.Count() > 0 ? g.Sum(o => o.TotalAmount) / g.Count() : 0
                })
                .OrderBy(r => r.Date)
                .ToList();
        }

        private async Task<List<RevenueReportDTO>> GetYearlyRevenue(IQueryable<Order> query, ReportFilterDTO filter)
        {
            // First get the raw grouped data from database
            var groupedData = await query
                .GroupBy(o => o.OrderDate.Year)
                .Select(g => new 
                {
                    Year = g.Key,
                    Revenue = g.Sum(o => o.TotalAmount),
                    OrderCount = g.Count(),
                    TotalAmount = g.Sum(o => o.TotalAmount),
                    OrderCountForAvg = g.Count()
                })
                .ToListAsync();

            // Then process on client side
            return groupedData.Select(g => new RevenueReportDTO
            {
                Date = new DateTime(g.Year, 1, 1),
                Period = g.Year.ToString(),
                Revenue = g.Revenue,
                OrderCount = g.OrderCount,
                AverageOrderValue = g.OrderCountForAvg > 0 ? g.TotalAmount / g.OrderCountForAvg : 0
            })
            .OrderBy(r => r.Date)
            .ToList();
        }
        #endregion

        #region Product Sales Report
        private async Task<List<ProductSalesReportDTO>> GetTopProductsReport(ReportFilterDTO filter)
        {
            var query = from oi in _context.OrderItems
                        join o in _context.Orders on oi.OrderID equals o.OrderID
                        join p in _context.Products on oi.ProductID equals p.ProductID
                        join c in _context.Categories on p.CategoryID equals c.CategoryID
                        join b in _context.Brands on p.BrandID equals b.BrandID
                        where o.OrderDate >= filter.StartDate &&
                              o.OrderDate <= filter.EndDate &&
                              o.Status == "Hoàn thành"
                        select new { oi, o, p, c, b };

            if (filter.CategoryID.HasValue)
            {
                query = query.Where(x => x.p.CategoryID == filter.CategoryID.Value);
            }

            if (filter.BrandID.HasValue)
            {
                query = query.Where(x => x.p.BrandID == filter.BrandID.Value);
            }

            var productSales = await query
                .GroupBy(x => new { x.p.ProductID, ProductName = x.p.Name, CategoryName = x.c.Name, BrandName = x.b.Name, x.p.Price, x.p.ImageURL })
                .Select(g => new ProductSalesReportDTO
                {
                    ProductID = g.Key.ProductID,
                    ProductName = g.Key.ProductName,
                    CategoryName = g.Key.CategoryName,
                    BrandName = g.Key.BrandName,
                    Price = g.Key.Price,
                    ImageURL = g.Key.ImageURL,
                    QuantitySold = g.Sum(x => x.oi.Quantity),
                    Revenue = g.Sum(x => x.oi.Quantity * x.oi.UnitPrice),
                    OrderCount = g.Count()
                })
                .OrderByDescending(p => p.Revenue)
                .Take(filter.PageSize)
                .ToListAsync();

            // Get average ratings
            foreach (var product in productSales)
            {
                var reviews = await _context.Reviews
                    .Where(r => r.ProductID == product.ProductID && r.Status == "Approved")
                    .Select(r => r.Rating)
                    .ToListAsync();

                if (reviews.Any())
                {
                    product.AverageRating = (decimal)reviews.Average()!;
                    product.ReviewCount = reviews.Count;
                }
            }

            return productSales;
        }
        #endregion

        #region Customer Report
        private async Task<List<CustomerReportDTO>> GetTopCustomersReport(ReportFilterDTO filter)
        {
            var customerData = await (from u in _context.Users
                                     join o in _context.Orders on u.UserID equals o.UserID into orders
                                     where u.RoleID == 2 // Customer role
                                     select new CustomerReportDTO
                                     {
                                         UserID = u.UserID,
                                         UserName = u.FullName,
                                         Email = u.Email,
                                         Phone = u.Phone,
                                         JoinDate = u.CreatedAt ?? DateTime.Now,
                                         TotalOrders = orders.Count(o => o.OrderDate >= filter.StartDate && 
                                                                    o.OrderDate <= filter.EndDate),
                                         TotalSpent = orders.Where(o => o.OrderDate >= filter.StartDate && 
                                                                   o.OrderDate <= filter.EndDate && 
                                                                   o.Status == "Hoàn thành")
                                                           .Sum(o => o.TotalAmount),
                                         LastOrderDate = orders.Any() ? orders.Max(o => o.OrderDate) : DateTime.MinValue
                                     })
                                     .Where(c => c.TotalOrders > 0)
                                     .OrderByDescending(c => c.TotalSpent)
                                     .Take(filter.PageSize)
                                     .ToListAsync();

            // Calculate additional metrics
            foreach (var customer in customerData)
            {
                if (customer.TotalOrders > 0)
                {
                    customer.AverageOrderValue = customer.TotalSpent / customer.TotalOrders;
                }

                customer.DaysSinceLastOrder = (DateTime.Now - customer.LastOrderDate).Days;

                // Determine customer type
                if (customer.TotalSpent >= 5000000) // 5M VND
                {
                    customer.CustomerType = "VIP";
                }
                else if (customer.TotalSpent >= 1000000) // 1M VND
                {
                    customer.CustomerType = "Regular";
                }
                else
                {
                    customer.CustomerType = "New";
                }

                // Determine status
                if (customer.DaysSinceLastOrder <= 30)
                {
                    customer.Status = "Active";
                }
                else if (customer.DaysSinceLastOrder <= 90)
                {
                    customer.Status = "Potential";
                }
                else
                {
                    customer.Status = "Inactive";
                }
            }

            return customerData;
        }
        #endregion

        #region Payment Method Report
        private async Task<List<PaymentMethodReportDTO>> GetPaymentMethodReport(ReportFilterDTO filter)
        {
            // Get all orders in date range
            var orders = await _context.Orders
                .Where(o => o.OrderDate >= filter.StartDate && o.OrderDate <= filter.EndDate)
                .ToListAsync();

            // Group by payment method (extracted from Note field or status)
            var paymentGroups = orders
                .GroupBy(o => GetPaymentMethod(o))
                .Select(g => new PaymentMethodReportDTO
                {
                    PaymentMethod = g.Key,
                    TotalTransactions = g.Count(),
                    SuccessfulTransactions = g.Count(o => o.Status == "Hoàn thành"),
                    FailedTransactions = g.Count(o => o.Status == "Đã hủy" || o.Status == "Thất bại"),
                    TotalAmount = g.Where(o => o.Status == "Hoàn thành").Sum(o => o.TotalAmount),
                    LastTransaction = g.Max(o => o.OrderDate)
                })
                .ToList();

            // Calculate success rates and average amounts
            foreach (var payment in paymentGroups)
            {
                if (payment.TotalTransactions > 0)
                {
                    payment.SuccessRate = (decimal)payment.SuccessfulTransactions / payment.TotalTransactions * 100;
                }

                if (payment.SuccessfulTransactions > 0)
                {
                    payment.AverageAmount = payment.TotalAmount / payment.SuccessfulTransactions;
                }
            }

            return paymentGroups.OrderByDescending(p => p.TotalAmount).ToList();
        }

        private string GetPaymentMethod(Order order)
        {
            if (string.IsNullOrEmpty(order.Note))
                return "COD";

            var note = order.Note.ToLower();
            if (note.Contains("momo"))
                return "MoMo";
            else if (note.Contains("vnpay") || note.Contains("vn pay"))
                return "VNPay";
            else
                return "COD";
        }
        #endregion

        #region Chart Data Preparation
        private ChartDataDTO PrepareRevenueChartData(List<RevenueReportDTO> revenueData)
        {
            return new ChartDataDTO
            {
                Labels = revenueData.Select(r => r.Period).ToList(),
                Data = revenueData.Select(r => r.Revenue).ToList(),
                Label = "Doanh thu",
                Color = "#5E72E4",
                Type = "line"
            };
        }

        private ChartDataDTO PrepareProductChartData(List<ProductSalesReportDTO> productData)
        {
            return new ChartDataDTO
            {
                Labels = productData.Take(10).Select(p => p.ProductName.Length > 20 ? 
                    p.ProductName.Substring(0, 20) + "..." : p.ProductName).ToList(),
                Data = productData.Take(10).Select(p => p.Revenue).ToList(),
                Label = "Doanh thu sản phẩm",
                Color = "#2dce89",
                Type = "bar"
            };
        }

        private ChartDataDTO PreparePaymentChartData(List<PaymentMethodReportDTO> paymentData)
        {
            return new ChartDataDTO
            {
                Labels = paymentData.Select(p => p.PaymentMethod).ToList(),
                Data = paymentData.Select(p => p.TotalAmount).ToList(),
                Label = "Doanh thu theo phương thức",
                Color = "#fb6340",
                Type = "doughnut"
            };
        }
        #endregion

        #region AJAX Endpoints
        [HttpGet]
        public async Task<JsonResult> GetRevenueData(ReportFilterDTO filter)
        {
            var data = await GetRevenueReport(filter);
            var chartData = PrepareRevenueChartData(data);
            return Json(new { success = true, data = chartData, details = data });
        }

        [HttpGet]
        public async Task<JsonResult> GetProductData(ReportFilterDTO filter)
        {
            var data = await GetTopProductsReport(filter);
            var chartData = PrepareProductChartData(data);
            return Json(new { success = true, data = chartData, details = data });
        }

        [HttpGet]
        public async Task<JsonResult> GetCustomerData(ReportFilterDTO filter)
        {
            var data = await GetTopCustomersReport(filter);
            return Json(new { success = true, data = data });
        }

        [HttpGet]
        public async Task<JsonResult> GetPaymentData(ReportFilterDTO filter)
        {
            var data = await GetPaymentMethodReport(filter);
            var chartData = PreparePaymentChartData(data);
            return Json(new { success = true, data = chartData, details = data });
        }

        [HttpGet]
        public async Task<JsonResult> GetOverviewData()
        {
            var data = await GetDashboardOverview();
            return Json(new { success = true, data = data });
        }
        #endregion

        #region Helper Methods
        private int GetWeekOfYear(DateTime date)
        {
            var calendar = CultureInfo.CurrentCulture.Calendar;
            return calendar.GetWeekOfYear(date, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
        }

        private int GetQuarter(DateTime date)
        {
            return (date.Month - 1) / 3 + 1;
        }
        #endregion
    }
}