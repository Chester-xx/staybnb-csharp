using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using staybnb_csharp.Models;

namespace staybnb_csharp.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : IdentityDbContext<ApplicationUser>(options)
    {
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

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // One-to-one relationships from the ERD.
            builder.Entity<HostProperty>()
                .HasOne(p => p.CheckInProcess)
                .WithOne(c => c.Property)
                .HasForeignKey<CheckInProcess>(c => c.PropertyId);

            builder.Entity<Booking>()
                .HasOne(b => b.Payment)
                .WithOne(p => p.Booking)
                .HasForeignKey<Payment>(p => p.BookingId);

            builder.Entity<Booking>()
                .HasOne(b => b.GuestCheckIn)
                .WithOne(g => g.Booking)
                .HasForeignKey<GuestCheckIn>(g => g.BookingId);

            // Avoid SQL Server multiple cascade paths: restrict deletes on our own tables
            // (Identity tables keep their defaults), then re-enable cascade for owned children.
            var ownNamespace = typeof(HostProperty).Namespace;
            var foreignKeys = builder.Model.GetEntityTypes()
                .Where(e => e.ClrType.Namespace == ownNamespace)
                .SelectMany(e => e.GetForeignKeys())
                .Where(fk => fk.DeleteBehavior == DeleteBehavior.Cascade);

            foreach (var fk in foreignKeys)
            {
                fk.DeleteBehavior = DeleteBehavior.Restrict;
            }

            builder.Entity<PropertyImage>()
                .HasOne(i => i.Property)
                .WithMany(p => p.Images)
                .HasForeignKey(i => i.PropertyId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<GuestDocument>()
                .HasOne(d => d.GuestCheckIn)
                .WithMany(g => g.Documents)
                .HasForeignKey(d => d.GuestCheckInId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
