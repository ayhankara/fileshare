using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SecureFileStorage.Infrastructure.Services.Interfaces;
using System;
using System.IO;
using System.Threading.Tasks;

namespace SecureFileStorage.Services
{
    public class LocalFileStorageService : IStorageService
    {
        private readonly string _storagePath;
        private readonly ILogger<LocalFileStorageService> _logger;

        public LocalFileStorageService(IConfiguration configuration, ILogger<LocalFileStorageService> logger)
        {
            _storagePath = configuration.GetSection("LocalFileStorage:StoragePath").Value ?? "";
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));

            // Klasörün var olup olmadığını kontrol et
            if (!Directory.Exists(_storagePath))
            {
                try
                {
                    Directory.CreateDirectory(_storagePath);
                    _logger.LogInformation("Depolama klasörü oluşturuldu: {StoragePath}", _storagePath);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Depolama klasörü oluşturulurken bir hata oluştu: {StoragePath}", _storagePath);
                    throw; // Uygulamanın başlamasını engellemek için istisnayı yeniden fırlat
                }
            }
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var filePath = Path.Combine(_storagePath, fileName);
            try
            {
                using (var file = new FileStream(filePath, FileMode.Create))
                {
                    await fileStream.CopyToAsync(file);
                }
                _logger.LogInformation("Dosya başarıyla yüklendi: {FilePath}", filePath);
                return fileName; // Dosya adını döndür
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya yüklenirken bir hata oluştu: {FilePath}", filePath);
                throw; // İstisnayı yeniden fırlat
            }
        }

        public async Task<Stream?> DownloadFileAsync(string fileName)
        {
            var filePath = Path.Combine(_storagePath, fileName);
            try
            {
                if (!File.Exists(filePath))
                {
                    _logger.LogWarning("Dosya bulunamadı: {FilePath}", filePath);
                    return null;
                }

                // Dosyayı asenkron olarak aç
                var stream = await Task.Run(() => File.OpenRead(filePath));
                _logger.LogInformation("Dosya başarıyla indirildi: {FilePath}", filePath);
                return stream;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya indirilirken bir hata oluştu: {FilePath}", filePath);
                return null; // Hata durumunda null döndür
            }
        }

        public async Task<bool> DeleteFileAsync(string fileName)
        {
            var filePath = Path.Combine(_storagePath, fileName);
            try
            {
                if (File.Exists(filePath))
                {
                    // Dosyayı asenkron olarak sil
                    await Task.Run(() => File.Delete(filePath));
                    _logger.LogInformation("Dosya başarıyla silindi: {FilePath}", filePath);
                    return true;
                }
                else
                {
                    _logger.LogWarning("Silinmek istenen dosya bulunamadı: {FilePath}", filePath);
                    return false; // Dosya bulunamadığında false döndür
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Dosya silinirken bir hata oluştu: {FilePath}", filePath);
                return false; // Hata durumunda false döndür
            }
        }
        public string GenerateUniqueFileName(string originalFileName)
        {
            string fileExtension = Path.GetExtension(originalFileName);
            string uniqueFileName = $"{Guid.NewGuid()}{fileExtension}";
            _logger.LogDebug("Benzersiz dosya adı oluşturuldu: {UniqueFileName}, Orijinal dosya adı: {OriginalFileName}", uniqueFileName, originalFileName);
            return uniqueFileName;
        }

        public Task<string?> GenerateFileUrlAsync(string fileName)
        {
            string fileUrl = $"/uploads/{fileName}";
            _logger.LogDebug("Dosya URL'si oluşturuldu: {FileUrl}, Dosya adı: {FileName}", fileUrl, fileName);
            return Task.FromResult<string?>(fileUrl);
        }

        public bool HasPermission(int fileId, int userId, string permissionName)
        {
            _logger.LogWarning("Yerel dosya depolama için izin kontrolü uygulanmadı. Dosya ID: {FileId}, Kullanıcı ID: {UserId}, İzin Adı: {PermissionName}", fileId, userId, permissionName);
            throw new NotImplementedException("Yerel dosya depolama için izin kontrolü uygulanmadı.");
        }

        public string GetFileUrl(string blobId)
        {
            _logger.LogWarning("Yerel dosya depolama için GetFileUrl metodu uygulanmadı. Blob ID: {BlobId}", blobId);
            throw new NotImplementedException("Yerel dosya depolama için GetFileUrl metodu uygulanmadı.");
        }
    }
}