using Microsoft.AspNetCore.Mvc;
using Moq;
using SecureFileStorage.Controller;
using SecureFileStorage.Application.DTOs;
using SecureFileStorage.Infrastructure.Services.Interfaces;
using Xunit;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using SecureFileStorage.Models; // AuthResponse sınıfının bulunduğu namespace
namespace SecureFileStorage.Tests
{


    public class UserControllerTests
    {
        private readonly Mock<IUserService> _userServiceMock;
        private readonly Mock<IAuthService> _authServiceMock;
        private readonly Mock<ILogger<UserController>> _loggerMock;
        private readonly UserController _controller;

        public UserControllerTests()
        {
            _userServiceMock = new Mock<IUserService>();
            _authServiceMock = new Mock<IAuthService>();
            _loggerMock = new Mock<ILogger<UserController>>();
            _controller = new UserController(_userServiceMock.Object, _authServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Register_ValidModel_ReturnsOkResult()
        {
            // Arrange
            var userRegisterDto = new UserRegisterDto { Email = "test@example.com", Password = "password" };
            _userServiceMock.Setup(x => x.RegisterUser(userRegisterDto)).ReturnsAsync(true);

            // Act
            var result = await _controller.Register(userRegisterDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();

            if (result is OkObjectResult okResult)
            {
                okResult.Value.Should().Be("Kullanıcı başarıyla kaydedildi.");
            }
            else
            {
                // result OkObjectResult türünde değilse yapılacak işlemler
                Assert.Fail("Result is not OkObjectResult"); // Veya başka bir assertion kullanın
            }
        }

        [Fact]
        public async Task Register_InvalidModel_ReturnsBadRequestResult()
        {
            // Arrange
            _controller.ModelState.AddModelError("Email", "Email is required");

            // Act
            var result = await _controller.Register(new UserRegisterDto());

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Login_ValidCredentials_ReturnsOkResult()
        {
            // Arrange
            var userLoginDto = new UserLoginDto { Email = "test@example.com", Password = "password" };
            var tokenDto = new TokenDto { AccessToken = "access_token", RefreshToken = "refresh_token" };
            _userServiceMock.Setup(x => x.LoginUser(userLoginDto)).ReturnsAsync(tokenDto);

            // Act
            var result = await _controller.Login(userLoginDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();

            if (result is OkObjectResult okResult)
            {
                okResult.Value.Should().Be(tokenDto);
            }
            else
            {
                Assert.Fail("Result is not OkObjectResult");
            }
        }

        [Fact]
        public async Task Login_InvalidCredentials_ReturnsUnauthorizedResult()
        {
            // Arrange
            var userLoginDto = new UserLoginDto { Email = "test@example.com", Password = "password" };
            _userServiceMock.Setup(x => x.LoginUser(userLoginDto)).ReturnsAsync((TokenDto)null!);

            // Act
            var result = await _controller.Login(userLoginDto);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();

            if (result is UnauthorizedObjectResult unauthorizedResult)
            {
                unauthorizedResult.Value.Should().Be("Email veya şifre yanlış.");
            }
            else
            {
                Assert.Fail("Result is not UnauthorizedObjectResult");
            }
        }
        [Fact]
        public async Task RegisterFireBase_ValidModel_ReturnsOkResult()
        {
            // Arrange
            var request = new CreateUserRequestDto { Email = "test@example.com", Password = "password", DisplayName = "Test User" };
            var loginResponse = new LoginResponse
            {
                IsAuthenticated = true,
                User = new AuthenticatedUser { Email = "test@example.com", Uid = "123" }
            };
            _authServiceMock.Setup(x => x.CreateUserAsync(request.Email, request.Password, request.DisplayName)).ReturnsAsync(loginResponse);

            // Act
            var result = await _controller.RegisterFireBase(request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            var authenticatedUser = okResult.Value as AuthenticatedUser;
            authenticatedUser.Should().NotBeNull();
            authenticatedUser.Email.Should().Be("test@example.com");
        }

        [Fact]
        public async Task RegisterFireBase_InvalidModel_ReturnsBadRequestResult()
        {
            // Arrange
            _controller.ModelState.AddModelError("Email", "Email is required");

            // Act
            var result = await _controller.RegisterFireBase(new CreateUserRequestDto());

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task LoginFireBase_ValidCredentials_ReturnsOkResult()
        {
            // Arrange
            var request = new LoginRequest { Email = "test@example.com", Password = "password", returnSecureToken = true };
            var loginResponse = new LoginResponse
            {
                IsAuthenticated = true,
                User = new AuthenticatedUser { Email = "test@example.com", Uid = "123" }
            };
            _authServiceMock.Setup(x => x.AuthenticateAsync(request.Email, request.Password, request.returnSecureToken)).ReturnsAsync(loginResponse);

            // Act
            var result = await _controller.LoginFireBase(request);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult.Should().NotBeNull();
            var authenticatedUser = okResult.Value as AuthenticatedUser;
            authenticatedUser.Should().NotBeNull();
            authenticatedUser.Email.Should().Be("test@example.com");
        }

        [Fact]
        public async Task LoginFireBase_InvalidCredentials_ReturnsUnauthorizedResult()
        {
            // Arrange
            var request = new LoginRequest { Email = "test@example.com", Password = "password", returnSecureToken = true };
            var loginResponse = new LoginResponse { IsAuthenticated = false, ErrorMessage = "Invalid credentials" };
            _authServiceMock.Setup(x => x.AuthenticateAsync(request.Email, request.Password, request.returnSecureToken)).ReturnsAsync(loginResponse);

            // Act
            var result = await _controller.LoginFireBase(request);

            // Assert
            result.Should().BeOfType<UnauthorizedObjectResult>();
            var unauthorizedResult = result as UnauthorizedObjectResult;
            unauthorizedResult.Should().NotBeNull();
            unauthorizedResult.Value.Should().Be("Invalid credentials");
        }
    }
}
