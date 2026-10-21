using staybnb_csharp.Models;
using staybnb_csharp.Models.Enums;

namespace staybnb_csharp.ViewModels
{
    // Admin dashboard view model
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int PendingApplications { get; set; }
        public int ActiveProperties { get; set; }
        public int TotalBookings { get; set; }
        public int PendingBookings { get; set; }
        public int ApprovedHosts { get; set; }
        public List<ActivityLog> RecentActivity { get; set; } = new();
        public List<HostApplication> RecentApplications { get; set; } = new();
    }

    // User management view model
    public class UserManagementViewModel
    {
        public string? Search { get; set; }
        public List<ApplicationUser> Guests { get; set; } = new();
        public List<ApplicationUser> Hosts { get; set; } = new();
        public List<ApplicationUser> Admins { get; set; } = new();
    }

    // Activity log list view model (filtered by type, paged)
    public class ActivityLogListViewModel
    {
        public List<ActivityLog> Items { get; set; } = new();
        public ActivityType? Type { get; set; }
        public int Page { get; set; } = 1;
        public int TotalPages { get; set; } = 1;
    }
}
