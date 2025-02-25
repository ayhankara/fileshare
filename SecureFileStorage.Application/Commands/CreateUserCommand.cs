using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureFileStorage.Application.Commands
{
    public class CreateUserCommand
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public string? Name { get; set; }
        public string? Surname { get; set; }
    }
}
