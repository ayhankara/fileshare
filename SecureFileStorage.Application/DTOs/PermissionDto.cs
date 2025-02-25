namespace SecureFileStorage.Application.DTOs
{
    public class PermissionDto
    {
        public int FileId { get; set; }
        public int UserId { get; set; }
        public AccessLevel AccessLevel { get; set; }
    }
    public enum AccessLevel
    {
        View,
        Edit,
        Comment,
        Owner
    }
}
