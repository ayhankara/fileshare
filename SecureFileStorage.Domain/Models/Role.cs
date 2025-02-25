using SecureFileStorage.Domain.ModelPermissions;
using SecureFileStorage.Domain.Models;

namespace SecureFileStorage.Models
{
    public class Role
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public virtual ICollection<UserRoles>? UserRoles { get; set; }
        public virtual ICollection<RolePermissions>? RolePermissions { get; set; }
        public virtual ICollection<FileRolePermission>? FileRolePermissions { get; set; }
    }


}
