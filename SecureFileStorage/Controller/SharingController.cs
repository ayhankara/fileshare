using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using SecureFileStorage.Models;
using SecureFileStorage.Services;
using SecureFileStorage.Application.DTOs;

namespace SecureFileStorage.Controller
{
    [Authorize] // Kimlik doğrulama gerektir
    [ApiController]
    [Route("api/[controller]")]
    public class SharingController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly AzureBlobStorageService _blobStorageService; // Blob storage servisi
        private readonly ILogger<SharingController> _logger;

        public SharingController(
            ApplicationDbContext context,
            AzureBlobStorageService blobStorageService,
            ILogger<SharingController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Dosya paylaşma endpoint'i.
        /// </summary>
        /// <returns>Paylaşım işleminin sonucu.</returns>
        [HttpPost("share")]
        public async Task<IActionResult> ShareFile([FromBody] ShareDto shareDto)
        {
            if (shareDto == null || shareDto.FileId <= 0 || shareDto.SharedWithUserId <= 0)
            {
                _logger.LogWarning("Geçersiz paylaşım bilgileri.");
                return BadRequest("Geçersiz paylaşım bilgileri.");
            }

            try
            {
                var file = await _context.Files.FindAsync(shareDto.FileId);
                var sharedWithUser = await _context.Users.FindAsync(shareDto.SharedWithUserId);

                if (file == null || sharedWithUser == null)
                {
                    _logger.LogWarning("Dosya veya kullanıcı bulunamadı. Dosya ID: {FileId}, Kullanıcı ID: {UserId}", shareDto.FileId, shareDto.SharedWithUserId);
                    return NotFound("Dosya veya kullanıcı bulunamadı.");
                }

                var newShare = new SharedFile
                {
                    FileId = shareDto.FileId,
                    SharedWithUserId = shareDto.SharedWithUserId,
                    SharedDate = DateTime.UtcNow, // Paylaşım tarihini ekledim
                    ShareLink = GenerateShareLink() // Paylaşım linki oluştur
                };

                _context.SharedFiles.Add(newShare);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Dosya başarıyla paylaşıldı. Dosya ID: {FileId}, Paylaşılan Kullanıcı ID: {UserId}", shareDto.FileId, shareDto.SharedWithUserId);
                return CreatedAtAction(nameof(GetSharedFiles), new { userId = shareDto.SharedWithUserId }, new { message = "Dosya başarıyla paylaşıldı.", shareId = newShare.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya paylaşılırken bir hata oluştu. Dosya ID: {FileId}, Paylaşılan Kullanıcı ID: {UserId}", shareDto.FileId, shareDto.SharedWithUserId);
                return StatusCode(500, "Dosya paylaşılırken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Belirli bir kullanıcıyla paylaşılan dosyaları listeler.
        /// </summary>
        /// <returns>Paylaşılan dosyaların listesi.</returns>
        [HttpGet("sharedWithMe/{userId}")]
        public async Task<IActionResult> GetSharedFiles(int userId)
        {
            if (userId <= 0)
            {
                _logger.LogWarning("Geçersiz kullanıcı ID.");
                return BadRequest("Geçersiz kullanıcı ID.");
            }

            try
            {
                var sharedFiles = await _context.SharedFiles
                    .Where(sf => sf.SharedWithUserId == userId)
                    .Include(sf => sf.File) // Dosya bilgilerini de getir
                    .ToListAsync();

                var result = sharedFiles.Select(sf => new
                {
                    sf.Id,
                    sf.FileId,
                    FileName = sf.File != null ? sf.File.Name : "Bilinmeyen Dosya", // Null kontrolü ve varsayılan değer
                    sf.SharedDate, // Paylaşım tarihini ekledim
                    ShareLink = sf.ShareLink // Paylaşım linkini ekledim
                }).ToList();

                _logger.LogInformation("Kullanıcıyla paylaşılan dosyalar başarıyla alındı. Kullanıcı ID: {UserId}", userId);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Paylaşılan dosyalar alınırken bir hata oluştu. Kullanıcı ID: {UserId}", userId);
                return StatusCode(500, "Paylaşılan dosyalar alınırken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Belirli bir paylaşımı siler.
        /// </summary>
        /// <returns>Silme işleminin sonucu.</returns>
        [HttpDelete("share/{id}")]
        public async Task<IActionResult> DeleteShare(int id)
        {
            try
            {
                var share = await _context.SharedFiles.FindAsync(id);

                if (share == null)
                {
                    _logger.LogWarning("Paylaşım bilgisi bulunamadı. Paylaşım ID: {ShareId}", id);
                    return NotFound("Paylaşım bilgisi bulunamadı.");
                }

                _context.SharedFiles.Remove(share);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Paylaşım başarıyla silindi. Paylaşım ID: {ShareId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Paylaşım silinirken bir hata oluştu. Paylaşım ID: {ShareId}", id);
                return StatusCode(500, "Paylaşım silinirken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Belirli bir dosyayı siler.
        /// </summary>
        /// <returns>Silme işleminin sonucu.</returns>
        [HttpDelete("file/{id}")]
        public async Task<IActionResult> DeleteFile(int id)
        {
            try
            {
                var file = await _context.Files.FindAsync(id);

                if (file == null)
                {
                    _logger.LogWarning("Dosya bulunamadı. Dosya ID: {FileId}", id);
                    return NotFound("Dosya bulunamadı.");
                }

                await _blobStorageService.DeleteFileAsync(file.BlobId = "");

                _context.Files.Remove(file);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Dosya başarıyla silindi. Dosya ID: {FileId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya silinirken bir hata oluştu. Dosya ID: {FileId}", id);
                return StatusCode(500, "Dosya silinirken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Güvenli ve benzersiz bir paylaşım linki oluşturur.
        /// </summary>
        /// <returns>Oluşturulan paylaşım linki.</returns>
        private string GenerateShareLink()
        {
            //TODO :
            return Guid.NewGuid().ToString(); // Geçici olarak Guid kullanılıyor, daha güvenli bir yöntemle değiştirilebilir
        }
    }
}