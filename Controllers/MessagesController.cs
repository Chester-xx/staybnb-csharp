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
    // Signed in users only
    [Authorize]
    // Messages controller, database backed messaging with no real-time, conversations are grouped by the other person
    public class MessagesController(ApplicationDbContext db, UserManager<ApplicationUser> userManager, INotificationService notifications) : Controller
    {
        // Max message length
        private const int MaxLength = 2000;

        // Check if the signed in user is an admin or super admin
        private bool IsStaff => User.IsInRole(Roles.Admin) || User.IsInRole(Roles.SuperAdmin);

        // Define read method
        public async Task<IActionResult> Index()
        {
            // Get user id
            var me = userManager.GetUserId(User)!;

            // Get all messages sent or received by the user, including the sender and receiver, ordered desc
            var messages = await db.Messages
                .Include(m => m.Sender)
                .Include(m => m.Receiver)
                .Where(m => m.SenderId == me || m.ReceiverId == me)
                .OrderByDescending(m => m.Timestamp)
                .ToListAsync();

            // Group the messages by the other person, with the last message and the unread count, latest first
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

            // Return the conversations with the view
            return View(conversations);
        }

        // Define thread method
        public async Task<IActionResult> Thread(string id)
        {
            // Get user id
            var me = userManager.GetUserId(User)!;

            // Get the other user, if they do not exist or are the signed in user, return NotFound
            var other = await userManager.FindByIdAsync(id);
            if (other is null || other.Id == me) return NotFound();

            // Get all messages between the two users, oldest first
            var messages = await db.Messages
                .Where(m => (m.SenderId == me && m.ReceiverId == id) || (m.SenderId == id && m.ReceiverId == me))
                .OrderBy(m => m.Timestamp)
                .ToListAsync();

            // If there are no messages yet, redirect to compose
            if (messages.Count == 0) return RedirectToAction(nameof(Compose), new { to = id });

            // Opening a conversation marks its incoming messages as read
            var unread = messages.Where(m => m.ReceiverId == me && !m.IsRead).ToList();
            if (unread.Count > 0) { unread.ForEach(m => m.IsRead = true); await db.SaveChangesAsync(); }

            // Return the thread model with the view
            return View(new MessageThreadViewModel { Other = other, Messages = messages, CurrentUserId = me });
        }

        // Get only
        [HttpGet]
        // Define compose method
        public async Task<IActionResult> Compose(string? to, string? about)
        {
            // Get user id and an empty model
            var me = userManager.GetUserId(User)!;
            var model = new ComposeMessageViewModel();

            if (to is not null)
            {
                // Get the recipient, if they do not exist or cannot be messaged, redirect to index with an error message
                var recipient = await userManager.FindByIdAsync(to);
                if (recipient is null || !await CanMessageAsync(me, recipient))
                {
                    TempData["Error"] = "You can't message that user.";
                    return RedirectToAction(nameof(Index));
                }

                // Fix the recipient and prefill the message if it is about a property
                model.ToUserId = recipient.Id;
                model.RecipientName = recipient.FullName;
                model.Content = about is null ? string.Empty : $"Hi, I have a question about \"{about}\".";
            }
            else
            {
                // Otherwise let the user choose from the recipients they are allowed to message
                model.Recipients = (await GetAllowedRecipientsAsync(me)).Select(u => new SelectListItem($"{u.FullName} ({u.Email})", u.Id)).ToList();
            }

            // Return the model with the view
            return View(model);
        }

        // Post only
        [HttpPost, ValidateAntiForgeryToken]
        // Define send method
        public async Task<IActionResult> Send(string? toUserId, string? content)
        {
            // Get the sender and trim the content
            var sender = (await userManager.GetUserAsync(User))!;
            content = content?.Trim() ?? string.Empty;

            // If no recipient was chosen, redirect to compose with an error message
            if (string.IsNullOrEmpty(toUserId))
            {
                TempData["Error"] = "Please choose who to message.";
                return RedirectToAction(nameof(Compose));
            }

            // Get the recipient, if they do not exist or are the sender, return NotFound
            var recipient = await userManager.FindByIdAsync(toUserId);
            if (recipient is null || recipient.Id == sender.Id) return NotFound();

            // If the sender may not message the recipient, redirect to index with an error message
            if (!await CanMessageAsync(sender.Id, recipient))
            {
                TempData["Error"] = "You can't message that user.";
                return RedirectToAction(nameof(Index));
            }

            // If the content is empty or too long, redirect to compose with an error message
            if (content.Length == 0 || content.Length > MaxLength)
            {
                TempData["Error"] = content.Length == 0 ? "Write a message first." : $"Messages can be at most {MaxLength} characters.";
                return RedirectToAction(nameof(Compose), new { to = toUserId });
            }

            // Add the message as unread
            db.Messages.Add(new Message
            {
                SenderId = sender.Id,
                ReceiverId = recipient.Id,
                Content = content,
                IsRead = false
            });

            // Save db changes
            await db.SaveChangesAsync();

            // Notify the recipient
            await notifications.CreateAsync(recipient.Id, $"New message from {sender.FullName}", NotificationType.Message);

            // Redirect to the thread
            return RedirectToAction(nameof(Thread), new { id = recipient.Id });
        }

        // Define can message method
        // Guests and hosts can only start conversations with admins or the super admin from the messages page
        // The one exception is the message the host button on a property page, so a guest may also message a host who has a listed property
        // Admins and the super admin can message anyone, and anyone can reply to a person who has messaged them
        private async Task<bool> CanMessageAsync(string meId, ApplicationUser other)
        {
            // Nobody can message themselves, staff can message anyone
            if (other.Id == meId) return false;
            if (IsStaff) return true;

            // Anyone can reply to a person who has messaged them
            if (await db.Messages.AnyAsync(m => m.SenderId == other.Id && m.ReceiverId == meId)) return true;

            // Anyone can message admins and the super admin
            var otherRoles = await userManager.GetRolesAsync(other);
            if (otherRoles.Contains(Roles.Admin) || otherRoles.Contains(Roles.SuperAdmin)) return true;

            // Guests can message a host who has a listed property
            return User.IsInRole(Roles.Guest) && otherRoles.Contains(Roles.Host) && await db.HostProperties.AnyAsync(p => p.HostId == other.Id && p.IsActive);
        }

        // Define get allowed recipients method
        private async Task<List<ApplicationUser>> GetAllowedRecipientsAsync(string meId)
        {
            // Staff can message every other user
            if (IsStaff) { return await userManager.Users.Where(u => u.Id != meId).OrderBy(u => u.FirstName).ToListAsync(); }

            // Everyone else can only start conversations with admins and super admins
            return (await userManager.GetUsersInRoleAsync(Roles.Admin))
                .Concat(await userManager.GetUsersInRoleAsync(Roles.SuperAdmin))
                .Where(u => u.Id != meId)
                .DistinctBy(u => u.Id)
                .OrderBy(u => u.FirstName)
                .ToList();
        }
    }
}
