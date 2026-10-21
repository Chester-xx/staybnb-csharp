using staybnb_csharp.Models;

namespace staybnb_csharp.ViewModels
{
    public record DocumentSlot(string DocumentType, GuestDocument? Document);

    public class CheckInWizardViewModel
    {
        public Booking Booking { get; set; } = null!;
        public CheckInSettings Settings { get; set; } = new();
        public GuestCheckIn? CheckIn { get; set; }
        public List<DocumentSlot> Slots { get; set; } = new();

        /// <summary>1 = rules, 2 = documents, 3 = review and submit, 4 = submitted or verified.</summary>
        public int Step { get; set; }

        public bool AllDocumentsUploaded => Slots.All(s => s.Document is not null);
    }
}
