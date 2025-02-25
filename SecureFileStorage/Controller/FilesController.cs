using AutoMapper;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using SecureFileStorage.Application.DTOs;
using SecureFileStorage.Domain.ModelPermissions;
using SecureFileStorage.Infrastructure.Services.Interfaces;
using SecureFileStorage.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SecureFileStorage.Controller
{
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme + ",JwtBearer")]
    [ApiController]
    [Route("api/[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly IStorageService _azureBlobStorageService;
        private readonly ILogger<FilesController> _logger;
        private readonly IMemoryCache _cache;
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;

        public FilesController(
            ApplicationDbContext context,
            [FromKeyedServices("AzureBlobStorage")] IStorageService azureBlobStorageService,
            IMapper mapper,
            ILogger<FilesController> logger,
            IMemoryCache cache)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _azureBlobStorageService = azureBlobStorageService ?? throw new ArgumentNullException(nameof(azureBlobStorageService));
            _mapper = mapper ?? throw new ArgumentNullException(nameof(mapper));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        }

        /// <summary>
        /// Dosya yükleme endpoint'i.
        /// </summary>
        /// <returns>Yüklenen dosyanın bilgileri.</returns>
        [HttpPost("upload")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> UploadFile([FromForm] FileUploadDto fileUploadDto)
        {
            if (fileUploadDto?.File == null || fileUploadDto.File.Length == 0)
            {
                _logger.LogWarning("Dosya yükleme başarısız: Dosya yok veya boş.");
                return BadRequest("Dosya yüklenemedi.");
            }

            try
            {
                var uniqueFileName = $"{Guid.NewGuid()}_{fileUploadDto.File.FileName}";

                using (var stream = fileUploadDto.File.OpenReadStream())
                {
                    await _azureBlobStorageService.UploadFileAsync(stream, uniqueFileName, fileUploadDto.File.ContentType);
                }

                var newFile = _mapper.Map<Models.File>(fileUploadDto);
                newFile.Name = fileUploadDto.File.FileName;
                newFile.Type = fileUploadDto.File.ContentType;
                newFile.Size = fileUploadDto.File.Length;
                newFile.UploadDate = DateTime.UtcNow;
                newFile.OwnerId = GetCurrentUserId();
                newFile.BlobId = uniqueFileName;
                newFile.Path = uniqueFileName;
                newFile.IsActive = 1;
                newFile.IsDelete = 0;
                newFile.CreateDate = DateTime.Now;
                newFile.FolderId = fileUploadDto.FolderId;

                _context.Files.Add(newFile);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Dosya başarıyla yüklendi: {FileName}", fileUploadDto.File.FileName);

                var fileDto = _mapper.Map<FileDto>(newFile);
                return Ok(fileDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya yüklenirken bir hata oluştu: {FileName}", fileUploadDto.File.FileName);
                return StatusCode(500, "Dosya yüklenirken bir hata oluştu.");
            }
        }

        [HttpGet("download/{id}")]
        public async Task<IActionResult> DownloadFile(int id)
        {
            try
            {
                var file = await _context.Files.FindAsync(id);

                if (file == null)
                {
                    _logger.LogWarning("Dosya bulunamadı: {FileId}", id);
                    return NotFound("Dosya bulunamadı.");
                }

                var userId = GetCurrentUserId();
                if (!_azureBlobStorageService.HasPermission(id, userId, "Dosya Okuma"))
                {
                    _logger.LogWarning("Kullanıcının dosya indirme izni yok: {UserId}, {FileId}", userId, id);
                    return StatusCode(StatusCodes.Status403Forbidden, "Dosyayı indirme izniniz yok.");
                }

                var stream = await _azureBlobStorageService.DownloadFileAsync(file.BlobId??"");

                if (stream == null)
                {
                    _logger.LogError("Dosya Azure Blob Storage'da bulunamadı: {FileId}, {BlobId}", id, file.BlobId);
                    return NotFound("Dosya Azure Blob Storage'da bulunamadı.");
                }

                _logger.LogInformation("Dosya başarıyla indirildi: {FileId}, {FileName}", id, file.Name);
                return File(stream, file.Type = "", file.Name);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya indirilirken bir hata oluştu: {FileId}", id);
                return StatusCode(500, "Dosya indirilirken bir hata oluştu.");
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFile(int id)
        {
            try
            {
                var file = await _context.Files.FindAsync(id);

                if (file == null)
                {
                    _logger.LogWarning("Dosya bulunamadı: {FileId}", id);
                    return NotFound("Dosya bulunamadı.");
                }

                var userId = GetCurrentUserId();
                if (!_azureBlobStorageService.HasPermission(id, userId, "Dosya Silme"))
                {
                    _logger.LogWarning("Kullanıcının dosya silme izni yok: {UserId}, {FileId}", userId, id);
                    return StatusCode(StatusCodes.Status403Forbidden, "Dosyayı silme izniniz yok.");
                }

                await _azureBlobStorageService.DeleteFileAsync(file.BlobId = "");
                _context.Files.Remove(file);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Dosya başarıyla silindi: {FileId}, {FileName}", id, file.Name);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya silinirken bir hata oluştu: {FileId}", id);
                return StatusCode(500, "Dosya silinirken bir hata oluştu.");
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateFile(int id, [FromBody] FileUpdateDto fileUpdateDto)
        {
            if (fileUpdateDto == null || id <= 0)
            {
                _logger.LogWarning("Geçersiz istek: FileUpdateDto null veya ID geçersiz.");
                return BadRequest("Geçersiz istek.");
            }

            try
            {
                var file = await _context.Files.FindAsync(id);

                if (file == null)
                {
                    _logger.LogWarning("Dosya bulunamadı: {FileId}", id);
                    return NotFound("Dosya bulunamadı.");
                }

                var userId = GetCurrentUserId();
                if (!_azureBlobStorageService.HasPermission(id, userId, "Dosya Düzenleme"))
                {
                    _logger.LogWarning("Kullanıcının dosya düzenleme izni yok: {UserId}, {FileId}", userId, id);
                    return StatusCode(StatusCodes.Status403Forbidden, "Dosyayı düzenleme izniniz yok.");
                }

                file.Name = fileUpdateDto.Name ?? file.Name;
                file.FolderId = fileUpdateDto.FolderId ?? file.FolderId;

                _context.Files.Update(file);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Dosya başarıyla güncellendi: {FileId}, {FileName}", id, file.Name);
                return Ok(new { message = "Dosya başarıyla güncellendi.", fileId = file.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya güncellenirken bir hata oluştu: {FileId}", id);
                return StatusCode(500, "Dosya güncellenirken bir hata oluştu.");
            }
        }

        [HttpPost("share")]
        public async Task<IActionResult> ShareFile([FromBody] ShareFileDto shareFileDto)
        {
            try
            {
                var file = await _context.Files.FindAsync(shareFileDto.FileId);
                if (file == null)
                {
                    _logger.LogWarning("Dosya bulunamadı: {FileId}", shareFileDto.FileId);
                    return NotFound("Dosya bulunamadı.");
                }

                var currUserId = GetCurrentUserId();
                if (file.OwnerId != currUserId)
                {
                    _logger.LogWarning("Kullanıcının dosyayı paylaşma izni yok: {UserId}, {FileId}", currUserId, shareFileDto.FileId);
                    return StatusCode(StatusCodes.Status403Forbidden, "Bu dosyayı paylaşma izniniz yok.");
                }

                var sharedWithUser = await _context.Users.FindAsync(shareFileDto.SharedWithUserId);
                if (sharedWithUser == null)
                {
                    _logger.LogWarning("Paylaşılacak kullanıcı bulunamadı: {UserId}", shareFileDto.SharedWithUserId);
                    return NotFound("Paylaşılacak kullanıcı bulunamadı.");
                }

                var readPermission = await _context.Permissions.FirstOrDefaultAsync(p => p.Name == "Dosya Okuma");
                if (readPermission == null)
                {
                    _logger.LogError("Dosya Okuma izni bulunamadı.");
                    return StatusCode(StatusCodes.Status500InternalServerError, "Dosya Okuma izni bulunamadı.");
                }

                // Mevcut izinleri temizle
                var existingPermissions = await _context.FileUserPermissions
                    .Where(fup => fup.FileId == shareFileDto.FileId && fup.UserId == shareFileDto.SharedWithUserId)
                    .ToListAsync();

                _context.FileUserPermissions.RemoveRange(existingPermissions);

                // Yeni izni ekle
                var newFileUserPermission = new FileUserPermission
                {
                    FileId = shareFileDto.FileId,
                    UserId = shareFileDto.SharedWithUserId,
                    PermissionId = readPermission.Id
                };
                _context.FileUserPermissions.Add(newFileUserPermission);

                // Paylaşım kaydını oluştur
                var fileUrl = _azureBlobStorageService.GetFileUrl(file.BlobId = "");
                var sharedFile = new SharedFile
                {
                    FileId = shareFileDto.FileId,
                    SharedByUserId = currUserId,
                    SharedWithUserId = shareFileDto.SharedWithUserId,
                    SharedDate = DateTime.UtcNow,
                    ShareLink = fileUrl
                };
                _context.SharedFiles.Add(sharedFile);

                await _context.SaveChangesAsync();

                _logger.LogInformation("Dosya başarıyla paylaşıldı: {FileId}, {SharedWithUserId}", shareFileDto.FileId, shareFileDto.SharedWithUserId);
                return Ok(new { message = "Dosya başarıyla paylaşıldı.", shareLink = fileUrl });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya paylaşılırken bir hata oluştu: {FileId}, {SharedWithUserId}", shareFileDto.FileId, shareFileDto.SharedWithUserId);
                return StatusCode(500, "Dosya paylaşılırken bir hata oluştu.");
            }
        }

        [HttpGet("myfiles")]
        public async Task<IActionResult> GetMyFiles()
        {
            var userId = GetCurrentUserId();
            var cacheKey = $"MyFiles_{userId}";

            if (!_cache.TryGetValue(cacheKey, out List<FileDto>? files))
            {
                _logger.LogInformation("Dosyalar cache'de bulunamadı, veritabanından getiriliyor. Kullanıcı: {UserId}", userId);

                files = await _context.Files
                    .Where(f => f.OwnerId == userId)
                    .Select(f => new FileDto
                    {
                        Id = f.Id,
                        Name = f.Name,
                        Type = f.Type,
                        Size = f.Size,
                    }).ToListAsync();

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromMinutes(10)); // 10 dakika boyunca cache'de tut

                _cache.Set(cacheKey, files, cacheEntryOptions);
                _logger.LogInformation("Dosyalar cache'e kaydedildi. Kullanıcı: {UserId}", userId);
            }
            else
            {
                _logger.LogInformation("Dosyalar cache'den getirildi. Kullanıcı: {UserId}", userId);
            }

            return Ok(files);
        }

        [HttpGet("sharedwithme")]
        public async Task<IActionResult> GetSharedWithMe()
        {
            var userId = GetCurrentUserId();

            var sharedFiles = await _context.SharedFiles
                .Include(sf => sf.File)
                .Where(sf => sf.SharedWithUserId == userId)
                .ToListAsync();

            var sharedFileDtos = sharedFiles.Select(sf => new SharedFileDto
            {
                FileId = sf.FileId,
                FileName = sf.File != null ? sf.File.Name : "Bilinmeyen Dosya", // Null kontrolü ve varsayılan değer
                SharedByUserId = sf.SharedByUserId,
                SharedDate = sf.SharedDate
            }).ToList();

            return Ok(sharedFileDtos);
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);

            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }

            _logger.LogError("Kullanıcı ID'si bulunamadı.");
            throw new UnauthorizedAccessException("Kullanıcı ID'si bulunamadı.");
        }
    }
}