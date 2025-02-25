using Microsoft.AspNetCore.Http;

namespace SecureFileStorage.Application.DTOs
{
    public class FileUploadDto
    {
        public IFormFile? File { get; set; }
        public int FolderId { get; set; }
    }


}
