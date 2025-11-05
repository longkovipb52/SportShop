using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SportShop.Models
{
    [Table("CategoryAttributeType")]
    public class CategoryAttributeType
    {
        [Key]
        public int CategoryAttributeTypeID { get; set; }
        
        public int CategoryID { get; set; }
        
        public int AttributeTypeID { get; set; }
        
        public bool IsRequired { get; set; } = false;
        
        public int DisplayOrder { get; set; }
        
        // Navigation properties
        public Category? Category { get; set; }
        public AttributeType? AttributeType { get; set; }
    }
}
