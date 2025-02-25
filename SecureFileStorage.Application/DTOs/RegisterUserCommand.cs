using MediatR;
using SecureFileStorage.Application.DTOs;

namespace SecureFileStorage.Application.DTOs
{
    public class RegisterUserCommand : IRequest<bool>
    {
        public UserRegisterDto? UserRegisterDto { get; set; }
    }

}
