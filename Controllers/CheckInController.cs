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
    // Guest check-in wizard. {id} is the booking id.
    // The step is derived from saved state: no check-in = rules, missing documents = upload, all uploaded = review.
    [Authorize(Roles = Roles.Guest)]
    public class CheckInController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IFileStorageService files,
        INotificationService notifications,
        IActivityLogService activityLog) : Controller
    {
        public async Task<IActionResult> Index(int id, int? step)
        {
            var booking = await LoadBookingAsync(id);
            if (booking is null) return NotFound();

            var unavailable = CheckAvailability(booking);
            if (unavailable is not null) return unavailable;

            var settings = CheckInSettings.FromJson(booking.Property.CheckInProcess!.StepsJson);
            var checkIn = booking.GuestCheckIn;

            var slots = settings.RequiredDocuments
                .Select(type => new DocumentSlot(type, checkIn?.Documents.FirstOrDefault(d => d.DocumentType == type)))
                .ToList();

            var reachable = checkIn is null ? 1 : slots.All(s => s.Document is not null) ? 3 : 2;
            var finished = booking.Status == BookingStatus.CheckedIn
                || checkIn?.Status is CheckInStatus.Submitted or CheckInStatus.Verified;

            return View(new CheckInWizardViewModel
            {
                Booking = booking,
                Settings = settings,
                CheckIn = checkIn,
                Slots = slots,
                Step = finished ? 4 : Math.Clamp(step ?? reachable, 1, reachable)
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> AcceptRules(int id, bool accepted)
        {
            var booking = await LoadBookingAsync(id);
            if (booking is null) return NotFound();

            var unavailable = CheckAvailability(booking);
            if (unavailable is not null) return unavailable;

            if (booking.GuestCheckIn is not null) return RedirectToAction(nameof(Index), new { id });

            if (!accepted)
            {
                TempData["Error"] = "You must agree to the house rules to continue.";
                return RedirectToAction(nameof(Index), new { id });
            }

            db.GuestCheckIns.Add(new GuestCheckIn
            {
                BookingId = booking.Id,
                CheckInProcessId = booking.Property.CheckInProcess!.Id,
                Status = CheckInStatus.InProgress
            });
            await db.SaveChangesAsync();

            await activityLog.LogAsync(booking.GuestId, ActivityType.CheckIn,
                $"Started check-in for booking #{booking.Id}");

            return RedirectToAction(nameof(Index), new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(6_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 6_000_000)]
        public async Task<IActionResult> UploadDocument(int id, string documentType, IFormFile? file)
        {
            var booking = await LoadBookingAsync(id);
            if (booking is null) return NotFound();

            var unavailable = CheckAvailability(booking);
            if (unavailable is not null) return unavailable;

            var checkIn = booking.GuestCheckIn;
            if (checkIn is null || checkIn.Status != CheckInStatus.InProgress)
                return RedirectToAction(nameof(Index), new { id });

            var settings = CheckInSettings.FromJson(booking.Property.CheckInProcess!.StepsJson);
            if (!settings.RequiredDocuments.Contains(documentType))
            {
                TempData["Error"] = "That document is not required for this property.";
                return RedirectToAction(nameof(Index), new { id, step = 2 });
            }

            var error = file is null ? "Please choose a file to upload." : files.ValidateDocument(file);
            if (error is not null)
            {
                TempData["Error"] = error;
                return RedirectToAction(nameof(Index), new { id, step = 2 });
            }

            var storedName = await files.SaveGuestDocumentAsync(file!);

            // Uploading again replaces the earlier file for the same document type.
            var existing = checkIn.Documents.FirstOrDefault(d => d.DocumentType == documentType);
            if (existing is not null)
            {
                files.DeleteGuestDocument(existing.FileName);
                db.GuestDocuments.Remove(existing);
            }

            db.GuestDocuments.Add(new GuestDocument
            {
                GuestCheckInId = checkIn.Id,
                DocumentType = documentType,
                FileName = storedName,
                Status = DocumentStatus.Pending
            });
            await db.SaveChangesAsync();

            TempData["Success"] = $"{documentType} uploaded.";
            return RedirectToAction(nameof(Index), new { id, step = 2 });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Submit(int id)
        {
            var booking = await LoadBookingAsync(id);
            if (booking is null) return NotFound();

            var unavailable = CheckAvailability(booking);
            if (unavailable is not null) return unavailable;

            var checkIn = booking.GuestCheckIn;
            if (checkIn is null || checkIn.Status != CheckInStatus.InProgress)
                return RedirectToAction(nameof(Index), new { id });

            var settings = CheckInSettings.FromJson(booking.Property.CheckInProcess!.StepsJson);
            var missing = settings.RequiredDocuments.Any(t => checkIn.Documents.All(d => d.DocumentType != t));
            if (missing)
            {
                TempData["Error"] = "Please upload all required documents before submitting.";
                return RedirectToAction(nameof(Index), new { id, step = 2 });
            }

            checkIn.Status = CheckInStatus.Submitted;
            await db.SaveChangesAsync();

            await activityLog.LogAsync(booking.GuestId, ActivityType.CheckIn,
                $"Submitted check-in for booking #{booking.Id}");
            await notifications.CreateAsync(booking.Property.HostId,
                $"Check-in submitted: {booking.Property.Title}", NotificationType.CheckIn);

            TempData["Success"] = "Check-in submitted. The host will verify your documents.";
            return RedirectToAction(nameof(Index), new { id });
        }

        // Only the signed-in guest's own bookings can be loaded.
        private Task<Booking?> LoadBookingAsync(int id)
        {
            var guestId = userManager.GetUserId(User)!;
            return db.Bookings
                .Include(b => b.Property).ThenInclude(p => p.CheckInProcess)
                .Include(b => b.GuestCheckIn).ThenInclude(c => c!.Documents)
                .FirstOrDefaultAsync(b => b.Id == id && b.GuestId == guestId);
        }

        private IActionResult? CheckAvailability(Booking booking)
        {
            if (booking.Status is not (BookingStatus.Approved or BookingStatus.CheckedIn))
            {
                TempData["Error"] = "Check-in is only available for approved bookings.";
                return RedirectToAction("Index", "Bookings");
            }

            if (booking.Property.CheckInProcess is null)
            {
                TempData["Error"] = "The host hasn't set up check-in for this property yet.";
                return RedirectToAction("Index", "Bookings");
            }

            return null;
        }
    }
}
