using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using SecureFileStorage.Models;

namespace SecureFileStorage.Domain.Models
{
    public class UserRoles
    {
        [Key]
        [Column(Order = 0)]
        public int UserId { get; set; }

        [ForeignKey("UserId")]
        public virtual User? User { get; set; }

        [Key]
        [Column(Order = 1)]
        public int RoleId { get; set; }

        [ForeignKey("RoleId")]
        public virtual Role? Role { get; set; }
    }
}