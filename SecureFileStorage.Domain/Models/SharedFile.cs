namespace SecureFileStorage.Models
{
    public class SharedFile
    {
        public int Id { get; set; }
        public int FileId { get; set; }
        public virtual File? File { get; set; }
        public int SharedByUserId { get; set; }
        public virtual User? SharedByUser { get; set; }
        public int SharedWithUserId { get; set; }
        public virtual User? SharedWithUser { get; set; }
        public DateTime SharedDate { get; set; }
        public string? ShareLink { get; set; }
    }


}
