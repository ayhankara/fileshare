
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using SecureFileStorage.Infrastructure.Services.Interfaces;
namespace SecureFileStorage.Infrastructure.Services
{
    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName = "files"; // Azure'daki konteyner adı

        public BlobStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"];
            _blobServiceClient = new BlobServiceClient(connectionString);
        }

        public async Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType)
        {
            try
            {
                var blobContainer = _blobServiceClient.GetBlobContainerClient(_containerName);
                await blobContainer.CreateIfNotExistsAsync();
                var blobClient = blobContainer.GetBlobClient(fileName);

                var blobHttpHeaders = new BlobHttpHeaders { ContentType = contentType };
                await blobClient.UploadAsync(fileStream, blobHttpHeaders);

                return blobClient.Uri.ToString();
            }
            catch (Exception ex)
            {
                // Log mekanizmasına bağlanabilir
                throw new Exception("Dosya yüklenirken hata oluştu: " + ex.Message);
            }
        }

        public async Task<Stream?> DownloadFileAsync(string fileName)
        {
            try
            {
                var blobContainer = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = blobContainer.GetBlobClient(fileName);

                if (await blobClient.ExistsAsync())
                {
                    var response = await blobClient.DownloadAsync();
                    return response.Value.Content;
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Dosya indirilirken hata oluştu: " + ex.Message);
            }
        }

        public async Task<bool> DeleteFileAsync(string fileName)
        {
            try
            {
                var blobContainer = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = blobContainer.GetBlobClient(fileName);

                return await blobClient.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Dosya silinirken hata oluştu: " + ex.Message);
            }
        }

        public async Task<string?> GetFileUrlAsync(string fileName)
        {
            try
            {
                var blobContainer = _blobServiceClient.GetBlobContainerClient(_containerName);
                var blobClient = blobContainer.GetBlobClient(fileName);

                if (await blobClient.ExistsAsync())
                {
                    return blobClient.Uri.ToString();
                }
                return null;
            }
            catch (Exception ex)
            {
                throw new Exception("Dosya URL alınırken hata oluştu: " + ex.Message);
            }
        }
    }
}

