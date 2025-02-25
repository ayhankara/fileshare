using System.ComponentModel.DataAnnotations;

namespace SecureFileStorage.Models
{
    public class LoginRequest
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        public string? Password { get; set; }

        public bool returnSecureToken { get; set; }

    
    }
}
