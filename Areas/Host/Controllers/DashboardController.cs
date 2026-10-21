using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using staybnb_csharp.Data;
using staybnb_csharp.Models;
using staybnb_csharp.Models.Enums;
using staybnb_csharp.ViewModels;

namespace staybnb_csharp.Areas.Host.Controllers
{
    // Host only
    [Area("Host")]
    [Authorize(Roles = Roles.Host)]
    
    // Dashboard controller
    public class DashboardController(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : Controller
    {
        // Define read
        public async Task<IActionResult> Index()
        {
            // Get the current user
            var user = (await userManager.GetUserAsync(User))!;

            // Create the view model, host name, total properties, active properties, pending applications, pending booking requests, and total bookings
            var model = new HostDashboardViewModel
            {
                HostName = user.FirstName,
                TotalProperties = await db.HostApplications.CountAsync(a => a.Property.HostId == user.Id && a.Status == ApplicationStatus.Approved),
                ActiveProperties = await db.HostProperties.CountAsync(p => p.HostId == user.Id && p.IsActive),
                PendingApplications = await db.HostApplications.CountAsync(a => a.Property.HostId == user.Id && a.Status == ApplicationStatus.Pending),
                PendingBookingRequests = await db.Bookings.CountAsync(b => b.Property.HostId == user.Id && b.Status == BookingStatus.Pending),
                TotalBookings = await db.Bookings.CountAsync(b => b.Property.HostId == user.Id)
            };

            // Return model with view
            return View(model);
        }
    }
}
