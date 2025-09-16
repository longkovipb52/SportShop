using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace SportShop.Models.ViewModels
{
    public class OrderHistoryViewModel
    {
        public List<OrderSummaryViewModel> Orders { get; set; } = new List<OrderSummaryViewModel>();
        public int TotalOrders { get; set; }
        public decimal TotalAmount { get; set; }
        public string? StatusFilter { get; set; }
        public string? SearchTerm { get; set; }
        public int CurrentPage { get; set; } = 1;
        public int TotalPages { get; set; }
        public int PageSize { get; set; } = 10;
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

    public class OrderDetailViewModel
    {
        public int OrderID { get; set; }
        public DateTime OrderDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string StatusDisplay { get; set; } = string.Empty;
        public string StatusClass { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;

        // Shipping Information
        public string ShippingName { get; set; } = string.Empty;
        public string ShippingAddress { get; set; } = string.Empty;
        public string ShippingPhone { get; set; } = string.Empty;

        // Payment Information
        public string PaymentMethod { get; set; } = string.Empty;
        public string PaymentStatus { get; set; } = string.Empty;
        public DateTime? PaymentDate { get; set; }

        // Order Items
        public List<OrderItemDetailViewModel> Items { get; set; } = new List<OrderItemDetailViewModel>();

        // Order Timeline
        public List<OrderTimelineViewModel> Timeline { get; set; } = new List<OrderTimelineViewModel>();
    }

    public class OrderItemDetailViewModel
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public string ProductImage { get; set; } = string.Empty;
        public string ProductDescription { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice => Quantity * UnitPrice;
        public string? Size { get; set; }
        public string? Color { get; set; }
        public string? BrandName { get; set; }
    }

    public class OrderTimelineViewModel
    {
        public DateTime Date { get; set; }
        public string Status { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public bool IsCompleted { get; set; }
        public bool IsCurrent { get; set; }
    }

    public class OrderStatusStatisticsViewModel
    {
        public int PendingCount { get; set; }
        public int ProcessingCount { get; set; }
        public int ShippingCount { get; set; }
        public int CompletedCount { get; set; }
        public int CancelledCount { get; set; }
        public decimal TotalSpent { get; set; }
    }
}