using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using staybnb_csharp.Models;

namespace staybnb_csharp.ViewModels
{
    // Notification list view model (filtered, with counts per filter)
    public class NotificationListViewModel
    {
        public List<Notification> Items { get; set; } = new();
        public string Filter { get; set; } = "all";
        public int All { get; set; }
        public int Unread { get; set; }
        public int Read { get; set; }
        public int Today { get; set; }
    }

    // Conversation summary record (the other user, the last message and the unread count)
    public record ConversationSummary(ApplicationUser Other, Message Last, int Unread);

    // Message thread view model
    public class MessageThreadViewModel
    {
        public ApplicationUser Other { get; set; } = null!;
        public List<Message> Messages { get; set; } = new();
        public string CurrentUserId { get; set; } = string.Empty;
    }

    // Compose message view model
    public class ComposeMessageViewModel
    {
        public string? ToUserId { get; set; }
        public string? RecipientName { get; set; }
        public List<SelectListItem> Recipients { get; set; } = new();

        [Required, StringLength(2000)]
        public string Content { get; set; } = string.Empty;
    }
}
