using System;

namespace SportShop.Models.ViewModels
{
    public class TestimonialViewModel
    {
        public int ReviewID { get; set; }
        public int Rating { get; set; }
        public string Comment { get; set; }
        public string UserFullName { get; set; }
        public string ProductName { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Occupation { get; set; } // Lưu thông tin nghề nghiệp (nếu có)
    }
}