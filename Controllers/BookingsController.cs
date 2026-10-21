using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using staybnb_csharp.Data;
using staybnb_csharp.Models;
using staybnb_csharp.Models.Enums;
using staybnb_csharp.Services;
using staybnb_csharp.ViewModels;

namespace staybnb_csharp.Controllers
{
    [Authorize(Roles = Roles.Guest)]
    public class BookingsController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IPricingService pricing,
        INotificationService notifications,
        IActivityLogService activityLog) : Controller
    {
        private const int MaxGuests = 10;
        private const int MaxNights = 365;

        public async Task<IActionResult> Index()
        {
            var guestId = userManager.GetUserId(User)!;

            var bookings = await db.Bookings
                .Include(b => b.Property).ThenInclude(p => p.Images)
                .Include(b => b.GuestCheckIn)
                .Where(b => b.GuestId == guestId)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BookingCreateViewModel model)
        {
            var property = await db.HostProperties.FirstOrDefaultAsync(p => p.Id == model.PropertyId && p.IsActive);
            if (property is null) return NotFound();

            var checkIn = model.CheckInDate.Date;
            var checkOut = model.CheckOutDate.Date;
            var nights = (checkOut - checkIn).Days;

            var error = Validate(checkIn, nights, model.NumberOfGuests)
                ?? (await HasConflictAsync(property.Id, checkIn, checkOut)
                    ? "Those dates are no longer available for this property."
                    : null);

            if (error is not null)
            {
                TempData["Error"] = error;
                return RedirectToAction("Details", "Properties", new { id = property.Id });
            }

            var guest = (await userManager.GetUserAsync(User))!;
            var price = pricing.Calculate(property.PricePerNight, nights);

            var booking = new Booking
            {
                PropertyId = property.Id,
                GuestId = guest.Id,
                CheckInDate = checkIn,
                CheckOutDate = checkOut,
                NumberOfGuests = model.NumberOfGuests,
                TotalPrice = price.Total,
                Status = BookingStatus.Pending
            };

            db.Bookings.Add(booking);
            await db.SaveChangesAsync();

            await activityLog.LogAsync(guest.Id, ActivityType.BookingStatusChange,
                $"Booking #{booking.Id} requested (Pending): {property.Title}");
            await notifications.CreateAsync(property.HostId,
                $"New booking request: {property.Title}", NotificationType.Booking);
            await notifications.CreateAsync(guest.Id,
                $"Booking requested: {property.Title}", NotificationType.Booking);

            TempData["Success"] = "Your booking request was sent to the host.";
            return RedirectToAction(nameof(Index));
        }

        private static string? Validate(DateTime checkIn, int nights, int guests)
        {
            if (checkIn < DateTime.Today) return "Check-in cannot be in the past.";
            if (nights < 1) return "Check-out must be after check-in.";
            if (nights > MaxNights) return $"Bookings can be at most {MaxNights} nights.";
            if (guests < 1 || guests > MaxGuests) return $"Number of guests must be between 1 and {MaxGuests}.";
            return null;
        }

        private Task<bool> HasConflictAsync(int propertyId, DateTime checkIn, DateTime checkOut) =>
            db.Bookings.AnyAsync(b =>
                b.PropertyId == propertyId &&
                (b.Status == BookingStatus.Approved || b.Status == BookingStatus.CheckedIn) &&
                b.CheckInDate < checkOut && b.CheckOutDate > checkIn);
    }
}
