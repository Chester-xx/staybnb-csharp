namespace staybnb_csharp.Services
{
    public interface IFileStorageService
    {
        const int MaxImagesPerApplication = 10;

        /// <summary>Returns an error message, or null when the image is acceptable.</summary>
        string? ValidateImage(IFormFile file);

        /// <summary>Saves the image and returns the generated stored file name.</summary>
        Task<string> SavePropertyImageAsync(IFormFile file);

        void DeletePropertyImage(string storedFileName);
    }

    public class FileStorageService(IWebHostEnvironment env) : IFileStorageService
    {
        private const long MaxImageBytes = 5 * 1024 * 1024;

        private static readonly Dictionary<string, string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp"
        };

        private string PropertyImageFolder => Path.Combine(env.WebRootPath, "uploads", "properties");

        public string? ValidateImage(IFormFile file)
        {
            if (file.Length == 0)
                return $"'{file.FileName}' is empty.";

            if (file.Length > MaxImageBytes)
                return $"'{file.FileName}' is larger than 5 MB.";

            var extension = Path.GetExtension(file.FileName);
            if (!AllowedImageTypes.TryGetValue(extension, out var contentType)
                || !string.Equals(file.ContentType, contentType, StringComparison.OrdinalIgnoreCase))
                return $"'{file.FileName}' must be a JPG, PNG or WebP image.";

            return null;
        }

        public async Task<string> SavePropertyImageAsync(IFormFile file)
        {
            Directory.CreateDirectory(PropertyImageFolder);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var storedName = $"{DateTime.UtcNow:yyyyMMddTHHmmssfff}_{suffix}{extension}";

            await using var stream = File.Create(Path.Combine(PropertyImageFolder, storedName));
            await file.CopyToAsync(stream);

            return storedName;
        }

        public void DeletePropertyImage(string storedFileName)
        {
            var path = Path.Combine(PropertyImageFolder, Path.GetFileName(storedFileName));
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
