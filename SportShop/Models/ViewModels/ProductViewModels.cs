using SportShop.Models;
using SportShop.Models.DTOs;
using System.Collections.Generic;
using System.Linq;

namespace SportShop.Models.ViewModels
{
    // ViewModel cho trang danh sách sản phẩm
    public class ProductIndexViewModel
    {
        public List<Product> Products { get; set; } = new List<Product>();
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<Brand> Brands { get; set; } = new List<Brand>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string SortOrder { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
        public string Keyword { get; set; }
        
        // Thêm dictionary lưu dữ liệu đánh giá
        public Dictionary<int, ProductRatingDTO> ProductRatings { get; set; } = new Dictionary<int, ProductRatingDTO>();
        
        // Helper method để lấy rating
        public double GetProductRating(int productId)
        {
            if (ProductRatings != null && ProductRatings.TryGetValue(productId, out var rating))
            {
                return rating.AverageRating;
            }
            return 0;
        }
        
        // Helper method để lấy số lượng đánh giá
        public int GetProductReviewCount(int productId)
        {
            if (ProductRatings != null && ProductRatings.TryGetValue(productId, out var rating))
            {
                return rating.ReviewCount;
            }
            return 0;
        }
    }
    
    // ViewModel cho trang chi tiết sản phẩm
    public class ProductDetailViewModel
    {
        public Product Product { get; set; }
        public List<ProductAttribute> Attributes { get; set; } = new List<ProductAttribute>();
        public List<Review> Reviews { get; set; } = new List<Review>();
        public List<Product> RelatedProducts { get; set; } = new List<Product>();
        
        // Các thuộc tính bổ sung
        public double AverageRating 
        { 
            get 
            {
                if (Reviews == null || !Reviews.Any())
                    return 0;
                
                var validReviews = Reviews.Where(r => r.Rating.HasValue && r.Status == "Approved").ToList();
                if (!validReviews.Any())
                    return 0;
                
                // Làm tròn đến 1 chữ số thập phân để nhất quán với trang Index
                return Math.Round(validReviews.Average(r => r.Rating.Value), 1);
            }
        }
        
        public int TotalReviews => Reviews?.Count(r => r.Status == "Approved") ?? 0;
        
        // Danh sách màu sắc và kích thước có sẵn
        public List<string> AvailableColors => 
            Attributes?
                .Where(a => !string.IsNullOrEmpty(a.Color))
                .Select(a => a.Color)
                .Distinct()
                .ToList() ?? new List<string>();
                
        public List<string> AvailableSizes => 
            Attributes?
                .Where(a => !string.IsNullOrEmpty(a.Size))
                .Select(a => a.Size)
                .Distinct()
                .ToList() ?? new List<string>();
    }
}