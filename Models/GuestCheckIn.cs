using staybnb_csharp.Models.Enums;

namespace staybnb_csharp.Models
{
    public class GuestCheckIn
    {
        public int Id { get; set; }

        public int BookingId { get; set; }
        public Booking Booking { get; set; } = null!;

        public int CheckInProcessId { get; set; }
        public CheckInProcess CheckInProcess { get; set; } = null!;

        public CheckInStatus Status { get; set; } = CheckInStatus.InProgress;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<GuestDocument> Documents { get; set; } = new List<GuestDocument>();
    }
}
