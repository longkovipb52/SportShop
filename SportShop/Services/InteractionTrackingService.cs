using Microsoft.EntityFrameworkCore;
using SportShop.Data;
using SportShop.Models;
using System;
using System.Threading.Tasks;

namespace SportShop.Services
{
    public class InteractionTrackingService
    {
        private readonly ApplicationDbContext _context;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public InteractionTrackingService(ApplicationDbContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        /// <summary>
        /// Track user interaction event
        /// </summary>
        public async Task TrackEventAsync(string eventType, int? productId = null, int? rating = null)
        {
            try
            {
                var httpContext = _httpContextAccessor.HttpContext;
                if (httpContext == null) return;

                var userId = httpContext.Session.GetInt32("UserId");
                
                // Chỉ track khi user đã đăng nhập
                if (!userId.HasValue) return;

                var interactionEvent = new InteractionEvent
                {
                    UserID = userId.Value,
                    EventType = eventType,
                    ProductID = productId,
                    Rating = rating,
                    CreatedAt = DateTime.Now
                };

                _context.InteractionEvent.Add(interactionEvent);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                // Log error but don't throw - tracking shouldn't break the main flow
                Console.WriteLine($"[InteractionTracking] Error: {ex.Message}");
            }
        }

        /// <summary>
        /// Track product view
        /// </summary>
        public async Task TrackProductViewAsync(int productId)
        {
            await TrackEventAsync(EventType.VIEW_PRODUCT, productId);
        }

        /// <summary>
        /// Track add to cart
        /// </summary>
        public async Task TrackAddToCartAsync(int productId, int quantity = 1, int? attributeId = null)
        {
            await TrackEventAsync(EventType.ADD_TO_CART, productId);
        }

        /// <summary>
        /// Track remove from cart
        /// </summary>
        public async Task TrackRemoveFromCartAsync(int productId, int? attributeId = null)
        {
            await TrackEventAsync(EventType.REMOVE_FROM_CART, productId);
        }

        /// <summary>
        /// Track add to wishlist
        /// </summary>
        public async Task TrackAddToWishlistAsync(int productId)
        {
            await TrackEventAsync(EventType.ADD_TO_WISHLIST, productId);
        }

        /// <summary>
        /// Track remove from wishlist
        /// </summary>
        public async Task TrackRemoveFromWishlistAsync(int productId)
        {
            await TrackEventAsync(EventType.REMOVE_FROM_WISHLIST, productId);
        }

        /// <summary>
        /// Track purchase - track từng product được mua
        /// </summary>
        public async Task TrackPurchaseAsync(int orderId, decimal totalAmount, int[] productIds)
        {
            // Track từng sản phẩm được mua
            foreach (var productId in productIds)
            {
                await TrackEventAsync(EventType.PURCHASE, productId);
            }
        }

        /// <summary>
        /// Track search
        /// </summary>
        public async Task TrackSearchAsync(string keyword, int resultCount)
        {
            // Search không có ProductID cụ thể
            await TrackEventAsync(EventType.SEARCH, null);
        }

        /// <summary>
        /// Track category filter
        /// </summary>
        public async Task TrackCategoryFilterAsync(int categoryId, string categoryName)
        {
            // Category filter không có ProductID cụ thể
            await TrackEventAsync(EventType.FILTER_CATEGORY, null);
        }

        /// <summary>
        /// Track brand filter
        /// </summary>
        public async Task TrackBrandFilterAsync(int brandId, string brandName)
        {
            // Brand filter không có ProductID cụ thể
            await TrackEventAsync(EventType.FILTER_BRAND, null);
        }

        /// <summary>
        /// Track quick view
        /// </summary>
        public async Task TrackQuickViewAsync(int productId)
        {
            await TrackEventAsync(EventType.QUICK_VIEW, productId);
        }

        /// <summary>
        /// Track review write
        /// </summary>
        public async Task TrackWriteReviewAsync(int productId, int rating)
        {
            await TrackEventAsync(EventType.WRITE_REVIEW, productId, rating);
        }

        /// <summary>
        /// Get user's interaction history for recommendations
        /// </summary>
        public async Task<List<InteractionEvent>> GetUserInteractionsAsync(int userId, int limit = 100)
        {
            return await _context.InteractionEvent
                .Where(e => e.UserID == userId)
                .OrderByDescending(e => e.CreatedAt)
                .Take(limit)
                .Include(e => e.Product)
                .ToListAsync();
        }

        /// <summary>
        /// Get trending products based on recent interactions
        /// </summary>
        public async Task<List<int>> GetTrendingProductsAsync(int days = 7, int limit = 10)
        {
            var startDate = DateTime.Now.AddDays(-days);

            return await _context.InteractionEvent
                .Where(e => e.CreatedAt >= startDate && 
                           e.ProductID.HasValue &&
                           (e.EventType == EventType.VIEW_PRODUCT || 
                            e.EventType == EventType.ADD_TO_CART ||
                            e.EventType == EventType.QUICK_VIEW))
                .GroupBy(e => e.ProductID)
                .Select(g => new
                {
                    ProductID = g.Key!.Value,
                    Score = g.Count()
                })
                .OrderByDescending(x => x.Score)
                .Take(limit)
                .Select(x => x.ProductID)
                .ToListAsync();
        }

        /// <summary>
        /// Get products viewed together (collaborative filtering)
        /// </summary>
        public async Task<List<int>> GetViewedTogetherAsync(int productId, int limit = 8)
        {
            // Get users who viewed this product
            var usersWhoViewed = await _context.InteractionEvent
                .Where(e => e.ProductID == productId && 
                           (e.EventType == EventType.VIEW_PRODUCT || e.EventType == EventType.QUICK_VIEW))
                .Select(e => e.UserID)
                .Distinct()
                .ToListAsync();

            if (!usersWhoViewed.Any())
                return new List<int>();

            return await _context.InteractionEvent
                .Where(e => usersWhoViewed.Contains(e.UserID) && 
                           e.ProductID.HasValue &&
                           e.ProductID != productId &&
                           (e.EventType == EventType.VIEW_PRODUCT || e.EventType == EventType.QUICK_VIEW))
                .GroupBy(e => e.ProductID)
                .Select(g => new
                {
                    ProductID = g.Key!.Value,
                    Score = g.Count()
                })
                .OrderByDescending(x => x.Score)
                .Take(limit)
                .Select(x => x.ProductID)
                .ToListAsync();
        }
    }
}
