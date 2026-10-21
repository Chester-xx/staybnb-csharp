using System.ComponentModel.DataAnnotations;
using staybnb_csharp.Models.Enums;

namespace staybnb_csharp.Models
{
    // Activity log model
    public class ActivityLog
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Action { get; set; } = string.Empty;

        public ActivityType ActivityType { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
