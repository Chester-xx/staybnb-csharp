using System.ComponentModel.DataAnnotations;

namespace staybnb_csharp.Models
{
    // Check-in process model, the host setup for a property
    public class CheckInProcess
    {
        public int Id { get; set; }

        public int PropertyId { get; set; }
        public HostProperty Property { get; set; } = null!;

        [Required, MaxLength(200)]
        public string Title { get; set; } = string.Empty;

        public string StepsJson { get; set; } = "[]";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
