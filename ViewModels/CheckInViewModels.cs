using staybnb_csharp.Models;

namespace staybnb_csharp.ViewModels
{
    // Document slot record (a required document type and its uploaded document, if any)
    public record DocumentSlot(string DocumentType, GuestDocument? Document);

    // Check-in wizard view model
    public class CheckInWizardViewModel
    {
        public Booking Booking { get; set; } = null!;
        public CheckInSettings Settings { get; set; } = new();
        public GuestCheckIn? CheckIn { get; set; }
        public List<DocumentSlot> Slots { get; set; } = new();

        // Current step: 1 = rules, 2 = documents, 3 = review and submit, 4 = submitted or verified
        public int Step { get; set; }

        // True when every required document slot has an uploaded document
        public bool AllDocumentsUploaded => Slots.All(s => s.Document is not null);
    }
}
