using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using staybnb_csharp.Models;

namespace staybnb_csharp.Data
{
    // DB Context
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
        // Table inclusions
        public DbSet<HostProperty> HostProperties => Set<HostProperty>();
        public DbSet<PropertyImage> PropertyImages => Set<PropertyImage>();
        public DbSet<HostApplication> HostApplications => Set<HostApplication>();
        public DbSet<Booking> Bookings => Set<Booking>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<Review> Reviews => Set<Review>();
        public DbSet<Amenity> Amenities => Set<Amenity>();
        public DbSet<WishlistItem> WishlistItems => Set<WishlistItem>();
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<CheckInProcess> CheckInProcesses => Set<CheckInProcess>();
        public DbSet<GuestCheckIn> GuestCheckIns => Set<GuestCheckIn>();
        public DbSet<GuestDocument> GuestDocuments => Set<GuestDocument>();

        // Relationships
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // properties have one check in process plus fk
            builder.Entity<HostProperty>()
                .HasOne(p => p.CheckInProcess)
                .WithOne(c => c.Property)
                .HasForeignKey<CheckInProcess>(c => c.PropertyId);

            // bookings have one payment plus fk
            builder.Entity<Booking>()
                .HasOne(b => b.Payment)
                .WithOne(p => p.Booking)
                .HasForeignKey<Payment>(p => p.BookingId);

            // bookings have one guest check in plus fk
            builder.Entity<Booking>()
                .HasOne(b => b.GuestCheckIn)
                .WithOne(g => g.Booking)
                .HasForeignKey<GuestCheckIn>(g => g.BookingId);

            // Namespace filters identity rules, was an issue when using a shared db context for identity and app tables
            var ownNamespace = typeof(HostProperty).Namespace;

            // Define delete behavior for foreign keys in the same namespace ^ see above
            var foreignKeys = builder.Model.GetEntityTypes()
                .Where(e => e.ClrType.Namespace == ownNamespace)
                .SelectMany(e => e.GetForeignKeys())
                .Where(fk => fk.DeleteBehavior == DeleteBehavior.Cascade);

            // Restrict del on fk
            foreach (var fk in foreignKeys) { fk.DeleteBehavior = DeleteBehavior.Restrict; }

            // Many property images have one property plus fk
            builder.Entity<PropertyImage>()
                .HasOne(i => i.Property)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            // Many guest docs have one guest check in plus fk
            builder.Entity<GuestDocument>()
                .HasOne(d => d.GuestCheckIn)
                .WithMany(g => g.Documents)
                .HasForeignKey(d => d.GuestCheckInId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
