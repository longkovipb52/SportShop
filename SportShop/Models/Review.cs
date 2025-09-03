using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportShop.Models
{
    [Table("Review")]
    public class Review
    {
        public int ReviewID { get; set; }

        public int ProductID { get; set; }

        public int UserID { get; set; }

        public int? Rating { get; set; }

        [MaxLength(500)]
        public string Comment { get; set; }

        public DateTime? CreatedAt { get; set; }

        [MaxLength(20)]
        public string Status { get; set; }

        // Navigation properties
        public virtual Product Product { get; set; }

        public virtual User User { get; set; }
    }
}