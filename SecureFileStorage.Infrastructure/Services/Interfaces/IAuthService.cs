using SecureFileStorage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SecureFileStorage.Infrastructure.Services.Interfaces
{
    public interface IAuthService
    {
        Task<LoginResponse> AuthenticateAsync(string email, string password, bool returnSecureToken);
        Task<LoginResponse> CreateUserAsync(string email, string password, string displayName ); //Yeni metod
    }
}
