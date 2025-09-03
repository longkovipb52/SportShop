using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Models.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SportShop.Controllers
{
    public class AboutController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AboutController(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            List<TestimonialViewModel> testimonials = new List<TestimonialViewModel>();

            try
            {
                // Lấy các đánh giá có rating cao (4-5 sao) và được phê duyệt
                testimonials = await _context.Reviews
                    .Include(r => r.User)
                    .Include(r => r.Product)
                    .Where(r => r.Rating.HasValue && r.Rating.Value >= 4 && r.Status == "Approved")
                    .OrderByDescending(r => r.Rating)
                    .ThenByDescending(r => r.CreatedAt)
                    .Take(6) // Lấy 6 đánh giá tốt nhất để hiển thị 2 mỗi slide
                    .Select(r => new TestimonialViewModel
                    {
                        ReviewID = r.ReviewID,
                        Rating = r.Rating ?? 5, // Sử dụng coalescing operator ?? để cung cấp giá trị mặc định
                        Comment = r.Comment ?? "Sản phẩm rất tuyệt vời!",
                        UserFullName = r.User.FullName,
                        ProductName = r.Product.Name,
                        CreatedAt = r.CreatedAt ?? DateTime.Now, // Sử dụng giá trị mặc định là DateTime.Now
                        Occupation = "Khách hàng" // Mặc định vì DB không lưu thông tin này
                    })
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                // Log lỗi nếu có
                Console.WriteLine($"Error loading testimonials: {ex.Message}");
                
                // Tạo dữ liệu mẫu trong trường hợp lỗi
                testimonials = GetSampleTestimonials();
            }

            ViewBag.Testimonials = testimonials;
            return View();
        }

        private List<TestimonialViewModel> GetSampleTestimonials()
        {
            // Tạo danh sách đánh giá mẫu trong trường hợp không có dữ liệu thực hoặc xảy ra lỗi
            return new List<TestimonialViewModel>
            {
                new TestimonialViewModel
                {
                    ReviewID = 1,
                    Rating = 5,
                    Comment = "LoLoSport là điểm đến lý tưởng cho những người yêu thích thể thao như tôi. Sản phẩm chính hãng, nhân viên tư vấn nhiệt tình và chuyên nghiệp. Tôi đã mua giày chạy bộ Nike và rất hài lòng với chất lượng.",
                    UserFullName = "Hoàng Minh Tuấn",
                    ProductName = "Giày chạy bộ Nike Revolution 6",
                    CreatedAt = DateTime.Now.AddDays(-10),
                    Occupation = "Huấn luyện viên thể hình"
                },
                new TestimonialViewModel
                {
                    ReviewID = 2,
                    Rating = 5,
                    Comment = "Tôi thực sự ấn tượng với dịch vụ giao hàng nhanh chóng và chất lượng sản phẩm từ LoLoSport. Quần áo thể thao Adidas mà tôi mua có chất lượng tuyệt vời và giá cả phải chăng.",
                    UserFullName = "Nguyễn Thị Mai",
                    ProductName = "Áo thun Adidas Originals",
                    CreatedAt = DateTime.Now.AddDays(-15),
                    Occupation = "Giáo viên yoga"
                },
                new TestimonialViewModel
                {
                    ReviewID = 3,
                    Rating = 4,
                    Comment = "Là một vận động viên chuyên nghiệp, tôi rất khó tính trong việc lựa chọn trang phục và dụng cụ tập luyện. LoLoSport đã đáp ứng được yêu cầu của tôi với các sản phẩm chính hãng và chất lượng cao.",
                    UserFullName = "Trần Quang Huy",
                    ProductName = "Giày bóng rổ Under Armour",
                    CreatedAt = DateTime.Now.AddDays(-5),
                    Occupation = "Vận động viên bóng rổ"
                }
            };
        }
    }
}