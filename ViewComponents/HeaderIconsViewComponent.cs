using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using staybnb_csharp.Data;
using staybnb_csharp.Models;

namespace staybnb_csharp.ViewComponents
{
    public record HeaderIconsModel(int UnreadNotifications, int UnreadMessages);

    // Mail and bell icons with unread counts, shown in the header for signed-in users.
    public class HeaderIconsViewComponent(ApplicationDbContext db, UserManager<ApplicationUser> userManager) : ViewComponent
    {
        // Define invoke method
        public async Task<IViewComponentResult> InvokeAsync()
        {
            // Get user id, if the user is not signed in, render nothing
            var userId = userManager.GetUserId(UserClaimsPrincipal);
            if (userId is null) return Content(string.Empty);

            // Count the user's unread notifications and unread received messages
            var model = new HeaderIconsModel(
                await db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead),
                await db.Messages.CountAsync(m => m.ReceiverId == userId && !m.IsRead));

            // Return the counts with the view
            return View(model);
        }
    }
}
