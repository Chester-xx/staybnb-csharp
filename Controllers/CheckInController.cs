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
    // Guest only
    [Authorize(Roles = Roles.Guest)]
    // Check-in controller, the guest check-in wizard where id is the booking id
    public class CheckInController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IFileStorageService files, INotificationService notifications, IActivityLogService activityLog) : Controller
    {
        // Define read method
        public async Task<IActionResult> Index(int id, int? step)
        {
            // Get the booking, if it does not exist, return NotFound
            var booking = await LoadBookingAsync(id);
            if (booking is null) return NotFound();

            // Check check-in is available for the booking
            var unavailable = CheckAvailability(booking);
            if (unavailable is not null) return unavailable;

            // Get the check-in settings and the guest's check-in
            var settings = CheckInSettings.FromJson(booking.Property.CheckInProcess!.StepsJson);
            var checkIn = booking.GuestCheckIn;

            // Build a document slot for each required document with its uploaded document, if any
            var slots = settings.RequiredDocuments.Select(type => new DocumentSlot(type, checkIn?.Documents.FirstOrDefault(d => d.DocumentType == type))).ToList();

            // Work out the furthest step the guest can reach and if the check-in is finished
            var reachable = checkIn is null ? 1 : slots.All(s => s.Document is not null) ? 3 : 2;
            var finished = booking.Status == BookingStatus.CheckedIn || checkIn?.Status is CheckInStatus.Submitted or CheckInStatus.Verified;

            // Return the wizard model with the view, limited to the reachable step
            return View(new CheckInWizardViewModel
            {
                Booking = booking,
                Settings = settings,
                CheckIn = checkIn,
                Slots = slots,
                Step = finished ? 4 : Math.Clamp(step ?? reachable, 1, reachable)
            });
        }

        // Post only
        [HttpPost, ValidateAntiForgeryToken]
        // Define accept rules method
        public async Task<IActionResult> AcceptRules(int id, bool accepted)
        {
            // Get the booking, if it does not exist, return NotFound
            var booking = await LoadBookingAsync(id);
            if (booking is null) return NotFound();

            // Check check-in is available for the booking
            var unavailable = CheckAvailability(booking);
            if (unavailable is not null) return unavailable;

            // If the check-in was already started, redirect to index
            if (booking.GuestCheckIn is not null) return RedirectToAction(nameof(Index), new { id });

            // If the rules were not accepted, redirect to index with an error message
            if (!accepted)
            {
                TempData["Error"] = "You must agree to the house rules to continue.";
                return RedirectToAction(nameof(Index), new { id });
            }

            // Start the check-in
            db.GuestCheckIns.Add(new GuestCheckIn
            {
                BookingId = booking.Id,
                CheckInProcessId = booking.Property.CheckInProcess!.Id,
                Status = CheckInStatus.InProgress
            });

            // Save db changes
            await db.SaveChangesAsync();

            // Log the activity
            await activityLog.LogAsync(booking.GuestId, ActivityType.CheckIn, $"Started check-in for booking #{booking.Id}");

            // Redirect to index
            return RedirectToAction(nameof(Index), new { id });
        }

        // Post only
        [HttpPost, ValidateAntiForgeryToken]
        // Set request size limit to 6 MB for the document upload
        [RequestSizeLimit(6_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 6_000_000)]
        // Define upload document method
        public async Task<IActionResult> UploadDocument(int id, string documentType, IFormFile? file)
        {
            // Get the booking, if it does not exist, return NotFound
            var booking = await LoadBookingAsync(id);
            if (booking is null) return NotFound();

            // Check check-in is available for the booking
            var unavailable = CheckAvailability(booking);
            if (unavailable is not null) return unavailable;

            // If the check-in was not started or is no longer in progress, redirect to index
            var checkIn = booking.GuestCheckIn;
            if (checkIn is null || checkIn.Status != CheckInStatus.InProgress) return RedirectToAction(nameof(Index), new { id });

            // If the document type is not required for this property, redirect to the documents step with an error message
            var settings = CheckInSettings.FromJson(booking.Property.CheckInProcess!.StepsJson);
            if (!settings.RequiredDocuments.Contains(documentType))
            {
                TempData["Error"] = "That document is not required for this property.";
                return RedirectToAction(nameof(Index), new { id, step = 2 });
            }

            // Validate the file, if there is an error, redirect to the documents step with an error message
            var error = file is null ? "Please choose a file to upload." : files.ValidateDocument(file);
            if (error is not null)
            {
                TempData["Error"] = error;
                return RedirectToAction(nameof(Index), new { id, step = 2 });
            }

            // Save the file to storage
            var storedName = await files.SaveGuestDocumentAsync(file!);

            // Uploading again replaces the earlier file for the same document type
            var existing = checkIn.Documents.FirstOrDefault(d => d.DocumentType == documentType);
            if (existing is not null)
            {
                files.DeleteGuestDocument(existing.FileName);
                db.GuestDocuments.Remove(existing);
            }

            // Add the document as pending
            db.GuestDocuments.Add(new GuestDocument
            {
                GuestCheckInId = checkIn.Id,
                DocumentType = documentType,
                FileName = storedName,
                Status = DocumentStatus.Pending
            });

            // Save db changes
            await db.SaveChangesAsync();

            // Redirect to the documents step with a success message
            TempData["Success"] = $"{documentType} uploaded.";
            return RedirectToAction(nameof(Index), new { id, step = 2 });
        }

        // Post only
        [HttpPost, ValidateAntiForgeryToken]
        // Define submit method
        public async Task<IActionResult> Submit(int id)
        {
            // Get the booking, if it does not exist, return NotFound
            var booking = await LoadBookingAsync(id);
            if (booking is null) return NotFound();

            // Check check-in is available for the booking
            var unavailable = CheckAvailability(booking);
            if (unavailable is not null) return unavailable;

            // If the check-in was not started or is no longer in progress, redirect to index
            var checkIn = booking.GuestCheckIn;
            if (checkIn is null || checkIn.Status != CheckInStatus.InProgress) return RedirectToAction(nameof(Index), new { id });

            // Check if any required document is missing, if so, redirect to the documents step with an error message
            var settings = CheckInSettings.FromJson(booking.Property.CheckInProcess!.StepsJson);
            var missing = settings.RequiredDocuments.Any(t => checkIn.Documents.All(d => d.DocumentType != t));
            if (missing)
            {
                TempData["Error"] = "Please upload all required documents before submitting.";
                return RedirectToAction(nameof(Index), new { id, step = 2 });
            }

            // Mark the check-in as submitted and save db changes
            checkIn.Status = CheckInStatus.Submitted;
            await db.SaveChangesAsync();

            // Log the activity and notify the host
            await activityLog.LogAsync(booking.GuestId, ActivityType.CheckIn, $"Submitted check-in for booking #{booking.Id}");
            await notifications.CreateAsync(booking.Property.HostId, $"Check-in submitted: {booking.Property.Title}", NotificationType.CheckIn);

            // Redirect to index with a success message
            TempData["Success"] = "Check-in submitted. The host will verify your documents.";
            return RedirectToAction(nameof(Index), new { id });
        }

        // Define load booking method, only the signed in guest's own bookings can be loaded
        private Task<Booking?> LoadBookingAsync(int id)
        {
            // Get guest id
            var guestId = userManager.GetUserId(User)!;

            // Get the booking including the property check-in process and the check-in documents
            return db.Bookings
                .Include(b => b.Property).ThenInclude(p => p.CheckInProcess)
                .Include(b => b.GuestCheckIn).ThenInclude(c => c!.Documents)
                .FirstOrDefaultAsync(b => b.Id == id && b.GuestId == guestId);
        }

        // Define check availability method, returns a redirect when check-in is unavailable or null when it is available
        private IActionResult? CheckAvailability(Booking booking)
        {
            // Check-in is only available for approved or checked-in bookings
            if (booking.Status is not (BookingStatus.Approved or BookingStatus.CheckedIn))
            {
                TempData["Error"] = "Check-in is only available for approved bookings.";
                return RedirectToAction("Index", "Bookings");
            }

            // The host must have set up check-in for the property
            if (booking.Property.CheckInProcess is null)
            {
                TempData["Error"] = "The host hasn't set up check-in for this property yet.";
                return RedirectToAction("Index", "Bookings");
            }

            // Available
            return null;
        }
    }
}
