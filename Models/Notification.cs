using System.ComponentModel.DataAnnotations;
using staybnb_csharp.Models.Enums;

namespace staybnb_csharp.Models
{
    // Notification model
    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string UserId { get; set; } = string.Empty;
        public ApplicationUser User { get; set; } = null!;

        [Required, MaxLength(100)]
        public string Title { get; set; } = string.Empty;

        public NotificationType Type { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsRead { get; set; }
    }
}
