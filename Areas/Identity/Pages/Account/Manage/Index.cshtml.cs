using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using staybnb_csharp.Models;
using staybnb_csharp.Services;

namespace staybnb_csharp.Areas.Identity.Pages.Account.Manage;

public class IndexModel(
    UserManager<ApplicationUser> userManager,
    SignInManager<ApplicationUser> signInManager,
    IFileStorageService files) : PageModel
{
    public string UserId { get; set; } = string.Empty;

    /// <summary>Changes when the picture changes, so the browser does not show a cached image.</summary>
    public string? PictureVersion { get; set; }

    public bool HasPicture => PictureVersion is not null;

    [TempData]
    public string? StatusMessage { get; set; }

    [BindProperty]
    public InputModel Input { get; set; } = default!;

    [BindProperty]
    public IFormFile? ProfilePicture { get; set; }

    public class InputModel
    {
        [Required, StringLength(50)]
        [Display(Name = "First name")]
        public string FirstName { get; set; } = default!;

        [Required, StringLength(50)]
        [Display(Name = "Last name")]
        public string LastName { get; set; } = default!;
    }

    public async Task<IActionResult> OnGetAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        Input = new InputModel { FirstName = user.FirstName, LastName = user.LastName };
        SetPicture(user);
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        if (ProfilePicture is not null)
        {
            var error = files.ValidateImage(ProfilePicture);
            if (error is not null) ModelState.AddModelError(nameof(ProfilePicture), error);
        }

        if (!ModelState.IsValid)
        {
            SetPicture(user);
            return Page();
        }

        var oldPicture = user.ProfilePictureUrl;
        string? newPicture = null;

        if (ProfilePicture is not null)
        {
            newPicture = await files.SaveProfilePictureAsync(ProfilePicture);
            user.ProfilePictureUrl = newPicture;
        }

        user.FirstName = Input.FirstName.Trim();
        user.LastName = Input.LastName.Trim();

        var result = await userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            if (newPicture is not null) files.DeleteProfilePicture(newPicture);
            StatusMessage = "Error: your profile could not be updated.";
            return RedirectToPage();
        }

        if (newPicture is not null && oldPicture is not null) files.DeleteProfilePicture(oldPicture);

        await signInManager.RefreshSignInAsync(user);
        StatusMessage = "Your profile has been updated.";
        return RedirectToPage();
    }

    public async Task<IActionResult> OnPostRemovePictureAsync()
    {
        var user = await userManager.GetUserAsync(User);
        if (user is null) return NotFound();

        if (user.ProfilePictureUrl is not null)
        {
            files.DeleteProfilePicture(user.ProfilePictureUrl);
            user.ProfilePictureUrl = null;
            await userManager.UpdateAsync(user);
        }

        StatusMessage = "Your profile picture was removed.";
        return RedirectToPage();
    }

    private void SetPicture(ApplicationUser user)
    {
        UserId = user.Id;
        PictureVersion = user.ProfilePictureUrl;
    }
}
