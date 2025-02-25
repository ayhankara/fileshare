namespace SecureFileStorage.Models
{
    public class LoginResponse
    {
        public bool IsAuthenticated { get; set; }
        public AuthenticatedUser? User { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
