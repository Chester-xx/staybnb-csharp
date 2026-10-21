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
        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = userManager.GetUserId(UserClaimsPrincipal);
            if (userId is null) return Content(string.Empty);

            var model = new HeaderIconsModel(
                await db.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead),
                await db.Messages.CountAsync(m => m.ReceiverId == userId && !m.IsRead));

            return View(model);
        }
    }
}
