using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SecureFileStorage.Application.DTOs;
using SecureFileStorage.Infrastructure.Services.Interfaces;
using SecureFileStorage.Models;

namespace SecureFileStorage.Controller
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IAuthService _authService;
        private readonly ILogger<UserController> _logger;
        public UserController(IUserService userService, IAuthService authService, ILogger<UserController> logger)
        {
            _userService = userService;
            _authService = authService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(UserRegisterDto userRegisterDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for user registration.");
                return BadRequest(ModelState);
            }

            try
            {
                var result = await _userService.RegisterUser(userRegisterDto);

                if (result)
                {
                    _logger.LogInformation("User registered successfully: {Email}", userRegisterDto.Email);
                    return Ok("Kullanıcı başarıyla kaydedildi.");
                }
                else
                {
                    _logger.LogWarning("User registration failed: {Email}", userRegisterDto.Email);
                    return BadRequest("Kullanıcı kaydı başarısız oldu. Email zaten kullanılıyor olabilir.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while registering user: {Email}", userRegisterDto.Email);
                return StatusCode(500, "Internal server error");
            }
        }
       

        [HttpPost("RegisterFireBase")]
        [AllowAnonymous]
        public async Task<IActionResult> RegisterFireBase([FromBody] CreateUserRequestDto request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _authService.CreateUserAsync(request.Email??"", request.Password??"", request.DisplayName??"");

            if (response.IsAuthenticated)
            {
                return Ok(response.User);
            }
            else
            {
                return BadRequest(response.ErrorMessage); // Kullanıcı oluşturma hatası
            }
        }

         
        [HttpPost("login")]
        public async Task<IActionResult> Login(UserLoginDto userLoginDto)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid model state for user login.");
                return BadRequest(ModelState);
            }

            try
            {
                var token = await _userService.LoginUser(userLoginDto);

                if (token == null)
                {
                    _logger.LogWarning("Login failed for user: {Email}", userLoginDto.Email);
                    return Unauthorized("Email veya şifre yanlış.");
                }

                _logger.LogInformation("User logged in successfully: {Email}", userLoginDto.Email);
                return Ok(token);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while logging in user: {Email}", userLoginDto.Email);
                return StatusCode(500, "Internal server error");
            }
        }
 


        [HttpPost("LoginFireBase")]
        [AllowAnonymous] // Kimlik doğrulaması gerektirmeyen bir endpoint
        public async Task<IActionResult> LoginFireBase([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var response = await _authService.AuthenticateAsync(request.Email??"", request.Password??"",request.returnSecureToken);

            if (response.IsAuthenticated)
            { 
                return Ok(response.User);
            }
            else
            {
                return Unauthorized(response.ErrorMessage);
            }
        }



        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequestDto tokenDto)
        {
            if (tokenDto is null)
            {
                return BadRequest("Invalid client request");
            }

            if (string.IsNullOrEmpty(tokenDto.RefreshToken))
            {
                return BadRequest("Refresh token is required");
            }

            var token = await _userService.RefreshToken(tokenDto.RefreshToken);

            if (token is null)
            {
                return BadRequest("Invalid refresh token or access token");
            }

            return Ok(token);
        }
    }
}