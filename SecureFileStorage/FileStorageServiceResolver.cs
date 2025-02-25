using SecureFileStorage.Infrastructure.Services.Interfaces;
using SecureFileStorage.Services;



namespace SecureFileStorage
{
    public class FileStorageServiceResolver
    {
        private readonly IServiceProvider _serviceProvider;

        public FileStorageServiceResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public IStorageService GetStorageService(string storageType)
        {
            switch (storageType)
            {
                case "AzureBlob":
                    return _serviceProvider.GetRequiredService<AzureBlobStorageService>();
                case "Local":
                    return _serviceProvider.GetRequiredService<LocalFileStorageService>();
                default:
                    throw new ArgumentException($"Unsupported storage type: {storageType}");
            }
        }
    }
}
