namespace staybnb_csharp.Services
{
    // File storage service interface
    public interface IFileStorageService
    {
        // Max number of images per host application
        const int MaxImagesPerApplication = 10;

        // Methods
        string? ValidateImage(IFormFile file);
        Task<string> SavePropertyImageAsync(IFormFile file);
        void DeletePropertyImage(string storedFileName);
        string? ValidateDocument(IFormFile file);
        Task<string> SaveGuestDocumentAsync(IFormFile file);
        void DeleteGuestDocument(string storedFileName);
        (string Path, string ContentType)? GetGuestDocument(string storedFileName);
        Task<string> SaveProfilePictureAsync(IFormFile file);
        void DeleteProfilePicture(string storedFileName);
        (string Path, string ContentType)? GetProfilePicture(string? storedFileName);
    }

    // File storage service
    // Property images are public (wwwroot/uploads/properties), guest documents and profile pictures are private (PrivateUploads) and never served as static files
    public class FileStorageService(IWebHostEnvironment env) : IFileStorageService
    {
        // Max file size of 5 MB
        private const long MaxFileBytes = 5 * 1024 * 1024;

        // Allowed image extensions mapped to their content types
        private static readonly Dictionary<string, string> ImageTypes = new(StringComparer.OrdinalIgnoreCase)
        {
            [".jpg"] = "image/jpeg",
            [".jpeg"] = "image/jpeg",
            [".png"] = "image/png",
            [".webp"] = "image/webp"
        };

        // Allowed document extensions (images + pdf) mapped to their content types
        private static readonly Dictionary<string, string> DocumentTypes = new(ImageTypes, StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf"
        };

        // Storage folders
        private string PropertyImageFolder => Path.Combine(env.WebRootPath, "uploads", "properties");
        private string GuestDocumentFolder => Path.Combine(env.ContentRootPath, "PrivateUploads", "documents");
        private string ProfilePictureFolder => Path.Combine(env.ContentRootPath, "PrivateUploads", "profile-pictures");

        // Fallback profile picture, never deleted
        private const string DefaultProfilePicture = "default.svg";

        // Define save profile picture method
        public Task<string> SaveProfilePictureAsync(IFormFile file) => SaveAsync(file, ProfilePictureFolder);

        // Define delete profile picture method
        public void DeleteProfilePicture(string storedFileName)
        {
            // Only delete if it is not the default profile picture
            if (!string.Equals(Path.GetFileName(storedFileName), DefaultProfilePicture, StringComparison.OrdinalIgnoreCase)) { Delete(ProfilePictureFolder, storedFileName); }
        }

        // Define get profile picture method
        public (string Path, string ContentType)? GetProfilePicture(string? storedFileName)
        {
            // Use the default picture if no file name is set, GetFileName strips any path parts
            var name = string.IsNullOrWhiteSpace(storedFileName) ? DefaultProfilePicture : Path.GetFileName(storedFileName);
            var path = Path.Combine(ProfilePictureFolder, name);

            // If the file does not exist, fall back to the default picture, if that is missing too, return null
            if (!File.Exists(path)) { name = DefaultProfilePicture; path = Path.Combine(ProfilePictureFolder, name); if (!File.Exists(path)) return null; }

            // Get the content type from the extension (svg is only allowed for the default picture)
            var extension = Path.GetExtension(name);
            var contentType = extension.Equals(".svg", StringComparison.OrdinalIgnoreCase) ? "image/svg+xml" : ImageTypes.GetValueOrDefault(extension);

            // Return the path and content type, or null if the type is not allowed
            return contentType is null ? null : (path, contentType);
        }

        // Define validate image method
        public string? ValidateImage(IFormFile file) => Validate(file, ImageTypes, "a JPG, PNG or WebP image");

        // Define save property image method
        public Task<string> SavePropertyImageAsync(IFormFile file) => SaveAsync(file, PropertyImageFolder);

        // Define delete property image method
        public void DeletePropertyImage(string storedFileName) => Delete(PropertyImageFolder, storedFileName);

        // Define validate document method
        public string? ValidateDocument(IFormFile file) => Validate(file, DocumentTypes, "a JPG, PNG, WebP or PDF file");

        // Define save guest document method
        public Task<string> SaveGuestDocumentAsync(IFormFile file) => SaveAsync(file, GuestDocumentFolder);

        // Define delete guest document method
        public void DeleteGuestDocument(string storedFileName) => Delete(GuestDocumentFolder, storedFileName);

        // Define get guest document method
        public (string Path, string ContentType)? GetGuestDocument(string storedFileName)
        {
            // Strip any path parts from the file name
            var safeName = Path.GetFileName(storedFileName);
            var path = Path.Combine(GuestDocumentFolder, safeName);

            // If the file does not exist or its extension is not allowed, return null
            if (!File.Exists(path) || !DocumentTypes.TryGetValue(Path.GetExtension(safeName), out var contentType)) return null;

            // Return the path and content type
            return (path, contentType);
        }

        // Define validate method
        private static string? Validate(IFormFile file, Dictionary<string, string> allowed, string allowedText)
        {
            // Check the file is not empty
            if (file.Length == 0) return $"'{file.FileName}' is empty.";

            // Check the file is not larger than the max size
            if (file.Length > MaxFileBytes) return $"'{file.FileName}' is larger than 5 MB.";

            // Check both the extension and the content type are allowed and match
            var extension = Path.GetExtension(file.FileName);
            if (!allowed.TryGetValue(extension, out var contentType) || !string.Equals(file.ContentType, contentType, StringComparison.OrdinalIgnoreCase)) return $"'{file.FileName}' must be {allowedText}.";

            // No error
            return null;
        }

        // Define save method
        private static async Task<string> SaveAsync(IFormFile file, string folder)
        {
            // Create the folder if it does not exist
            Directory.CreateDirectory(folder);

            // Build the stored name: upload timestamp + short random suffix + extension
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var suffix = Guid.NewGuid().ToString("N")[..8];
            var storedName = $"{DateTime.UtcNow:yyyyMMddTHHmmssfff}_{suffix}{extension}";

            // Write the file to disk
            await using var stream = File.Create(Path.Combine(folder, storedName));
            await file.CopyToAsync(stream);

            // Return the stored name
            return storedName;
        }

        // Define delete method
        private static void Delete(string folder, string storedFileName)
        {
            // Strip any path parts from the file name, then delete the file if it exists
            var path = Path.Combine(folder, Path.GetFileName(storedFileName));
            if (File.Exists(path)) { File.Delete(path); }
        }
    }
}
