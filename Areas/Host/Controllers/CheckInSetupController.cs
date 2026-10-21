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
    // Host only
    [Area("Host")]
    [Authorize(Roles = Roles.Host)]
    
    // Check in setup controller
    public class CheckInSetupController( ApplicationDbContext db, UserManager<ApplicationUser> userManager, IActivityLogService activityLog) : Controller
    {
        // Decl maximum allowed rules and rule length
        private const int MaxRules = 20;
        private const int MaxRuleLength = 200;

        // Get only
        [HttpGet]
        // Define edit method
        public async Task<IActionResult> Edit(int id)
        {
            // Get approved property by id, if null, return not found
            var property = await LoadApprovedPropertyAsync(id);
            if (property is null) return NotFound();

            // Get settings from steps json
            var settings = CheckInSettings.FromJson(property.CheckInProcess?.StepsJson);

            // return model to view, including property id, title, is first setup, title, rules, and required docs
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

        // Post only
        [HttpPost, ValidateAntiForgeryToken]
        // Define edit method where parsing model
        public async Task<IActionResult> Edit(int id, CheckInSetupViewModel model)
        {
            // Get approved property by id, if null, return not found
            var property = await LoadApprovedPropertyAsync(id);
            if (property is null) return NotFound();

            // Validate required documents, remove duplicates, and ensure at least one document is selected
            model.RequiredDocuments = (model.RequiredDocuments ?? new())
                .Where(CheckInSettings.DocumentOptions.Contains)
                .Distinct()
                .ToList();

            // Validate that at least one required document is selected
            if (model.RequiredDocuments.Count == 0) { ModelState.AddModelError(nameof(model.RequiredDocuments), "Select at least one document guests must upload."); }

            // Set model attributes, including property id, title, is first setup, and rules
            model.PropertyId = property.Id;
            model.PropertyTitle = property.Title;
            model.IsFirstSetup = property.CheckInProcess is null;
            model.Rules = (model.Rules ?? new())
                .Select(r => r?.Trim() ?? string.Empty)
                .Where(r => r.Length > 0)
                .Select(r => r.Length > MaxRuleLength ? r[..MaxRuleLength] : r)
                .Take(MaxRules)
                .ToList();

            // Verify model state is valid, if not, return view with model
            if (!ModelState.IsValid) { return View(model); }

            // Create new settings obj, set rules and required docs
            var settings = new CheckInSettings
            {
                Rules = model.Rules,
                RequiredDocuments = model.RequiredDocuments
            };

            // Get first setup value
            var firstSetup = property.CheckInProcess is null;

            // if is first setup
            if (firstSetup)
            {
                // Create new check in process, set property id, title, and steps json
                db.CheckInProcesses.Add(new CheckInProcess
                {
                    PropertyId = property.Id,
                    Title = model.Title.Trim(),
                    StepsJson = settings.ToJson()
                });

                // Set property to active on process complete
                property.IsActive = true;
            }
            // else, update existing check in process
            else
            {
                property.CheckInProcess!.Title = model.Title.Trim();
                property.CheckInProcess.StepsJson = settings.ToJson();
            }

            // Save db changes
            await db.SaveChangesAsync();

            // Log activity for check in setup or update
            await activityLog.LogAsync(userManager.GetUserId(User)!, ActivityType.Property, $"{(firstSetup ? "Set up" : "Updated")} check-in: {property.Title}");

            // Set success message and redirect to properties index
            TempData["Success"] = firstSetup ? $"Check-in is set up and '{property.Title}' is now listed." : "Check-in settings updated.";
            return RedirectToAction("Index", "Properties");
        }

        // Load only approved properties for specific host
        private Task<HostProperty?> LoadApprovedPropertyAsync(int id)
        {
            var hostId = userManager.GetUserId(User)!;
            return db.HostProperties.Include(p => p.CheckInProcess).FirstOrDefaultAsync(p => p.Id == id && p.HostId == hostId && db.HostApplications.Any(a => a.PropertyId == p.Id && a.Status == ApplicationStatus.Approved));
        }
    }
}
