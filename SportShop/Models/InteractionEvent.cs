using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportShop.Models
{
    [Table("InteractionEvent")]
    public class InteractionEvent
    {
        [Key]
        public int EventID { get; set; }

        [Required]
        public int UserID { get; set; }

        [Required]
        [MaxLength(50)]
        public string EventType { get; set; } = string.Empty;

        public int? ProductID { get; set; }

        public int? Rating { get; set; }

        [Required]
        public DateTime CreatedAt { get; set; }

        // Navigation properties
        [ForeignKey("UserID")]
        public virtual User? User { get; set; }

        [ForeignKey("ProductID")]
        public virtual Product? Product { get; set; }
    }

    // Enum for Event Types
    public static class EventType
    {
        public const string VIEW_PRODUCT = "VIEW_PRODUCT";
        public const string ADD_TO_CART = "ADD_TO_CART";
        public const string REMOVE_FROM_CART = "REMOVE_FROM_CART";
        public const string ADD_TO_WISHLIST = "ADD_TO_WISHLIST";
        public const string REMOVE_FROM_WISHLIST = "REMOVE_FROM_WISHLIST";
        public const string PURCHASE = "PURCHASE";
        public const string SEARCH = "SEARCH";
        public const string FILTER_CATEGORY = "FILTER_CATEGORY";
        public const string FILTER_BRAND = "FILTER_BRAND";
        public const string QUICK_VIEW = "QUICK_VIEW";
        public const string SHARE_PRODUCT = "SHARE_PRODUCT";
        public const string WRITE_REVIEW = "WRITE_REVIEW";
    }
}
