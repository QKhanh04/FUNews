using CloudinaryDotNet;
using CloudinaryDotNet.Actions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Service.Interface;
using System;
using System.Threading.Tasks;

namespace Service.Implement
{
    public class CloudinaryService : ICloudinaryService
    {
        private readonly Cloudinary _cloudinary;

        public CloudinaryService(IConfiguration config)
        {
            var acc = new Account(
                config["CloudinarySettings:CloudName"],
                config["ApiKey"] ?? config["CloudinarySettings:ApiKey"],
                config["ApiSecret"] ?? config["CloudinarySettings:ApiSecret"]
            );
            _cloudinary = new Cloudinary(acc);
        }

        public async Task<string?> UploadImageAsync(IFormFile file)
        {
            if (file == null || file.Length == 0) return null;

            using var stream = file.OpenReadStream();
            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(file.FileName, stream),
                Folder = "fu-news-v2",
                Transformation = new Transformation().Width(800).Crop("limit").Quality("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new Exception(uploadResult.Error.Message);
            }

            return uploadResult.SecureUrl.ToString();
        }
        public async Task<string?> UploadImageFromUrlAsync(string imageUrl)
        {
            if (string.IsNullOrEmpty(imageUrl)) return null;

            var uploadParams = new ImageUploadParams
            {
                File = new FileDescription(imageUrl),
                Folder = "fu-news-v2",
                Transformation = new Transformation().Width(800).Crop("limit").Quality("auto")
            };

            var uploadResult = await _cloudinary.UploadAsync(uploadParams);

            if (uploadResult.Error != null)
            {
                throw new Exception(uploadResult.Error.Message);
            }

            return uploadResult.SecureUrl.ToString();
        }
    }
}
