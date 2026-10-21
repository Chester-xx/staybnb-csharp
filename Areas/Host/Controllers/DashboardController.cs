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
    [Area("Host")]
    [Authorize(Roles = Roles.Host)]
    public class DashboardController(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var user = (await userManager.GetUserAsync(User))!;

            var model = new HostDashboardViewModel
            {
                HostName = user.FirstName,
                TotalProperties = await db.HostApplications.CountAsync(a =>
                    a.Property.HostId == user.Id && a.Status == ApplicationStatus.Approved),
                ActiveProperties = await db.HostProperties.CountAsync(p => p.HostId == user.Id && p.IsActive),
                PendingApplications = await db.HostApplications.CountAsync(a =>
                    a.Property.HostId == user.Id && a.Status == ApplicationStatus.Pending),
                PendingBookingRequests = await db.Bookings.CountAsync(b =>
                    b.Property.HostId == user.Id && b.Status == BookingStatus.Pending),
                TotalBookings = await db.Bookings.CountAsync(b => b.Property.HostId == user.Id)
            };

            return View(model);
        }
    }
}
