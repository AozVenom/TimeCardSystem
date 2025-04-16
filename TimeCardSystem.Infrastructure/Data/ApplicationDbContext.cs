using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TimeCardSystem.Core.Models;

namespace TimeCardSystem.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<TimeEntry> TimeEntries { get; set; }
        public DbSet<Schedule> Schedules { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<TimeEntry>(entity =>
            {
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(te => te.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(te => te.Status)
                    .HasConversion<int>();

                // Ensure properties are configured
                entity.Property(te => te.UserId)
                    .IsRequired();
            });

            modelBuilder.Entity<Schedule>(entity =>
            {
                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(s => s.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(s => s.CreatedById)
                    .OnDelete(DeleteBehavior.Restrict);

                entity.Property(s => s.Status)
                    .HasConversion<int>();

                // Ensure properties are configured
                entity.Property(s => s.UserId)
                    .IsRequired();

                entity.Property(s => s.CreatedById)
                    .IsRequired();

                // Configure new properties
                entity.Property(s => s.HasLunch)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(s => s.LunchDuration)
                    .HasDefaultValue(TimeSpan.FromMinutes(30));

                // Optional: Add index for performance
                entity.HasIndex(s => s.ShiftStart);
                entity.HasIndex(s => s.UserId);
            });
        }

        // Optional: Override SaveChanges to add created/modified timestamps
        public override int SaveChanges()
        {
            AddTimestamps();
            return base.SaveChanges();
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            AddTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        private void AddTimestamps()
        {
            var entries = ChangeTracker
                .Entries()
                .Where(e => e.Entity is Schedule &&
                            (e.State == EntityState.Added || e.State == EntityState.Modified));

            foreach (var entityEntry in entries)
            {
                if (entityEntry.Entity is Schedule schedule)
                {
                    if (entityEntry.State == EntityState.Added)
                    {
                        schedule.CreatedAt = DateTime.UtcNow;
                    }
                    schedule.ModifiedAt = DateTime.UtcNow;
                }
            }
        }
    }
}