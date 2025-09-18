using SportShop.Models;

namespace SportShop.Areas.Admin.Models
{
    public class CustomerViewModel
    {
        public int UserID { get; set; }
        public string Username { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public int TotalOrders { get; set; }
        public decimal TotalSpent { get; set; }
    }

    public class CustomerListViewModel
    {
        public List<CustomerViewModel> Customers { get; set; } = new List<CustomerViewModel>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public string Search { get; set; } = "";
        public int TotalCustomers { get; set; }
    }

    public class CustomerDetailViewModel : CustomerViewModel
    {
        public List<OrderSummaryViewModel> RecentOrders { get; set; } = new List<OrderSummaryViewModel>();
    }

    public class PurchaseHistoryViewModel
    {
        public CustomerViewModel Customer { get; set; } = new CustomerViewModel();
        public List<Order> Orders { get; set; } = new List<Order>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int PageSize { get; set; }
        public int TotalOrders { get; set; }
    }

    public class OrderSummaryViewModel
    {
        public int OrderID { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusDisplay { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
        public string ShippingName { get; set; } = string.Empty;
        public int ItemCount { get; set; }
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public List<OrderItemSummaryViewModel> Items { get; set; } = new List<OrderItemSummaryViewModel>();
    }

    public class OrderItemSummaryViewModel
    {
        public string ProductName { get; set; } = string.Empty;
        public string ProductImage { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public string? Size { get; set; }
        public string? Color { get; set; }
    }
}