using SecureFileStorage.Models;

namespace SecureFileStorage.Domain.ModelPermissions
{

    public class FileRolePermission
    {
        public int FileId { get; set; }
        public virtual SecureFileStorage.Models.File? File { get; set; }
        public int RoleId { get; set; }
        public virtual Role? Role { get; set; }
        public int PermissionId { get; set; }
        public virtual Permission? Permission { get; set; }
    }
}
