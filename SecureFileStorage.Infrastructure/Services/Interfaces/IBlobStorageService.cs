namespace SecureFileStorage.Infrastructure.Services.Interfaces
{
    public interface IBlobStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
        Task<Stream?> DownloadFileAsync(string fileName);
        Task<bool> DeleteFileAsync(string fileName);
        Task<string?> GetFileUrlAsync(string fileName);
    }
}
