using MediatR;
using SecureFileStorage.Application.DTOs;
using SecureFileStorage.Infrastructure.Services.Interfaces;
using System.Threading;
using System.Threading.Tasks;

namespace SecureFileStorage.Infrastructure.Handlers
{
    public class RegisterUserHandler : IRequestHandler<RegisterUserCommand, bool>
    {
        private readonly IUserService _userService;

        public RegisterUserHandler(IUserService userService)
        {
            _userService = userService;
        }

        public async Task<bool> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
        {
            var userRegisterDto = request.UserRegisterDto?? new UserRegisterDto { };
            return await _userService.RegisterUser(userRegisterDto);
        }
    }
}
