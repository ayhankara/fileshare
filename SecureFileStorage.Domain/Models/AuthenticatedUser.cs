namespace SecureFileStorage.Models
{
    public class AuthenticatedUser
    {
        public string? Uid { get; set; } // Firebase UID
        public string? Email { get; set; }
        public string? DisplayName { get; set; }
        public string? PhotoUrl { get; set; }
        public string? IdToken { get; set; }
        public string? RefreshToken { get; set; }
        public string? ExpiresIn { get; set; }
        public string? LocalId { get; set; }

   
    }
}
