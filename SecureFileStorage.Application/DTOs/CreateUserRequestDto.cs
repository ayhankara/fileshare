using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureFileStorage.Application.DTOs
{
    public class CreateUserRequestDto
    {
        [Required]
        [EmailAddress]
        public string? Email { get; set; }

        [Required]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "The Password must be at least 6 characters long.")]
        public string? Password { get; set; }

        public string? DisplayName { get; set; } = "";
    }
}
