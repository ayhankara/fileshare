namespace SecureFileStorage.Infrastructure.Services.Interfaces
{
    public interface IStorageService
    {
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType);
        Task<Stream?> DownloadFileAsync(string fileName);
        Task<bool> DeleteFileAsync(string fileName);
        Task<string?> GenerateFileUrlAsync(string fileName);
        bool HasPermission(int fileId, int userId, string permissionName);

        string GetFileUrl(string blobId);
    }
}
