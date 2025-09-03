namespace SportShop.Models.DTOs
{
    public class ProductRatingDTO
    {
        public int ProductID { get; set; }
        public double AverageRating { get; set; }
        public int ReviewCount { get; set; }
    }
}