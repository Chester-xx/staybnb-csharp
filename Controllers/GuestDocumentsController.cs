using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using staybnb_csharp.Data;
using staybnb_csharp.Models;
using staybnb_csharp.Services;

namespace staybnb_csharp.Controllers
{
    // Signed in users only
    [Authorize]
    // Guest documents controller, documents live outside wwwroot so they are only reachable through this action
    public class GuestDocumentsController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, IFileStorageService files) : Controller
    {
        // Define open method
        public async Task<IActionResult> Open(int id)
        {
            // Get user id
            var userId = userManager.GetUserId(User)!;

            // Get the document by its id, including the check-in, booking and property
            var document = await db.GuestDocuments
                .Include(d => d.GuestCheckIn).ThenInclude(c => c.Booking).ThenInclude(b => b.Property)
                .FirstOrDefaultAsync(d => d.Id == id);

            // If the document does not exist, return NotFound
            if (document is null) return NotFound();

            // Get the booking the document belongs to
            var booking = document.GuestCheckIn.Booking;

            // Only the guest who uploaded it and the host of the property may view it
            if (booking.GuestId != userId && booking.Property.HostId != userId) return Forbid();

            // Get the file from storage, if it is missing, return NotFound
            var file = files.GetGuestDocument(document.FileName);
            if (file is null) return NotFound();

            // Return the file without caching it
            Response.Headers.CacheControl = "private, no-store";
            return PhysicalFile(file.Value.Path, file.Value.ContentType);
        }
    }
}
