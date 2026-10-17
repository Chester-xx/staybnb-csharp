using System.ComponentModel.DataAnnotations.Schema;
using staybnb_csharp.Models.Enums;

namespace staybnb_csharp.Models
{
    // Database design only; no payment functionality is implemented.
    public class Payment
    {
        public int Id { get; set; }

        public int BookingId { get; set; }
        public Booking Booking { get; set; } = null!;

        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public PaymentStatus Status { get; set; } = PaymentStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
