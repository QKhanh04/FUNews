using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace FUNewsManagement.Services
{
    public interface ICloudinaryService
    {
        Task<string?> UploadImageAsync(IFormFile file);
        Task<string?> UploadImageFromUrlAsync(string imageUrl);
    }
}
