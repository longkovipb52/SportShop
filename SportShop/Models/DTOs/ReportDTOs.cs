using System.ComponentModel.DataAnnotations;

namespace SportShop.Models.DTOs
{
    // DTO cho báo cáo doanh thu
    public class RevenueReportDTO
    {
        public string Period { get; set; } = string.Empty;
        public decimal Revenue { get; set; }
        public int OrderCount { get; set; }
        public decimal AverageOrderValue { get; set; }
        public DateTime Date { get; set; }
        public decimal PreviousPeriodRevenue { get; set; }
        public decimal GrowthRate { get; set; }
    }

    // DTO cho báo cáo sản phẩm bán chạy
    public class ProductSalesReportDTO
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string CategoryName { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
        public int QuantitySold { get; set; }
        public decimal Revenue { get; set; }
        public decimal Price { get; set; }
        public string ImageURL { get; set; } = string.Empty;
        public int OrderCount { get; set; }
        public decimal AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }

    // DTO cho báo cáo khách hàng tiềm năng
    public class CustomerReportDTO
    {
        public int UserID { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal AverageOrderValue { get; set; }
        public DateTime LastOrderDate { get; set; }
        public DateTime JoinDate { get; set; }
        public string CustomerType { get; set; } = string.Empty; // VIP, Regular, New
        public int DaysSinceLastOrder { get; set; }
        public string Status { get; set; } = string.Empty; // Active, Inactive, Potential
    }

    // DTO cho báo cáo phương thức thanh toán
    public class PaymentMethodReportDTO
    {
        public string PaymentMethod { get; set; } = string.Empty;
        public int TotalTransactions { get; set; }
        public int SuccessfulTransactions { get; set; }
        public int FailedTransactions { get; set; }
        public decimal SuccessRate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal AverageAmount { get; set; }
        public DateTime LastTransaction { get; set; }
    }

    // DTO cho báo cáo tổng quan
    public class DashboardOverviewDTO
    {
        public decimal TodayRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public decimal YearRevenue { get; set; }
        public int TodayOrders { get; set; }
        public int MonthOrders { get; set; }
        public int YearOrders { get; set; }
        public int TotalCustomers { get; set; }
        public int NewCustomersThisMonth { get; set; }
        public int TotalProducts { get; set; }
        public int LowStockProducts { get; set; }
        public decimal AverageOrderValue { get; set; }
        public decimal ConversionRate { get; set; }
    }

    // DTO cho filter báo cáo
    public class ReportFilterDTO
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public string Period { get; set; } = "month"; // day, week, month, quarter, year
        public string ReportType { get; set; } = "revenue";
        public int? CategoryID { get; set; }
        public int? BrandID { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public int PageSize { get; set; } = 10;
        public int PageNumber { get; set; } = 1;
    }

    // DTO cho chart data
    public class ChartDataDTO
    {
        public List<string> Labels { get; set; } = new List<string>();
        public List<decimal> Data { get; set; } = new List<decimal>();
        public string Label { get; set; } = string.Empty;
        public string Color { get; set; } = string.Empty;
        public string Type { get; set; } = "line"; // line, bar, pie, doughnut
    }

    // ViewModel cho Reports Index
    public class ReportsIndexViewModel
    {
        public DashboardOverviewDTO Overview { get; set; } = new DashboardOverviewDTO();
        public List<RevenueReportDTO> RevenueData { get; set; } = new List<RevenueReportDTO>();
        public List<ProductSalesReportDTO> TopProducts { get; set; } = new List<ProductSalesReportDTO>();
        public List<CustomerReportDTO> TopCustomers { get; set; } = new List<CustomerReportDTO>();
        public List<PaymentMethodReportDTO> PaymentMethods { get; set; } = new List<PaymentMethodReportDTO>();
        public ReportFilterDTO Filter { get; set; } = new ReportFilterDTO();
        
        // Chart data
        public ChartDataDTO RevenueChartData { get; set; } = new ChartDataDTO();
        public ChartDataDTO ProductChartData { get; set; } = new ChartDataDTO();
        public ChartDataDTO PaymentChartData { get; set; } = new ChartDataDTO();
        
        // Dropdowns
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<Brand> Brands { get; set; } = new List<Brand>();
    }
}