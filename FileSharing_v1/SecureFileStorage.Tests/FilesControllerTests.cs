using Microsoft.AspNetCore.Mvc;
using Moq;
using SecureFileStorage.Controller;
using SecureFileStorage.Infrastructure.Services.Interfaces;
using Xunit;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using SecureFileStorage.Models;
using AutoMapper;
using SecureFileStorage.Application.DTOs;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;

namespace SecureFileStorage.Tests
{

   
    public class FilesControllerTests
    {
        private readonly Mock<IStorageService> _azureBlobStorageServiceMock; // Local değil Azure'yi kullanıyoruz
        private readonly Mock<ApplicationDbContext> _contextMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<FilesController>> _loggerMock;
        private readonly Mock<IMemoryCache> _memoryCacheMock;
        private readonly FilesController _controller;

        public FilesControllerTests()
        {
            _azureBlobStorageServiceMock = new Mock<IStorageService>(); // Sadece Azure'yi mockluyoruz
            _contextMock = new Mock<ApplicationDbContext>(new DbContextOptions<ApplicationDbContext>());
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<FilesController>>();
            _memoryCacheMock = new Mock<IMemoryCache>();

            // Mock Authorization
            var authServiceMock = new Mock<IAuthorizationService>();
            authServiceMock.Setup(x => x.AuthorizeAsync(It.IsAny<ClaimsPrincipal>(), It.IsAny<object>(), It.IsAny<IEnumerable<IAuthorizationRequirement>>()))
                .ReturnsAsync(AuthorizationResult.Success());

            // ControllerContext oluştur
            var controllerContext = new ControllerContext(new ActionContext(new DefaultHttpContext(), new RouteData(), new ControllerActionDescriptor()));

            _controller = new FilesController(
                _contextMock.Object,
                _azureBlobStorageServiceMock.Object, // Sadece Azure'yi kullanıyoruz
                _mapperMock.Object,
                _loggerMock.Object,
                _memoryCacheMock.Object
            )
            {
                ControllerContext = controllerContext
            };
        }

        [Fact]
        public async Task UploadFile_ValidFile_ReturnsOkResult()
        {
            // Arrange
            var fileUploadDto = new FileUploadDto
            {
                File = new FormFile(Stream.Null, 0, 10, "file", "test.txt")
            };

            // Act
            var result = await _controller.UploadFile(fileUploadDto);

            // Assert
            result.Should().BeOfType<OkObjectResult>();
        }

        [Fact]
        public async Task UploadFile_NullFile_ReturnsBadRequestResult()
        {
            // Arrange
            var fileUploadDto = new FileUploadDto();

            // Act
            var result = await _controller.UploadFile(fileUploadDto);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
