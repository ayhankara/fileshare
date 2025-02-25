namespace SecureFileStorage.Application.DTOs
{
    public class SharedFileDto
    {
        public int FileId { get; set; }
        public string? FileName { get; set; }
        public int SharedByUserId { get; set; }
        public DateTime SharedDate { get; set; } 
    }
}
