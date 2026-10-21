namespace staybnb_csharp.ViewModels
{
    public class PropertyListViewModel
    {
        public List<Models.HostProperty> Properties { get; set; } = new();
        public List<string> Cities { get; set; } = new();
        public string? City { get; set; }
        public Models.Enums.PropertyType? PropertyType { get; set; }
    }

    public record BookedRange(string From, string To);

    public class PropertyDetailsViewModel
    {
        public Models.HostProperty Property { get; set; } = null!;
        public decimal CleaningFeePercent { get; set; }
        public decimal ServiceFeePercent { get; set; }

        /// <summary>Upcoming approved or checked-in stays that block those dates.</summary>
        public List<BookedRange> BookedRanges { get; set; } = new();
        public string BookedRangesJson { get; set; } = "[]";

        /// <summary>Why the viewer can't book, or null when they can.</summary>
        public string? BookingBlockedReason { get; set; }
        public bool CanBook => BookingBlockedReason is null;
        public bool NeedsLogin { get; set; }
    }

    public class BookingCreateViewModel
    {
        public int PropertyId { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int NumberOfGuests { get; set; } = 1;
    }
}
