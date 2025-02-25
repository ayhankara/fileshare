using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using SecureFileStorage.Models;
using SecureFileStorage.Services;
using SecureFileStorage.Application.DTOs;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SecureFileStorage.Controller
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class FoldersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly AzureBlobStorageService _blobStorageService;
        private readonly ILogger<FoldersController> _logger;

        public FoldersController(
            ApplicationDbContext context,
            AzureBlobStorageService blobStorageService,
            ILogger<FoldersController> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _blobStorageService = blobStorageService ?? throw new ArgumentNullException(nameof(blobStorageService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <summary>
        /// Yeni bir klasör oluşturur.
        /// </summary>
        [HttpPost("create")]
        public async Task<IActionResult> CreateFolder([FromBody] FolderCreateDto folderCreateDto)
        {
            if (folderCreateDto == null || string.IsNullOrWhiteSpace(folderCreateDto.Name))
            {
                _logger.LogWarning("Geçersiz klasör oluşturma isteği: Klasör adı boş veya null.");
                return BadRequest("Klasör adı gereklidir.");
            }

            try
            {
                var newFolder = new Folder
                {
                    Name = folderCreateDto.Name,
                    ParentFolderId = folderCreateDto.ParentFolderId,
                    OwnerId = GetCurrentUserId()
                };

                _context.Folders.Add(newFolder);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Klasör başarıyla oluşturuldu. Klasör ID: {FolderId}, Klasör Adı: {FolderName}", newFolder.Id, newFolder.Name);
                return CreatedAtAction(nameof(GetFolder), new { id = newFolder.Id }, new { message = "Klasör başarıyla oluşturuldu.", folderId = newFolder.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Klasör oluşturulurken bir hata oluştu. Klasör Adı: {FolderName}", folderCreateDto.Name);
                return StatusCode(500, "Klasör oluşturulurken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Belirli bir klasörün içeriğini (dosyalar ve alt klasörler) listeler.
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetFolder(int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning("Geçersiz klasör ID: {FolderId}", id);
                return BadRequest("Geçersiz klasör ID.");
            }

            try
            {
                var folder = await _context.Folders.FindAsync(id);

                if (folder == null)
                {
                    _logger.LogWarning("Klasör bulunamadı. Klasör ID: {FolderId}", id);
                    return NotFound("Klasör bulunamadı.");
                }

                var files = await _context.Files
                    .Where(f => f.FolderId == id)
                    .Select(f => new { f.Id, f.Name, f.Type, f.Size })
                    .ToListAsync();

                var subfolders = await _context.Folders
                    .Where(f => f.ParentFolderId == id)
                    .Select(f => new { f.Id, f.Name })
                    .ToListAsync();

                _logger.LogInformation("Klasör içeriği başarıyla alındı. Klasör ID: {FolderId}", id);
                return Ok(new { files, folders = subfolders });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Klasör içeriği alınırken bir hata oluştu. Klasör ID: {FolderId}", id);
                return StatusCode(500, "Klasör içeriği alınırken bir hata oluştu.");
            }
        }

        /// <summary>
        /// Belirli bir klasörü ve içeriğini (dosyalar ve alt klasörler) siler.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteFolder(int id)
        {
            if (id <= 0)
            {
                _logger.LogWarning("Geçersiz klasör ID: {FolderId}", id);
                return BadRequest("Geçersiz klasör ID.");
            }

            try
            {
                var folder = await _context.Folders.FindAsync(id);

                if (folder == null)
                {
                    _logger.LogWarning("Klasör bulunamadı. Klasör ID: {FolderId}", id);
                    return NotFound("Klasör bulunamadı.");
                }

                // Alt klasörleri ve dosyaları silmek için recursive bir fonksiyon kullanmak daha güvenli ve performanslı olabilir.
                await DeleteFolderRecursive(id);

                _logger.LogInformation("Klasör ve içeriği başarıyla silindi. Klasör ID: {FolderId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Klasör silinirken bir hata oluştu. Klasör ID: {FolderId}", id);
                return StatusCode(500, "Klasör silinirken bir hata oluştu.");
            }
        }

        private async Task DeleteFolderRecursive(int folderId)
        {
            // Dosyaları sil
            var filesInFolder = await _context.Files.Where(f => f.FolderId == folderId).ToListAsync();
            foreach (var file in filesInFolder)
            {
                try
                {
                    await _blobStorageService.DeleteFileAsync(file.BlobId = "");
                    _logger.LogInformation("Dosya başarıyla silindi. Dosya ID: {FileId}, Blob ID: {BlobId}", file.Id, file.BlobId);
                }
                catch (Exception blobEx)
                {
                    _logger.LogError(blobEx, "Blob silinirken hata oluştu. Dosya ID: {FileId}, Blob ID: {BlobId}", file.Id, file.BlobId);
                }
                _context.Files.Remove(file);
            }

            // Alt klasörleri sil (recursive olarak)
            var subfolders = await _context.Folders.Where(f => f.ParentFolderId == folderId).ToListAsync();
            foreach (var subfolder in subfolders)
            {
                await DeleteFolderRecursive(subfolder.Id);
                _context.Folders.Remove(subfolder);
            }

            // Klasörü sil
            var folder = await _context.Folders.FindAsync(folderId);
            if (folder != null)
            {
                _context.Folders.Remove(folder);
            }

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// Mevcut kullanıcının ID'sini alır.
        /// </summary>
        private int GetCurrentUserId()
        {
            // TODO: Kullanıcı ID'sini Claims'lerden alın.
            var userIdClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }

            _logger.LogError("Kullanıcı ID'si alınamadı.");
            throw new UnauthorizedAccessException("Kullanıcı ID'si alınamadı.");
        }
    }
}