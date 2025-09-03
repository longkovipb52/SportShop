using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Collections.Generic;

namespace SportShop.Models
{
    [Table("Role")]
    public class Role
    {
        [Key]
        public int RoleID { get; set; }
        
        [Required]
        [StringLength(50)]
        public string RoleName { get; set; }
        
        // Navigation Properties
        public virtual ICollection<User> Users { get; set; }
    }
}