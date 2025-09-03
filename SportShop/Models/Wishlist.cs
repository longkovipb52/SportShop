using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportShop.Models
{
    [Table("Wishlist")]
    public class Wishlist
    {
        [Key]
        public int WishlistID { get; set; }
        
        public int UserID { get; set; }
        
        [ForeignKey("UserID")]
        public virtual User User { get; set; }
        
        public int ProductID { get; set; }
        
        [ForeignKey("ProductID")]
        public virtual Product Product { get; set; }
        
        public DateTime CreatedAt { get; set; }
    }
}