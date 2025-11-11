using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;

namespace SportShop.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class RecommendationController : ControllerBase
    {
        private readonly IDbConnection _db;
        private readonly ILogger<RecommendationController> _logger;
        
        public RecommendationController(IDbConnection db, ILogger<RecommendationController> logger)
        {
            _db = db;
            _logger = logger;
        }
        
        /// <summary>
        /// Get personalized recommendations for a specific user
        /// </summary>
        [HttpGet("{userId}")]
        public async Task<IActionResult> GetRecommendations(int userId, int count = 10)
        {
            try
            {
                _logger.LogInformation($"Fetching {count} recommendations for user {userId}");
                
                var recommendations = await _db.QueryAsync<RecommendationDto>(@"
                    SELECT TOP (@count) 
                        r.ProductID,
                        p.Name as ProductName,
                        p.Price,
                        p.ImageURL,
                        CAST(r.Score AS DECIMAL(10,3)) as Score,
                        c.Name as CategoryName,
                        b.Name as BrandName,
                        COALESCE(AVG(CAST(rev.Rating AS DECIMAL(3,1))), 0) as AverageRating,
                        COUNT(rev.ReviewID) as ReviewCount
                    FROM Recommendation r
                    JOIN Product p ON r.ProductID = p.ProductID
                    LEFT JOIN Category c ON p.CategoryID = c.CategoryID
                    LEFT JOIN Brand b ON p.BrandID = b.BrandID
                    LEFT JOIN Review rev ON p.ProductID = rev.ProductID AND rev.Rating IS NOT NULL
                    WHERE r.UserID = @userId
                    GROUP BY r.ProductID, p.Name, p.Price, p.ImageURL, r.Score, c.Name, b.Name
                    ORDER BY r.Score DESC
                ", new { userId, count });
                
                _logger.LogInformation($"Found {recommendations.Count()} recommendations for user {userId}");
                
                return Ok(new { 
                    userId = userId,
                    recommendations = recommendations,
                    count = recommendations.Count(),
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching recommendations for user {userId}");
                return BadRequest(new { error = $"Failed to fetch recommendations: {ex.Message}" });
            }
        }
        
        /// <summary>
        /// Get popular/trending recommendations for anonymous users
        /// </summary>
        [HttpGet("popular")]
        public async Task<IActionResult> GetPopularRecommendations(int count = 10)
        {
            try
            {
                _logger.LogInformation($"Fetching {count} popular recommendations");
                
                var popular = await _db.QueryAsync<RecommendationDto>(@"
                    SELECT TOP (@count)
                        p.ProductID,
                        p.Name as ProductName,
                        p.Price,
                        p.ImageURL,
                        COUNT(*) as Score,
                        c.Name as CategoryName,
                        b.Name as BrandName,
                        COALESCE(AVG(CAST(rev.Rating AS DECIMAL(3,1))), 0) as AverageRating,
                        COUNT(DISTINCT rev.ReviewID) as ReviewCount
                    FROM Recommendation r
                    JOIN Product p ON r.ProductID = p.ProductID
                    LEFT JOIN Category c ON p.CategoryID = c.CategoryID
                    LEFT JOIN Brand b ON p.BrandID = b.BrandID
                    LEFT JOIN Review rev ON p.ProductID = rev.ProductID AND rev.Rating IS NOT NULL
                    GROUP BY p.ProductID, p.Name, p.Price, p.ImageURL, c.Name, b.Name
                    ORDER BY COUNT(*) DESC
                ", new { count });
                
                _logger.LogInformation($"Found {popular.Count()} popular recommendations");
                
                return Ok(new {
                    recommendations = popular,
                    count = popular.Count(),
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching popular recommendations");
                return BadRequest(new { error = $"Failed to fetch popular recommendations: {ex.Message}" });
            }
        }
        
        /// <summary>
        /// Get recommendations based on a specific product (similar products)
        /// </summary>
        [HttpGet("similar/{productId}")]
        public async Task<IActionResult> GetSimilarProducts(int productId, int count = 6)
        {
            try
            {
                _logger.LogInformation($"Fetching {count} similar products for product {productId}");
                
                var similar = await _db.QueryAsync<RecommendationDto>(@"
                    SELECT TOP (@count)
                        p2.ProductID,
                        p2.Name as ProductName,
                        p2.Price,
                        p2.ImageURL,
                        AVG(CAST(r.Score AS DECIMAL(10,3))) as Score,
                        c.Name as CategoryName,
                        b.Name as BrandName,
                        COALESCE(AVG(CAST(rev.Rating AS DECIMAL(3,1))), 0) as AverageRating,
                        COUNT(DISTINCT rev.ReviewID) as ReviewCount
                    FROM Recommendation r
                    JOIN Product p1 ON r.ProductID = @productId
                    JOIN Recommendation r2 ON r.UserID = r2.UserID
                    JOIN Product p2 ON r2.ProductID = p2.ProductID
                    LEFT JOIN Category c ON p2.CategoryID = c.CategoryID
                    LEFT JOIN Brand b ON p2.BrandID = b.BrandID
                    LEFT JOIN Review rev ON p2.ProductID = rev.ProductID AND rev.Rating IS NOT NULL
                    WHERE r2.ProductID != @productId
                    GROUP BY p2.ProductID, p2.Name, p2.Price, p2.ImageURL, c.Name, b.Name
                    ORDER BY AVG(CAST(r.Score AS DECIMAL(10,3))) DESC
                ", new { productId, count });
                
                _logger.LogInformation($"Found {similar.Count()} similar products for product {productId}");
                
                return Ok(new {
                    productId = productId,
                    recommendations = similar,
                    count = similar.Count(),
                    timestamp = DateTime.UtcNow
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching similar products for product {productId}");
                return BadRequest(new { error = $"Failed to fetch similar products: {ex.Message}" });
            }
        }
        
        /// <summary>
        /// Log user interaction for future model training
        /// </summary>
        [HttpPost("interaction")]
        public async Task<IActionResult> LogInteraction([FromBody] InteractionRequest request)
        {
            try
            {
                if (request == null || request.UserId <= 0 || request.ProductId <= 0)
                {
                    return BadRequest(new { error = "Invalid interaction data" });
                }
                
                await _db.ExecuteAsync(@"
                    INSERT INTO InteractionEvent (UserID, ProductID, EventType, EventValue, Timestamp)
                    VALUES (@UserId, @ProductId, @EventType, @EventValue, @Timestamp)
                ", new {
                    request.UserId,
                    request.ProductId,
                    request.EventType,
                    request.EventValue,
                    Timestamp = DateTime.UtcNow
                });
                
                _logger.LogInformation($"Logged interaction: User {request.UserId}, Product {request.ProductId}, Type: {request.EventType}");
                
                return Ok(new { message = "Interaction logged successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error logging interaction");
                return BadRequest(new { error = $"Failed to log interaction: {ex.Message}" });
            }
        }
        
        /// <summary>
        /// Get recommendation statistics
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetRecommendationStats()
        {
            try
            {
                var stats = await _db.QuerySingleAsync<RecommendationStats>(@"
                    SELECT 
                        (SELECT COUNT(*) FROM Recommendation) as TotalRecommendations,
                        (SELECT COUNT(DISTINCT UserID) FROM Recommendation) as UsersWithRecommendations,
                        (SELECT COUNT(DISTINCT ProductID) FROM Recommendation) as RecommendedProducts,
                        (SELECT COUNT(*) FROM InteractionEvent) as TotalInteractions,
                        (SELECT MAX(Timestamp) FROM InteractionEvent) as LastInteraction,
                        (SELECT AVG(CAST(Score AS DECIMAL(10,3))) FROM Recommendation) as AverageScore
                ");
                
                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error fetching recommendation statistics");
                return BadRequest(new { error = $"Failed to fetch statistics: {ex.Message}" });
            }
        }
    }
    
    public class RecommendationDto
    {
        public int ProductID { get; set; }
        public string ProductName { get; set; } = "";
        public decimal? Price { get; set; }
        public string ImageURL { get; set; } = "";
        public decimal Score { get; set; }
        public string CategoryName { get; set; } = "";
        public string BrandName { get; set; } = "";
        public decimal AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }
    
    public class InteractionRequest
    {
        public int UserId { get; set; }
        public int ProductId { get; set; }
        public string EventType { get; set; } = "view";
        public decimal EventValue { get; set; } = 1.0m;
    }
    
    public class RecommendationStats
    {
        public int TotalRecommendations { get; set; }
        public int UsersWithRecommendations { get; set; }
        public int RecommendedProducts { get; set; }
        public int TotalInteractions { get; set; }
        public DateTime? LastInteraction { get; set; }
        public decimal AverageScore { get; set; }
    }
}