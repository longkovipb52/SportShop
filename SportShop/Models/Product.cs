using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportShop.Models
{
    [Table("Product")]
    public class Product
    {
        [Key]
        public int ProductID { get; set; }
        
        [Required]
        [Display(Name = "Danh mục chính")]
        public int CategoryID { get; set; }
        
        [Display(Name = "Danh mục con")]
        public int? SubCategoryID { get; set; }
        
        [ForeignKey("CategoryID")]
        public virtual Category Category { get; set; }
        
        [ForeignKey("SubCategoryID")]
        public virtual SubCategory? SubCategory { get; set; }
        
        public int? BrandID { get; set; }  // Thêm BrandID, nullable
        
        [ForeignKey("BrandID")]
        public virtual Brand Brand { get; set; }  // Thêm navigation property
        
        [Required]
        [StringLength(100)]
        public string Name { get; set; }
        
        public string Description { get; set; }
        
        [Required]
        [Column(TypeName = "decimal(18, 2)")]
        public decimal Price { get; set; }
        
        [Required]
        public int Stock { get; set; }
        
        // Tổng số lượt nhấn yêu thích (không giảm khi bỏ yêu thích)
        public int TotalLikes { get; set; } = 0;
        
        [StringLength(255)]
        public string ImageURL { get; set; }
        
        public DateTime CreatedAt { get; set; }
        
        [NotMapped]
        public double AverageRating
        {
            get
            {
                if (Reviews == null || !Reviews.Any())
                    return 0;
                
                var validReviews = Reviews.Where(r => r.Rating.HasValue).ToList();
                if (!validReviews.Any())
                    return 0;
                
                return Math.Round(validReviews.Average(r => r.Rating.Value), 1);
            }
        }
        
        [NotMapped]
        public int ReviewCount
        {
            get
            {
                if (Reviews == null)
                    return 0;
                
                return Reviews.Count;
            }
        }
        // Navigation properties
        public virtual ICollection<ProductAttribute> Attributes { get; set; }
        
        public virtual ICollection<Review> Reviews { get; set; }
        
        public virtual ICollection<OrderItem> OrderItems { get; set; }
        
        public virtual ICollection<Cart> Carts { get; set; }
        
        public virtual ICollection<Wishlist> Wishlists { get; set; }
    }
}