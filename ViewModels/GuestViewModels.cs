namespace staybnb_csharp.ViewModels
{
    // Property list view model, filtered by city and property type
    public class PropertyListViewModel
    {
        public List<Models.HostProperty> Properties { get; set; } = new();
        public List<string> Cities { get; set; } = new();
        public string? City { get; set; }
        public Models.Enums.PropertyType? PropertyType { get; set; }
    }

    // Booked range record
    public record BookedRange(string From, string To);

    // Property details view model
    public class PropertyDetailsViewModel
    {
        public Models.HostProperty Property { get; set; } = null!;
        public decimal CleaningFeePercent { get; set; }
        public decimal ServiceFeePercent { get; set; }

        // Upcoming approved or checked-in stays that block those dates
        public List<BookedRange> BookedRanges { get; set; } = new();
        public string BookedRangesJson { get; set; } = "[]";

        // Why the viewer can't book, null when they can
        public string? BookingBlockedReason { get; set; }
        public bool CanBook => BookingBlockedReason is null;
        public bool NeedsLogin { get; set; }
    }

    // Booking create view model
    public class BookingCreateViewModel
    {
        public int PropertyId { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int NumberOfGuests { get; set; } = 1;
    }
}
