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
    public class BookingRequestsController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        INotificationService notifications,
        IActivityLogService activityLog) : Controller
    {
        public async Task<IActionResult> Index()
        {
            var userId = userManager.GetUserId(User)!;

            var bookings = await db.Bookings
                .Include(b => b.Property)
                .Include(b => b.Guest)
                .Where(b => b.Property.HostId == userId)
                .OrderBy(b => b.Status == BookingStatus.Pending ? 0 : 1)
                .ThenByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id)
        {
            var booking = await LoadOwnBookingAsync(id);
            if (booking is null) return NotFound();

            if (booking.Status != BookingStatus.Pending)
            {
                TempData["Error"] = "This booking request has already been handled.";
                return RedirectToAction(nameof(Index));
            }

            var hostId = userManager.GetUserId(User)!;

            // Two requests can overlap while pending; only one of them may be approved.
            var conflict = await db.Bookings.AnyAsync(b =>
                b.Id != booking.Id &&
                b.PropertyId == booking.PropertyId &&
                (b.Status == BookingStatus.Approved || b.Status == BookingStatus.CheckedIn) &&
                b.CheckInDate < booking.CheckOutDate && b.CheckOutDate > booking.CheckInDate);

            if (conflict)
            {
                TempData["Error"] = "These dates overlap with another approved booking for this property. Reject this request instead.";
                return RedirectToAction(nameof(Index));
            }

            booking.Status = BookingStatus.Approved;
            await db.SaveChangesAsync();

            await activityLog.LogAsync(hostId, ActivityType.BookingStatusChange,
                $"Booking #{booking.Id} Pending to Approved: {booking.Property.Title}");
            await notifications.CreateAsync(booking.GuestId,
                $"Booking approved: {booking.Property.Title}", NotificationType.Booking);

            TempData["Success"] = "Booking approved and the guest has been notified.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id)
        {
            var booking = await LoadOwnBookingAsync(id);
            if (booking is null) return NotFound();

            if (booking.Status != BookingStatus.Pending)
            {
                TempData["Error"] = "This booking request has already been handled.";
                return RedirectToAction(nameof(Index));
            }

            var hostId = userManager.GetUserId(User)!;

            booking.Status = BookingStatus.Rejected;
            await db.SaveChangesAsync();

            await activityLog.LogAsync(hostId, ActivityType.BookingStatusChange,
                $"Booking #{booking.Id} Pending to Rejected: {booking.Property.Title}");
            await notifications.CreateAsync(booking.GuestId,
                $"Booking rejected: {booking.Property.Title}", NotificationType.Booking);

            TempData["Success"] = "Booking rejected and the guest has been notified.";
            return RedirectToAction(nameof(Index));
        }

        // Only bookings on the signed-in host's own properties can be loaded.
        private Task<Booking?> LoadOwnBookingAsync(int id)
        {
            var hostId = userManager.GetUserId(User)!;
            return db.Bookings
                .Include(b => b.Property)
                .FirstOrDefaultAsync(b => b.Id == id && b.Property.HostId == hostId);
        }
    }
}
