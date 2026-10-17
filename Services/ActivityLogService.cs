using staybnb_csharp.Data;
using staybnb_csharp.Models;
using staybnb_csharp.Models.Enums;

namespace staybnb_csharp.Services
{
    public interface IActivityLogService
    {
        Task LogAsync(string userId, ActivityType type, string action);
    }

    public class ActivityLogService(ApplicationDbContext db) : IActivityLogService
    {
        public async Task LogAsync(string userId, ActivityType type, string action)
        {
            db.ActivityLogs.Add(new ActivityLog
            {
                UserId = userId,
                ActivityType = type,
                Action = action.Length > 100 ? action[..100] : action
            });
            await db.SaveChangesAsync();
        }
    }
}
