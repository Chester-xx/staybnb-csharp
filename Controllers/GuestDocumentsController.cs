using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using staybnb_csharp.Data;
using staybnb_csharp.Models;
using staybnb_csharp.Services;

namespace staybnb_csharp.Controllers
{
    // Guest documents live outside wwwroot, so they are only reachable through this authorised action.
    [Authorize]
    public class GuestDocumentsController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IFileStorageService files) : Controller
    {
        public async Task<IActionResult> Open(int id)
        {
            var userId = userManager.GetUserId(User)!;

            var document = await db.GuestDocuments
                .Include(d => d.GuestCheckIn).ThenInclude(c => c.Booking).ThenInclude(b => b.Property)
                .FirstOrDefaultAsync(d => d.Id == id);

            if (document is null) return NotFound();

            var booking = document.GuestCheckIn.Booking;

            // Only the guest who uploaded it and the host of that property may view it.
            if (booking.GuestId != userId && booking.Property.HostId != userId) return Forbid();

            var file = files.GetGuestDocument(document.FileName);
            if (file is null) return NotFound();

            Response.Headers.CacheControl = "private, no-store";
            return PhysicalFile(file.Value.Path, file.Value.ContentType);
        }
    }
}
