namespace staybnb_csharp.Services
{
    public interface IFileStorageService
    {
        const int MaxImagesPerApplication = 10;

        // Property images: public, stored under wwwroot/uploads/properties.
        /// <summary>Returns an error message, or null when the image is acceptable.</summary>
        string? ValidateImage(IFormFile file);
        Task<string> SavePropertyImageAsync(IFormFile file);
        void DeletePropertyImage(string storedFileName);

        // Guest documents: private, stored under PrivateUploads/documents and never served as static files.
        string? ValidateDocument(IFormFile file);
        Task<string> SaveGuestDocumentAsync(IFormFile file);
        void DeleteGuestDocument(string storedFileName);
        (string Path, string ContentType)? GetGuestDocument(string storedFileName);

        // Profile pictures: private, stored under PrivateUploads/profile-pictures; default.svg is the fallback.
        Task<string> SaveProfilePictureAsync(IFormFile file);
        void DeleteProfilePicture(string storedFileName);
        (string Path, string ContentType)? GetProfilePicture(string? storedFileName);
    }

    public class FileStorageService(IWebHostEnvironment env) : IFileStorageService
    {
        private const long MaxFileBytes = 5 * 1024 * 1024;

        private static readonly Dictionary<string, string> ImageTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp"
        };

        private static readonly Dictionary<string, string> DocumentTypes = new(ImageTypes, StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf"
        };

        private string PropertyImageFolder => Path.Combine(env.WebRootPath, "uploads", "properties");
        private string GuestDocumentFolder => Path.Combine(env.ContentRootPath, "PrivateUploads", "documents");
        private string ProfilePictureFolder => Path.Combine(env.ContentRootPath, "PrivateUploads", "profile-pictures");

        private const string DefaultProfilePicture = "default.svg";

        public Task<string> SaveProfilePictureAsync(IFormFile file) =>
            SaveAsync(file, ProfilePictureFolder);

        public void DeleteProfilePicture(string storedFileName)
        {
            if (!string.Equals(Path.GetFileName(storedFileName), DefaultProfilePicture, StringComparison.OrdinalIgnoreCase))
            {
                Delete(ProfilePictureFolder, storedFileName);
            }
        }

        public (string Path, string ContentType)? GetProfilePicture(string? storedFileName)
        {
            var name = string.IsNullOrWhiteSpace(storedFileName) ? DefaultProfilePicture : Path.GetFileName(storedFileName);
            var path = Path.Combine(ProfilePictureFolder, name);

            if (!File.Exists(path))
            {
                name = DefaultProfilePicture;
                path = Path.Combine(ProfilePictureFolder, name);
                if (!File.Exists(path)) return null;
            }

            var extension = Path.GetExtension(name);
            var contentType = extension.Equals(".svg", StringComparison.OrdinalIgnoreCase)
                ? "image/svg+xml"
                : ImageTypes.GetValueOrDefault(extension);

            return contentType is null ? null : (path, contentType);
        }

        public string? ValidateImage(IFormFile file) =>
            Validate(file, ImageTypes, "a JPG, PNG or WebP image");

        public Task<string> SavePropertyImageAsync(IFormFile file) =>
            SaveAsync(file, PropertyImageFolder);

        public void DeletePropertyImage(string storedFileName) =>
            Delete(PropertyImageFolder, storedFileName);

        public string? ValidateDocument(IFormFile file) =>
            Validate(file, DocumentTypes, "a JPG, PNG, WebP or PDF file");

        public Task<string> SaveGuestDocumentAsync(IFormFile file) =>
            SaveAsync(file, GuestDocumentFolder);

        public void DeleteGuestDocument(string storedFileName) =>
            Delete(GuestDocumentFolder, storedFileName);

        public (string Path, string ContentType)? GetGuestDocument(string storedFileName)
        {
            var safeName = Path.GetFileName(storedFileName);
            var path = Path.Combine(GuestDocumentFolder, safeName);

            if (!File.Exists(path) || !DocumentTypes.TryGetValue(Path.GetExtension(safeName), out var contentType))
                return null;

            return (path, contentType);
        }

        private static string? Validate(IFormFile file, Dictionary<string, string> allowed, string allowedText)
        {
            if (file.Length == 0)
                return $"'{file.FileName}' is empty.";

            if (file.Length > MaxFileBytes)
                return $"'{file.FileName}' is larger than 5 MB.";

            var extension = Path.GetExtension(file.FileName);
            if (!allowed.TryGetValue(extension, out var contentType)
                || !string.Equals(file.ContentType, contentType, StringComparison.OrdinalIgnoreCase))
                return $"'{file.FileName}' must be {allowedText}.";

            return null;
        }

        // Stored name: upload timestamp + short random suffix + extension.
        private static async Task<string> SaveAsync(IFormFile file, string folder)
        {
            Directory.CreateDirectory(folder);

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var storedName = $"{DateTime.UtcNow:yyyyMMddTHHmmssfff}_{suffix}{extension}";

            await using var stream = File.Create(Path.Combine(folder, storedName));
            await file.CopyToAsync(stream);

            return storedName;
        }

        private static void Delete(string folder, string storedFileName)
        {
            var path = Path.Combine(folder, Path.GetFileName(storedFileName));
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
    }
}
