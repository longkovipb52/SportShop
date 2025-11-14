using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Models;
using SportShop.Models.ViewModels;
using SportShop.Models.DTOs; // Thêm namespace này
using SportShop.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SportShop.Controllers
{
    public class ProductController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly InteractionTrackingService _trackingService;

        public ProductController(ApplicationDbContext context, InteractionTrackingService trackingService)
        {
            _context = context;
            _trackingService = trackingService;
        }

        // Trang danh sách sản phẩm
        public async Task<IActionResult> Index(int? categoryId, int? brandId, string sortOrder, int? rating, int? minPrice, int? maxPrice, string keyword, int page = 1)
        {
            int pageSize = 12;

            // Truy vấn sản phẩm cơ bản
            var productsQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .AsQueryable();

            // Lọc theo danh mục
            if (categoryId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.CategoryID == categoryId);
                ViewData["CurrentCategory"] = categoryId;
                
                // Track category filter
                var category = await _context.Categories.FindAsync(categoryId.Value);
                if (category != null)
                {
                    try
                    {
                        await _trackingService.TrackCategoryFilterAsync(categoryId.Value, category.Name);
                    }
                    catch (Exception)
                    {
                        // Tracking failure should not affect the main flow
                    }
                }
            }

            // Lọc theo thương hiệu
            if (brandId.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.BrandID == brandId);
                ViewData["CurrentBrand"] = brandId;
                
                // Track brand filter
                var brand = await _context.Brands.FindAsync(brandId.Value);
                if (brand != null)
                {
                    try
                    {
                        await _trackingService.TrackBrandFilterAsync(brandId.Value, brand.Name);
                    }
                    catch (Exception)
                    {
                        // Tracking failure should not affect the main flow
                    }
                }
            }

            // Thêm bộ lọc giá
            if (minPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Price >= minPrice.Value);
                ViewData["MinPrice"] = minPrice.Value;
            }

            if (maxPrice.HasValue)
            {
                productsQuery = productsQuery.Where(p => p.Price <= maxPrice.Value);
                ViewData["MaxPrice"] = maxPrice.Value;
            }

            // Lọc theo rating nếu có - Đã sửa lại để tránh lỗi
            if (rating.HasValue)
            {
                // Lấy danh sách ID sản phẩm có rating cao hơn giá trị cần lọc
                var productIdsWithHighRating = await _context.Reviews
                    .Where(r => r.Rating >= rating.Value && r.Status == "Approved")
                    .Select(r => r.ProductID)
                    .Distinct()
                    .ToListAsync();

                // Lọc sản phẩm theo danh sách ID
                productsQuery = productsQuery.Where(p => productIdsWithHighRating.Contains(p.ProductID));
                ViewData["CurrentRating"] = rating.Value;
            }

            // Tìm kiếm theo từ khóa
            if (!string.IsNullOrEmpty(keyword))
            {
                productsQuery = productsQuery.Where(p => p.Name.Contains(keyword) || p.Description.Contains(keyword));
                ViewData["CurrentKeyword"] = keyword;
            }

            // Sắp xếp sản phẩm
            ViewData["PriceSortParam"] = string.IsNullOrEmpty(sortOrder) ? "price_desc" : "";
            ViewData["NameSortParam"] = sortOrder == "name" ? "name_desc" : "name";

            switch (sortOrder)
            {
                case "price_desc":
                    productsQuery = productsQuery.OrderByDescending(p => p.Price);
                    break;
                case "name":
                    productsQuery = productsQuery.OrderBy(p => p.Name);
                    break;
                case "name_desc":
                    productsQuery = productsQuery.OrderByDescending(p => p.Name);
                    break;
                default:
                    productsQuery = productsQuery.OrderBy(p => p.Price);
                    break;
            }

            // Phân trang
            var totalItems = await productsQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            // Điều chỉnh trang hiện tại nếu vượt quá
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var products = await productsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Lấy đánh giá trung bình và số đánh giá cho các sản phẩm
            var productIds = products.Select(p => p.ProductID).ToList();
            var ratings = await GetProductRatingsAsync(productIds);

            // Debug log để kiểm tra
            Console.WriteLine($"🔍 Products count: {products.Count}");
            Console.WriteLine($"🔍 Ratings count: {ratings.Count}");
            foreach(var ratingItem in ratings)
            {
                Console.WriteLine($"🔍 ProductID {ratingItem.Key}: Rating={ratingItem.Value.AverageRating:F1}, Reviews={ratingItem.Value.ReviewCount}");
            }

            // Lấy danh mục và thương hiệu cho filter
            var categories = await _context.Categories.ToListAsync();
            var brands = await _context.Brands.ToListAsync();

            // Tạo view model
            var viewModel = new ProductIndexViewModel
            {
                Products = products,
                Categories = categories,
                Brands = brands,
                CurrentPage = page,
                TotalPages = totalPages,
                SortOrder = sortOrder,
                CategoryId = categoryId,
                BrandId = brandId,
                Keyword = keyword,
                ProductRatings = ratings // ✅ Gán ProductRatings dictionary đúng cách
            };

            return View(viewModel);
        }



        // Phương thức mới lấy dữ liệu đánh giá
        private async Task<Dictionary<int, ProductRatingDTO>> GetProductRatingsAsync(List<int> productIds)
        {
            try
            {
                var ratings = await _context.Reviews
                .Where(r => productIds.Contains(r.ProductID) && r.Status == "Approved" && r.Rating.HasValue)
                .GroupBy(r => r.ProductID)
                .Select(g => new ProductRatingDTO
                {
                    ProductID = g.Key,
                    AverageRating = g.Average(r => r.Rating.Value),
                    ReviewCount = g.Count()
                })
                .ToDictionaryAsync(r => r.ProductID);

                return ratings;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in GetProductRatingsAsync: {ex.Message}");
                return new Dictionary<int, ProductRatingDTO>();
            }

        }

        // Chi tiết sản phẩm
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                // Lấy sản phẩm chính
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Attributes)
                    .FirstOrDefaultAsync(p => p.ProductID == id);

                if (product == null)
                {
                    return NotFound();
                }

                // No VIEW_PRODUCT tracking - removed per user request
                // Since QUICK_VIEW provides better interaction data

                // Lấy thuộc tính sản phẩm (kích cỡ, màu sắc)
                var attributes = product.Attributes.ToList();

                // Lấy đánh giá sản phẩm
                var reviews = await _context.Reviews
                    .Where(r => r.ProductID == id && r.Status == "Approved")
                    .Include(r => r.User)
                    .OrderByDescending(r => r.CreatedAt)
                    .ToListAsync();

                // Lấy sản phẩm liên quan (cùng danh mục)
                var relatedProducts = await _context.Products
                    .Where(p => p.CategoryID == product.CategoryID && p.ProductID != id)
                    .Take(4)
                    .ToListAsync();

                // Tạo view model
                var viewModel = new ProductDetailViewModel
                {
                    Product = product,
                    Attributes = attributes,
                    Reviews = reviews,
                    RelatedProducts = relatedProducts
                };

                return View(viewModel);
            }
            catch (Exception ex)
            {
                // Log lỗi
                Console.WriteLine($"Error in Details: {ex.Message}");
                
                // Redirect về trang sản phẩm với thông báo lỗi
                TempData["ErrorMessage"] = "Không thể tải thông tin sản phẩm. Vui lòng thử lại sau.";
                return RedirectToAction("Index");
            }
        }

        // Tìm kiếm sản phẩm
        public async Task<IActionResult> Search(string keyword, int page = 1)
        {
            int pageSize = 12;

            if (string.IsNullOrEmpty(keyword))
            {
                return RedirectToAction(nameof(Index));
            }

            ViewData["CurrentKeyword"] = keyword;

            var productsQuery = _context.Products
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Where(p => p.Name.Contains(keyword) ||
                           p.Description.Contains(keyword) ||
                           p.Category.Name.Contains(keyword) ||
                           (p.Brand != null && p.Brand.Name.Contains(keyword)))
                .OrderBy(p => p.Name);

            // Phân trang
            var totalItems = await productsQuery.CountAsync();
            
            // Track search event
            try
            {
                await _trackingService.TrackSearchAsync(keyword, totalItems);
            }
            catch (Exception)
            {
                // Tracking failure should not affect the main flow
            }
            var totalPages = (int)Math.Ceiling(totalItems / (double)pageSize);

            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var products = await productsQuery
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            // Lấy đánh giá cho sản phẩm trong tìm kiếm
            var productIds = products.Select(p => p.ProductID).ToList();
            var ratings = await GetProductRatingsAsync(productIds);

            // Lấy danh mục và thương hiệu cho filter
            var categories = await _context.Categories.ToListAsync();
            var brands = await _context.Brands.ToListAsync();

            // Tạo view model
            var viewModel = new ProductIndexViewModel
            {
                Products = products,
                Categories = categories,
                Brands = brands,
                CurrentPage = page,
                TotalPages = totalPages,
                Keyword = keyword,
                ProductRatings = ratings
            };

            ViewData["Title"] = $"Kết quả tìm kiếm: {keyword}";

            return View("Index", viewModel);
        }
        
        // Trả về thông tin sản phẩm dạng JSON cho quick view
        [HttpGet]
        public async Task<IActionResult> GetProductJson(int id)
        {
            try
            {
                // Track quick view
                try
                {
                    await _trackingService.TrackQuickViewAsync(id);
                }
                catch (Exception)
                {
                    // Tracking failure should not affect the main flow
                }
                
                // Lấy thông tin sản phẩm
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Include(p => p.Attributes)
                    .FirstOrDefaultAsync(p => p.ProductID == id);

                if (product == null)
                {
                    return NotFound();
                }

                // Lấy đánh giá trung bình
                var reviewData = await _context.Reviews
                    .Where(r => r.ProductID == id && r.Status == "Approved" && r.Rating.HasValue)
                    .GroupBy(r => r.ProductID)
                    .Select(g => new 
                    {
                        AverageRating = g.Average(r => r.Rating.Value),
                        ReviewCount = g.Count()
                    })
                    .FirstOrDefaultAsync();

                // Xử lý attributes riêng để tránh lỗi
                var attributesList = new List<object>();
                if (product.Attributes != null)
                {
                    foreach (var attr in product.Attributes)
                    {
                        attributesList.Add(new
                        {
                            attributeID = attr.AttributeID,
                            color = attr.Color ?? "",
                            size = attr.Size ?? "",
                            stock = attr.Stock,
                            price = attr.Price,
                            imageURL = attr.ImageURL ?? ""
                        });
                    }
                }

                // Chuyển đổi sang đối tượng ẩn danh với thông tin cần thiết
                var productData = new
                {
                    productID = product.ProductID,
                    name = product.Name,
                    price = product.Price,
                    description = product.Description,
                    imageURL = product.ImageURL,
                    stock = product.Stock,
                    categoryName = product.Category?.Name ?? "Chưa phân loại",
                    brandName = product.Brand?.Name ?? "Không có thương hiệu",
                    averageRating = reviewData?.AverageRating ?? 0,
                    reviewCount = reviewData?.ReviewCount ?? 0,
                    attributes = attributesList
                };

                return Json(productData);
            }
            catch (Exception ex)
            {
                // Log lỗi và trả về lỗi 500
                Console.WriteLine($"Error in GetProductJson: {ex.Message}");
                return StatusCode(500, new { error = "Không thể tải thông tin sản phẩm" });
            }
        }

        // API để lấy gợi ý sản phẩm khi tìm kiếm
        [HttpGet]
        public async Task<IActionResult> SearchSuggestions(string term, int limit = 5)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(term) || term.Length < 2)
                {
                    return Json(new { success = true, suggestions = new List<object>() });
                }

                var suggestions = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Brand)
                    .Where(p => p.Name.Contains(term) || 
                               p.Description.Contains(term) ||
                               p.Category.Name.Contains(term) ||
                               p.Brand.Name.Contains(term))
                    .Take(limit)
                    .Select(p => new {
                        id = p.ProductID,
                        name = p.Name,
                        price = p.Price,
                        imageUrl = !string.IsNullOrEmpty(p.ImageURL) ? 
                                  (p.ImageURL.StartsWith("/") ? p.ImageURL : $"/upload/product/{p.ImageURL}") :
                                  "/image/loading-placeholder.png",
                        categoryName = p.Category.Name,
                        brandName = p.Brand.Name,
                        url = $"/Product/Details/{p.ProductID}"
                    })
                    .ToListAsync();

                return Json(new { success = true, suggestions = suggestions });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SearchSuggestions: {ex.Message}");
                return Json(new { success = false, suggestions = new List<object>() });
            }
        }

        // Thêm vào ProductController
        [HttpGet]
        public async Task<IActionResult> GetProductAttributes(int productId)
        {
            try
            {
                var attributes = await _context.ProductAttributes
                    .Where(a => a.ProductID == productId && a.Stock > 0)
                    .Select(a => new {
                        attributeID = a.AttributeID,
                        color = a.Color,
                        size = a.Size,
                        price = a.Price,
                        stock = a.Stock
                    })
                    .ToListAsync();
                    
                return Json(new { success = true, attributes });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }
    }
}