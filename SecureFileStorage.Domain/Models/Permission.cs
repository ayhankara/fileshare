using SecureFileStorage.Domain.ModelPermissions;

namespace SecureFileStorage.Models
{
    public class Permission
    {
        public int Id { get; set; }
        public int? FileId { get; set; }
        public int? FolderId { get; set; }
        public int? UserId { get; set; }
        public int? AccessLevel { get; set; }
        public DateTime? CreateDate { get; set; }
        public int? IsActive { get; set; }
        public int? IsDelete { get; set; }
        public File? File { get; set; }
        public Folder? Folder { get; set; }
        public User? User { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public virtual ICollection<RolePermissions>? RolePermissions { get; set; }
        public virtual ICollection<FileUserPermission>? FileUserPermissions { get; set; }
        public virtual ICollection<FileRolePermission>? FileRolePermissions { get; set; }
    }
 

}
