using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace Service.Interface
{
    public interface ICloudinaryService
    {
        Task<string?> UploadImageAsync(IFormFile file);
        Task<string?> UploadImageFromUrlAsync(string imageUrl);
    }
}
