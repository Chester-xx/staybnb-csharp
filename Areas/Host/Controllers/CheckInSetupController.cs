using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using staybnb_csharp.Data;
using staybnb_csharp.Models;
using staybnb_csharp.Models.Enums;
using staybnb_csharp.Services;
using staybnb_csharp.ViewModels;

namespace staybnb_csharp.Areas.Host.Controllers
{
    [Area("Host")]
    [Authorize(Roles = Roles.Host)]
    public class CheckInSetupController(
        ApplicationDbContext db,
        UserManager<ApplicationUser> userManager,
        IActivityLogService activityLog) : Controller
    {
        private const int MaxRules = 20;
        private const int MaxRuleLength = 200;

        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var property = await LoadApprovedPropertyAsync(id);
            if (property is null) return NotFound();

            var settings = CheckInSettings.FromJson(property.CheckInProcess?.StepsJson);

            return View(new CheckInSetupViewModel
            {
                PropertyId = property.Id,
                PropertyTitle = property.Title,
                IsFirstSetup = property.CheckInProcess is null,
                Title = property.CheckInProcess?.Title ?? $"Check-in for {property.Title}",
                Rules = settings.Rules,
                RequiredDocuments = settings.RequiredDocuments
                    .Where(CheckInSettings.DocumentOptions.Contains)
                    .ToList()
            });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, CheckInSetupViewModel model)
        {
            var property = await LoadApprovedPropertyAsync(id);
            if (property is null) return NotFound();

            model.RequiredDocuments = (model.RequiredDocuments ?? new())
                .Where(CheckInSettings.DocumentOptions.Contains)
                .Distinct()
                .ToList();

            if (model.RequiredDocuments.Count == 0)
            {
                ModelState.AddModelError(nameof(model.RequiredDocuments), "Select at least one document guests must upload.");
            }

            model.PropertyId = property.Id;
            model.PropertyTitle = property.Title;
            model.IsFirstSetup = property.CheckInProcess is null;
            model.Rules = (model.Rules ?? new())
                .Select(r => r?.Trim() ?? string.Empty)
                .Where(r => r.Length > 0)
                .Select(r => r.Length > MaxRuleLength ? r[..MaxRuleLength] : r)
                .Take(MaxRules)
                .ToList();

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var settings = new CheckInSettings
            {
                Rules = model.Rules,
                RequiredDocuments = model.RequiredDocuments
            };

            var firstSetup = property.CheckInProcess is null;

            if (firstSetup)
            {
                db.CheckInProcesses.Add(new CheckInProcess
                {
                    PropertyId = property.Id,
                    Title = model.Title.Trim(),
                    StepsJson = settings.ToJson()
                });

                // Completing the required setup for the first time puts the approved property on the marketplace.
                property.IsActive = true;
            }
            else
            {
                property.CheckInProcess!.Title = model.Title.Trim();
                property.CheckInProcess.StepsJson = settings.ToJson();
            }

            await db.SaveChangesAsync();

            await activityLog.LogAsync(userManager.GetUserId(User)!, ActivityType.Property,
                $"{(firstSetup ? "Set up" : "Updated")} check-in: {property.Title}");

            TempData["Success"] = firstSetup
                ? $"Check-in is set up and '{property.Title}' is now listed."
                : "Check-in settings updated.";
            return RedirectToAction("Index", "Properties");
        }

        // Only the signed-in host's own, approved properties can be configured.
        private Task<HostProperty?> LoadApprovedPropertyAsync(int id)
        {
            var hostId = userManager.GetUserId(User)!;
            return db.HostProperties
                .Include(p => p.CheckInProcess)
                .FirstOrDefaultAsync(p => p.Id == id && p.HostId == hostId &&
                    db.HostApplications.Any(a => a.PropertyId == p.Id && a.Status == ApplicationStatus.Approved));
        }
    }
}
