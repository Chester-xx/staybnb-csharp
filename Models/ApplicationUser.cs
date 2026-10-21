using Microsoft.AspNetCore.Identity;

namespace staybnb_csharp.Models
{
    // Application user model
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? ProfilePictureUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string FullName => $"{FirstName} {LastName}".Trim();
    }
}
