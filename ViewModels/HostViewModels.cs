namespace staybnb_csharp.ViewModels
{
    // Check-in setup view model
    public class CheckInSetupViewModel
    {
        public int PropertyId { get; set; }
        public string? PropertyTitle { get; set; }
        public bool IsFirstSetup { get; set; }

        [System.ComponentModel.DataAnnotations.Required]
        [System.ComponentModel.DataAnnotations.StringLength(200)]
        [System.ComponentModel.DataAnnotations.Display(Name = "Check-in title")]
        public string Title { get; set; } = string.Empty;

        [System.ComponentModel.DataAnnotations.Display(Name = "Required documents")]
        public List<string> RequiredDocuments { get; set; } = new();

        public List<string> Rules { get; set; } = new();
    }

    // Host dashboard view model
    public class HostDashboardViewModel
    {
        public string HostName { get; set; } = string.Empty;
        public int TotalProperties { get; set; }
        public int ActiveProperties { get; set; }
        public int PendingApplications { get; set; }
        public int PendingBookingRequests { get; set; }
        public int TotalBookings { get; set; }
    }
}
