using System.ComponentModel.DataAnnotations;
using staybnb_csharp.Models.Enums;

namespace staybnb_csharp.Models
{
    // Guest document model
    public class GuestDocument
    {
        public int Id { get; set; }

        public int GuestCheckInId { get; set; }
        public GuestCheckIn GuestCheckIn { get; set; } = null!;

        [Required, MaxLength(100)]
        public string DocumentType { get; set; } = string.Empty;

        // Generated stored file name
        [Required, MaxLength(200)]
        public string FileName { get; set; } = string.Empty;

        public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    }
}
