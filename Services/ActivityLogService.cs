using staybnb_csharp.Data;
using staybnb_csharp.Models;
using staybnb_csharp.Models.Enums;

namespace staybnb_csharp.Services
{
    // Activity log service interface
    public interface IActivityLogService
    {
        // Define log method
        Task LogAsync(string userId, ActivityType type, string action);
    }

    // Activity log service
    public class ActivityLogService(ApplicationDbContext db) : IActivityLogService
    {
        // Define log method
        public async Task LogAsync(string userId, ActivityType type, string action)
        {
            // Add the log entry, 100 length only
            db.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId,
                ActivityType = type,
                Action = action.Length > 100 ? action[..100] : action
            });

            // Save db changes
            await db.SaveChangesAsync();
        }
    }
}
