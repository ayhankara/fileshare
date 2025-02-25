using SecureFileStorage.Models;

namespace SecureFileStorage.Domain.ModelPermissions
{
    public class RolePermissions
    {
        public int RoleId { get; set; }
        public virtual Role? Role { get; set; }
        public int PermissionId { get; set; }
        public virtual Permission? Permission { get; set; }
    }
}
