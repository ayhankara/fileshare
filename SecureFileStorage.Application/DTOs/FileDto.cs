namespace SecureFileStorage.Application.DTOs
{
    public class FileDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? Path { get; set; }
        public long? Size { get; set; }
        public string? Type { get; set; }
        public DateTime CreateDate { get; set; }
        public int OwnerId { get; set; }  // Dosyanın sahibini belirtmek için
        public string? BlobId { get; set; }
    }
}
