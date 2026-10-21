using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using staybnb_csharp.Data;
using staybnb_csharp.Models;
using staybnb_csharp.Models.Enums;
using staybnb_csharp.Services;

namespace staybnb_csharp.Areas.Host.Controllers
{
    [Area("Host")]
    [Authorize(Roles = Roles.Host)]
    public class CheckInsController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        INotificationService notifications,
        IActivityLogService activityLog) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var hostId = userManager.GetUserId(User)!;

            var checkIns = await db.GuestCheckIns
                .Include(c => c.Booking).ThenInclude(b => b.Guest)
                .Include(c => c.Booking).ThenInclude(b => b.Property)
                .Where(c => c.Booking.Property.HostId == hostId)
                .OrderBy(c => c.Status == CheckInStatus.Submitted ? 0 : c.Status == CheckInStatus.InProgress ? 1 : 2)
                .ThenByDescending(c => c.CreatedAt)
                .ToListAsync();

            return View(checkIns);
        }

        public async Task<IActionResult> Details(int id)
        {
            var checkIn = await LoadOwnCheckInAsync(id);
            return checkIn is null ? NotFound() : View(checkIn);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Verify(int id)
        {
            var checkIn = await LoadOwnCheckInAsync(id);
            if (checkIn is null) return NotFound();

            if (checkIn.Status != CheckInStatus.Submitted)
            {
                TempData["Error"] = "Only submitted check-ins can be verified.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var hostId = userManager.GetUserId(User)!;
            var booking = checkIn.Booking;

            checkIn.Documents.ToList().ForEach(d => d.Status = DocumentStatus.Verified);
            checkIn.Status = CheckInStatus.Verified;
            booking.Status = BookingStatus.CheckedIn;
            await db.SaveChangesAsync();

            await activityLog.LogAsync(hostId, ActivityType.CheckIn,
                $"Verified check-in documents for booking #{booking.Id}");
            await activityLog.LogAsync(hostId, ActivityType.BookingStatusChange,
                $"Booking #{booking.Id} Approved to CheckedIn: {booking.Property.Title}");
            await notifications.CreateAsync(booking.GuestId,
                $"Check-in verified: {booking.Property.Title}", NotificationType.CheckIn);

            TempData["Success"] = $"Check-in verified. {booking.Guest.FullName} is now checked in.";
            return RedirectToAction(nameof(Index));
        }

        // Only check-ins for the signed-in host's own properties can be loaded.
        private Task<GuestCheckIn?> LoadOwnCheckInAsync(int id)
        {
            var hostId = userManager.GetUserId(User)!;
            return db.GuestCheckIns
                .Include(c => c.Documents)
                .Include(c => c.CheckInProcess)
                .Include(c => c.Booking).ThenInclude(b => b.Guest)
                .Include(c => c.Booking).ThenInclude(b => b.Property)
                .FirstOrDefaultAsync(c => c.Id == id && c.Booking.Property.HostId == hostId);
        }
    }
}
