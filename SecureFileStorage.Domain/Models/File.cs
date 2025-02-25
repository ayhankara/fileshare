using SecureFileStorage.Domain.ModelPermissions;
using System;

namespace SecureFileStorage.Models
{
    public class File
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Path { get; set; }
        public int? OwnerId { get; set; }
        public long? Size { get; set; }
        public string? Type { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? UploadDate { get; set; }
        public int? IsActive { get; set; }
        public int? IsDelete { get; set; }
        
        public User? Owner { get; set; }
        public int? FolderId { get; set; }
        public Folder? Folder { get; set; }
        public string? BlobId { get; set; } // Azure Blob Storage'daki dosya adı
        public virtual ICollection<FileUserPermission>? FileUserPermissions { get; set; }
        public virtual ICollection<FileRolePermission>? FileRolePermissions { get; set; }

    }


}
