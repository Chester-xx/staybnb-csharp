using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using staybnb_csharp.Data;
using staybnb_csharp.Models;
using staybnb_csharp.Models.Enums;
using staybnb_csharp.Services;
using staybnb_csharp.ViewModels;

namespace staybnb_csharp.Controllers
{
    // Database-backed messaging (no real-time). Conversations are grouped by the other person.
    [Authorize]
    public class MessagesController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        INotificationService notifications) : Controller
    {
        private const int MaxLength = 2000;

        private bool IsStaff => User.IsInRole(Roles.Admin) || User.IsInRole(Roles.SuperAdmin);

        public async Task<IActionResult> Index()
        {
            var me = userManager.GetUserId(User)!;

            var messages = await db.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.SenderId == me || m.ReceiverId == me)
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();

            var conversations = messages
                .GroupBy(m => m.SenderId == me ? m.ReceiverId : m.SenderId)
                .Select(g =>
                {
                    var last = g.First();
                    var other = last.SenderId == me ? last.Receiver : last.Sender;
                    return new ConversationSummary(other, last, g.Count(m => m.ReceiverId == me && !m.IsRead));
                })
                .OrderByDescending(c => c.Last.Timestamp)
                .ToList();

            return View(conversations);
        }

        public async Task<IActionResult> Thread(string id)
        {
            var me = userManager.GetUserId(User)!;

            var other = await userManager.FindByIdAsync(id);
            if (other is null || other.Id == me) return NotFound();

            var messages = await db.Messages
                .Where(m => (m.SenderId == me && m.ReceiverId == id) || (m.SenderId == id && m.ReceiverId == me))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            if (messages.Count == 0) return RedirectToAction(nameof(Compose), new { to = id });

            // Opening a conversation marks its incoming messages as read.
            var unread = messages.Where(m => m.ReceiverId == me && !m.IsRead).ToList();
            if (unread.Count > 0)
            {
                unread.ForEach(m => m.IsRead = true);
                await db.SaveChangesAsync();
            }

            return View(new MessageThreadViewModel { Other = other, Messages = messages, CurrentUserId = me });
        }

        [HttpGet]
        public async Task<IActionResult> Compose(string? to, string? about)
        {
            var me = userManager.GetUserId(User)!;
            var model = new ComposeMessageViewModel();

            if (to is not null)
            {
                var recipient = await userManager.FindByIdAsync(to);
                if (recipient is null || !await CanMessageAsync(me, recipient))
                {
                    TempData["Error"] = "You can't message that user.";
                    return RedirectToAction(nameof(Index));
                }

                model.ToUserId = recipient.Id;
                model.RecipientName = recipient.FullName;
                model.Content = about is null ? string.Empty : $"Hi, I have a question about \"{about}\".";
            }
            else
            {
                model.Recipients = (await GetAllowedRecipientsAsync(me))
                    .Select(u => new SelectListItem($"{u.FullName} ({u.Email})", u.Id))
                    .ToList();
            }

            return View(model);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(string? toUserId, string? content)
        {
            var sender = (await userManager.GetUserAsync(User))!;
            content = content?.Trim() ?? string.Empty;

            if (string.IsNullOrEmpty(toUserId))
            {
                TempData["Error"] = "Please choose who to message.";
                return RedirectToAction(nameof(Compose));
            }

            var recipient = await userManager.FindByIdAsync(toUserId);
            if (recipient is null || recipient.Id == sender.Id) return NotFound();

            if (!await CanMessageAsync(sender.Id, recipient))
            {
                TempData["Error"] = "You can't message that user.";
                return RedirectToAction(nameof(Index));
            }

            if (content.Length == 0 || content.Length > MaxLength)
            {
                TempData["Error"] = content.Length == 0
                    ? "Write a message first."
                    : $"Messages can be at most {MaxLength} characters.";
                return RedirectToAction(nameof(Compose), new { to = toUserId });
            }

            db.Messages.Add(new Message
            {
                SenderId = sender.Id,
                ReceiverId = recipient.Id,
                Content = content,
                IsRead = false
            });
            await db.SaveChangesAsync();

            await notifications.CreateAsync(recipient.Id, $"New message from {sender.FullName}", NotificationType.Message);

            return RedirectToAction(nameof(Thread), new { id = recipient.Id });
        }

        // Guests and hosts can only start conversations with admins or the super admin from the Messages page.
        // The one exception is the "Message the host" button on a property page, so a guest may also message
        // a host who has a listed property. Admins and the super admin can message anyone, and anyone can reply
        // to a person who has messaged them.
        private async Task<bool> CanMessageAsync(string meId, ApplicationUser other)
        {
            if (other.Id == meId) return false;
            if (IsStaff) return true;

            if (await db.Messages.AnyAsync(m => m.SenderId == other.Id && m.ReceiverId == meId)) return true;

            var otherRoles = await userManager.GetRolesAsync(other);
            if (otherRoles.Contains(Roles.Admin) || otherRoles.Contains(Roles.SuperAdmin)) return true;

            return User.IsInRole(Roles.Guest)
                && otherRoles.Contains(Roles.Host)
                && await db.HostProperties.AnyAsync(p => p.HostId == other.Id && p.IsActive);
        }

        private async Task<List<ApplicationUser>> GetAllowedRecipientsAsync(string meId)
        {
            if (IsStaff)
            {
                return await userManager.Users.Where(u => u.Id != meId).OrderBy(u => u.FirstName).ToListAsync();
            }

            return (await userManager.GetUsersInRoleAsync(Roles.Admin))
                .Concat(await userManager.GetUsersInRoleAsync(Roles.SuperAdmin))
                .Where(u => u.Id != meId)
                .DistinctBy(u => u.Id)
                .OrderBy(u => u.FirstName)
                .ToList();
        }
    }
}
