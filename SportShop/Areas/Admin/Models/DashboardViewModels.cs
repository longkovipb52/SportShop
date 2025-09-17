namespace SportShop.Areas.Admin.Models
{
    public class DashboardOverviewViewModel
    {
        // Revenue Statistics
        public decimal TodayRevenue { get; set; }
        public decimal WeekRevenue { get; set; }
        public decimal MonthRevenue { get; set; }
        public decimal YearRevenue { get; set; }
        
        // Order Statistics
        public int TodayOrders { get; set; }
        public int ProcessingOrders { get; set; }
        public int CompletedOrders { get; set; }
        public int TotalOrders { get; set; }
        
        // Customer Statistics
        public int NewCustomersToday { get; set; }
        public int NewCustomersThisWeek { get; set; }
        public int NewCustomersThisMonth { get; set; }
        public int TotalCustomers { get; set; }
        
        // Product Statistics
        public List<TopSellingProduct> TopSellingProducts { get; set; } = new List<TopSellingProduct>();
        public List<TopCategory> TopCategories { get; set; } = new List<TopCategory>();
        
        // Revenue Chart Data
        public List<RevenueChartData> RevenueChartData { get; set; } = new List<RevenueChartData>();
        
        // Notifications
        public List<AdminNotification> Notifications { get; set; } = new List<AdminNotification>();
        
        // Growth Percentages
        public decimal RevenueGrowthPercent { get; set; }
        public decimal OrderGrowthPercent { get; set; }
        public decimal CustomerGrowthPercent { get; set; }
    }

    public class TopSellingProduct
    {
        public int ProductID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageURL { get; set; } = string.Empty;
        public int TotalSold { get; set; }
        public decimal Revenue { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public string BrandName { get; set; } = string.Empty;
    }

    public class TopCategory
    {
        public int CategoryID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ImageURL { get; set; } = string.Empty;
        public int TotalProducts { get; set; }
        public int TotalSold { get; set; }
        public decimal Revenue { get; set; }
        public decimal Percentage { get; set; }
    }

    public class RevenueChartData
    {
        public string Label { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public DateTime Date { get; set; }
    }

    public class AdminNotification
    {
        public string Icon { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty; // success, warning, error, info
        public DateTime CreatedAt { get; set; }
        public string Url { get; set; } = string.Empty;
    }
}