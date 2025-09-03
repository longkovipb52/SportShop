using System.ComponentModel.DataAnnotations;

namespace SportShop.Models.ViewModels
{
    public class CheckoutViewModel
    {
        public List<CartViewModel> CartItems { get; set; } = new List<CartViewModel>();
        public decimal Subtotal { get; set; }
        public decimal ShippingFee { get; set; }
        public decimal Tax { get; set; }
        public decimal Total { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        [Display(Name = "Họ và tên")]
        public string? ShippingName { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        [Display(Name = "Số điện thoại")]
        public string? ShippingPhone { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ")]
        [Display(Name = "Địa chỉ giao hàng")]
        public string? ShippingAddress { get; set; }
        
        [Display(Name = "Ghi chú")]
        public string? Note { get; set; }
        
        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        [Display(Name = "Phương thức thanh toán")]
        public string? PaymentMethod { get; set; }
        
        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }
        
        // Thông tin thẻ tín dụng (nếu chọn thanh toán online)
        [Display(Name = "Số thẻ")]
        public string? CardNumber { get; set; }
        
        [Display(Name = "Tên chủ thẻ")]
        public string? CardHolderName { get; set; }
        
        [Display(Name = "Tháng/Năm hết hạn")]
        public string? ExpiryDate { get; set; }
        
        [Display(Name = "CVV")]
        public string? CVV { get; set; }
    }

    public class CartViewModel
    {
        public int CartId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = "";
        public string ImageUrl { get; set; } = "";
        public decimal Price { get; set; }
        public int Quantity { get; set; }
        public int? AttributeId { get; set; }
        public string Color { get; set; } = "";
        public string Size { get; set; } = "";
        public decimal TotalPrice { get; set; }
    }
}
