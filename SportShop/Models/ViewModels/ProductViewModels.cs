using SportShop.Models;
using SportShop.Models.DTOs;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace SportShop.Models.ViewModels
{
    // ViewModel cho trang danh sách sản phẩm (Admin)
    public class ProductListViewModel
    {
        public List<Product> Products { get; set; } = new List<Product>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public int TotalItems { get; set; }
        public int PageSize { get; set; }
        public string? SearchString { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? StockStatus { get; set; }
    }

    // ViewModel cho thêm sản phẩm mới (Admin)
    public class ProductCreateViewModel
    {
        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        [StringLength(100, ErrorMessage = "Tên sản phẩm không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá sản phẩm")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng tồn kho")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho không được âm")]
        public int Stock { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        public int CategoryID { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thương hiệu")]
        public int BrandID { get; set; }

        public IFormFile? ImageFile { get; set; }
    }

    // ViewModel cho chỉnh sửa sản phẩm (Admin)
    public class ProductEditViewModel
    {
        public int ProductID { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập tên sản phẩm")]
        [StringLength(100, ErrorMessage = "Tên sản phẩm không được vượt quá 100 ký tự")]
        public string Name { get; set; } = string.Empty;

        [StringLength(1000, ErrorMessage = "Mô tả không được vượt quá 1000 ký tự")]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập giá sản phẩm")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá sản phẩm phải lớn hơn 0")]
        public decimal Price { get; set; }

        [Required(ErrorMessage = "Vui lòng nhập số lượng tồn kho")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho không được âm")]
        public int Stock { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn danh mục")]
        public int CategoryID { get; set; }
        
        [Display(Name = "Danh mục con")]
        public int? SubCategoryID { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn thương hiệu")]
        public int BrandID { get; set; }

        public string? CurrentImageURL { get; set; }
        public IFormFile? ImageFile { get; set; }
        
        // Danh sách thuộc tính của sản phẩm
        public List<ProductAttribute> Attributes { get; set; } = new List<ProductAttribute>();
    }

    // ViewModel cho thêm/sửa thuộc tính sản phẩm
    public class ProductAttributeViewModel
    {
        public int AttributeID { get; set; }
        
        [Required(ErrorMessage = "Vui lòng chọn sản phẩm")]
        public int ProductID { get; set; }
        
        // Size: Có thể null nếu chọn từ SizeOption, hoặc nhập tự do
        [StringLength(50, ErrorMessage = "Kích thước không được vượt quá 50 ký tự")]
        public string? Size { get; set; }
        
        // Color: Có thể null nếu chọn từ ColorOption, hoặc nhập tự do
        [StringLength(50, ErrorMessage = "Màu sắc không được vượt quá 50 ký tự")]
        public string? Color { get; set; }
        
        [Required(ErrorMessage = "Vui lòng nhập số lượng tồn kho")]
        [Range(0, int.MaxValue, ErrorMessage = "Số lượng tồn kho không được âm")]
        public int Stock { get; set; }
        
        [Range(0.01, double.MaxValue, ErrorMessage = "Giá phải lớn hơn 0")]
        public decimal? Price { get; set; }
        
        // Foreign keys to master data - Ưu tiên sử dụng nếu có
        // Nếu chọn từ dropdown, sẽ tự động điền Size/Color từ master data
        public int? SizeOptionID { get; set; }
        public int? ColorOptionID { get; set; }
        
        public IFormFile? ImageFile { get; set; }
        public string? CurrentImageURL { get; set; }
    }

    // ViewModel cho trang danh sách sản phẩm (Public)
    public class ProductIndexViewModel
    {
        public List<Product> Products { get; set; } = new List<Product>();
        public List<Category> Categories { get; set; } = new List<Category>();
        public List<Brand> Brands { get; set; } = new List<Brand>();
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public string? SortOrder { get; set; }
        public int? CategoryId { get; set; }
        public int? BrandId { get; set; }
        public string? Keyword { get; set; }
        
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