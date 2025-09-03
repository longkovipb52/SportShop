using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportShop.Models
{
    [Table("Category")]
    public class Category
    {
        [Key]
        public int CategoryID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string ImageURL { get; set; }
        
        public ICollection<Product> Products { get; set; }
    }
}