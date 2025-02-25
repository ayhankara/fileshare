using AutoMapper;
using SecureFileStorage.Application.DTOs;


namespace SecureFileStorage.Models.Map
{
    public class AutoMapperProfile : Profile
    {
        public AutoMapperProfile()
        {
            CreateMap<File, FileDto>();
            CreateMap<FileUploadDto, File>();
        }
    }
}
