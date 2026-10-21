using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using staybnb_csharp.Data;
using staybnb_csharp.Models;

public partial class Program
{
    private static async Task Main(string[] args)
    {
        // init builder obj
        var builder = WebApplication.CreateBuilder(args);

        // Apply db context to builder + options
        var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(connectionString));
        builder.Services.AddDatabaseDeveloperPageExceptionFilter();

        // Add identity role and disable sign in required
        builder.Services.AddDefaultIdentity<ApplicationUser>(options => options.SignIn.RequireConfirmedAccount = false)
            .AddRoles<IdentityRole>()
            .AddEntityFrameworkStores<ApplicationDbContext>();

        // Add services to builder
        builder.Services.AddScoped<staybnb_csharp.Services.IActivityLogService, staybnb_csharp.Services.ActivityLogService>();
        builder.Services.AddScoped<staybnb_csharp.Services.INotificationService, staybnb_csharp.Services.NotificationService>();
        builder.Services.AddScoped<staybnb_csharp.Services.IFileStorageService, staybnb_csharp.Services.FileStorageService>();
        builder.Services.AddScoped<staybnb_csharp.Services.IHostApplicationService, staybnb_csharp.Services.HostApplicationService>();
        builder.Services.Configure<staybnb_csharp.Services.PricingOptions>(builder.Configuration.GetSection("Pricing"));
        builder.Services.AddScoped<staybnb_csharp.Services.IPricingService, staybnb_csharp.Services.PricingService>();
        
        // Enable role checks on every request
        builder.Services.Configure<SecurityStampValidatorOptions>(options => options.ValidationInterval = TimeSpan.Zero);
        builder.Services.AddControllersWithViews();

        // Build model context
        var app = builder.Build();

        // Seed base super role as a dep inj scope
        using (var scope = app.Services.CreateScope()) { await RoleSeeder.SeedAsync(scope.ServiceProvider); }

        // Configure HTTP request pipeline
        if (app.Environment.IsDevelopment())
        {
            app.UseMigrationsEndPoint();
        }
        else
        {
            // Add error handler
            app.UseExceptionHandler("/Home/Error");

            // Force https
            app.UseHsts();
        }

        // Enable http to https redirect
        app.UseHttpsRedirection();

        // Enable file system at runtime
        app.UseStaticFiles();

        // Route requests to matched endpoints "/Home" -> home index etc
        app.UseRouting();

        // Access control
        app.UseAuthorization();

        // Assign static wwwroot files
        app.MapStaticAssets();

        // Map MVC dashboard controllers
        app.MapControllerRoute(
            name: "areas",
            pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}")
            .WithStaticAssets();

        // Map MVC home controllers
        app.MapControllerRoute(
            name: "default",
            pattern: "{controller=Home}/{action=Index}/{id?}")
            .WithStaticAssets();

        // Map endpoints for wwwroot
        app.MapRazorPages().WithStaticAssets();

        // Run
        app.Run();
    }
}