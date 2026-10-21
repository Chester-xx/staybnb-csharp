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
    // Host only
    [Area("Host")]
    [Authorize(Roles = Roles.Host)]
    
    // Check in controller
    public class CheckInsController( ApplicationDbContext db, UserManager<ApplicationUser> userManager, INotificationService notifications, IActivityLogService activityLog) : Controller
    {
        // Define read
        public async Task<IActionResult> Index()
        {
            // Get the host ID of the currently logged in user
            var hostId = userManager.GetUserId(User)!;

            // Get all check-ins for the host, including booking with guest, booking with property, order by submitted, then progress, order by created at desc
            var checkIns = await db.GuestCheckIns
                .Include(c => c.Booking).ThenInclude(b => b.Guest)
                .Include(c => c.Booking).ThenInclude(b => b.Property)
                .Where(c => c.Booking.Property.HostId == hostId)
                .OrderBy(c => c.Status == CheckInStatus.Submitted ? 0 : c.Status == CheckInStatus.InProgress ? 1 : 2)
                .ThenByDescending(c => c.CreatedAt)
                .ToListAsync();

            // Return model to view
            return View(checkIns);
        }

        // Define detail read
        public async Task<IActionResult> Details(int id)
        {
            // Load check ins for specific hosts properties
            var checkIn = await LoadOwnCheckInAsync(id);

            // Return view or not found
            return checkIn is null ? NotFound() : View(checkIn);
        }

        // Post only
        [HttpPost, ValidateAntiForgeryToken]
        // Define verify check in
        public async Task<IActionResult> Verify(int id)
        {
            // Load check ins for specific hosts properties, if null, return not found
            var checkIn = await LoadOwnCheckInAsync(id);
            if (checkIn is null) return NotFound();

            // Check if check in status is not submitted, return error message and redirect to details
            if (checkIn.Status != CheckInStatus.Submitted)
            {
                TempData["Error"] = "Only submitted check-ins can be verified.";
                return RedirectToAction(nameof(Details), new { id });
            }

            // Get host id and get booking
            var hostId = userManager.GetUserId(User)!;
            var booking = checkIn.Booking;

            // Update status of docs to verified, update check in status to verified, update booking status to checked in
            checkIn.Documents.ToList().ForEach(d => d.Status = DocumentStatus.Verified);
            checkIn.Status = CheckInStatus.Verified;
            booking.Status = BookingStatus.CheckedIn;
            
            // Save db changes
            await db.SaveChangesAsync();

            // Log activities for check in and status change, create notification for guest
            await activityLog.LogAsync(hostId, ActivityType.CheckIn, $"Verified check-in documents for booking #{booking.Id}");
            await activityLog.LogAsync(hostId, ActivityType.BookingStatusChange, $"Booking #{booking.Id} Approved to CheckedIn: {booking.Property.Title}");
            await notifications.CreateAsync(booking.GuestId, $"Check-in verified: {booking.Property.Title}", NotificationType.CheckIn);

            // Set success message and redirect to index
            TempData["Success"] = $"Check-in verified. {booking.Guest.FullName} is now checked in.";
            return RedirectToAction(nameof(Index));
        }

        // Load check in for specific host
        private Task<GuestCheckIn?> LoadOwnCheckInAsync(int id)
        {
            // Get host id
            var hostId = userManager.GetUserId(User)!;
            
            // return check ins, including docs, process, booking with guest, booking with property
            return db.GuestCheckIns
                .Include(c => c.Documents)
                .Include(c => c.CheckInProcess)
                .Include(c => c.Booking).ThenInclude(b => b.Guest)
                .Include(c => c.Booking).ThenInclude(b => b.Property)
                .FirstOrDefaultAsync(c => c.Id == id && c.Booking.Property.HostId == hostId);
        }
    }
}
