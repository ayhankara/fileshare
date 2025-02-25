using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SecureFileStorage.Infrastructure.Services.Interfaces;

namespace SecureFileStorage.Services
{

    public class AzureBlobStorageService : IStorageService
    {
        private readonly string _connectionString;
        private readonly string _containerName;
        private readonly BlobServiceClient _blobServiceClient; 
        private readonly ApplicationDbContext _context;

        public AzureBlobStorageService(IConfiguration configuration, ApplicationDbContext context)
        {
            _connectionString = configuration.GetSection("AzureBlobStorage:ConnectionString").Value ?? ""; 
            _containerName = configuration.GetSection("AzureBlobStorage:ContainerName").Value ?? ""; 
            _blobServiceClient = new BlobServiceClient(_connectionString); 
            _context = context;
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.Blob);

            var blobClient = containerClient.GetBlobClient(fileName);
            var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };

            BlobUploadOptions blobUploadOptions = new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
            };

            await blobClient.UploadAsync(fileStream, blobHttpHeaders);

            return blobClient.Uri.ToString(); // Dosyanın erişim URL'sini döndür
        }

        public async Task<Stream?> DownloadFileAsync(string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            if (await blobClient.ExistsAsync())
            {
                //var response = await blobClient.OpenReadAsync();
                //return response;
                BlobDownloadResult downloadResult = await blobClient.DownloadContentAsync();
                return downloadResult.Content.ToStream();
            }

            //  throw new FileNotFoundException("File not found in Azure Blob Storage.");
            return  null;
        }

        public async Task<bool> DeleteFileAsync(string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            return await blobClient.DeleteIfExistsAsync();
        }
        public bool HasPermission(int fileId, int userId, string permissionName)
        {
            // Gerekli iznin adını al
            var requiredPermission = _context.Permissions
                .FirstOrDefault(p => p.Name == permissionName);

            if (requiredPermission == null)
            {
                return false; // İzin bulunamadı
            }

            // Dosya sahibinin otomatik olarak tüm izinlere sahip olduğunu kontrol et
            var file = _context.Files.FirstOrDefault(f => f.Id == fileId);
            if (file != null && file.OwnerId == userId)
            {
                return true; // Dosya sahibi otomatik olarak bu izne sahip
            }

            // Kullanıcının dosya üzerinde doğrudan sahip olduğu izni kontrol et
            var fileUserPermission = _context.FileUserPermissions
                .FirstOrDefault(fup => fup.FileId == fileId && fup.UserId == userId && fup.PermissionId == requiredPermission.Id);

            if (fileUserPermission != null)
            {
                return true; // Kullanıcının doğrudan bu izni var
            }

            // Kullanıcının rollerini al
            var userRoles = _context.UserRoles
                .Where(ur => ur.UserId == userId)
                .Select(ur => ur.RoleId)
                .ToList();

            // Rollerin dosya üzerinde sahip olduğu izinleri al
            var fileRolePermissions = _context.FileRolePermissions
                .Where(frp => frp.FileId == fileId && userRoles.Contains(frp.RoleId) && frp.PermissionId == requiredPermission.Id)
                .ToList();

            if (fileRolePermissions.Any())
            {
                return true; // Kullanıcının rolü aracılığıyla bu izni var
            }

            // İzin bulunamadı
            return false;
        }
        public async Task<string?> GenerateFileUrlAsync(string fileName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            var blobClient = containerClient.GetBlobClient(fileName);

            if (await blobClient.ExistsAsync())
            {
                return blobClient.Uri.ToString();
            }

            return "File not found in Azure Blob Storage.";
           // throw new FileNotFoundException("File not found in Azure Blob Storage.");
        }


        public string GetFileUrl(string blobId)
        {
            BlobServiceClient blobServiceClient = new BlobServiceClient(_connectionString);
            BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(_containerName);
            BlobClient blobClient = blobContainerClient.GetBlobClient(blobId);

            return blobClient.Uri.ToString(); // Blob'un URL'sini döndür
        }
    }
}
