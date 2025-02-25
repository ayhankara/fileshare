using SecureFileStorage.Models;

namespace SecureFileStorage.Domain.ModelPermissions
{
    public class FileUserPermission
    {
        public int FileId { get; set; }
        public virtual SecureFileStorage.Models.File? File { get; set; }
        public int UserId { get; set; }
        public virtual User? User { get; set; }
        public int PermissionId { get; set; }
        public virtual Permission? Permission { get; set; }
    }
}
