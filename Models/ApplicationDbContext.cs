using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using TimeCardSystem.Models;

namespace TimeCardSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<User>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<TimeEntry> TimeEntries { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure TimeEntry entity
            modelBuilder.Entity<TimeEntry>(entity =>
            {
                // Configure relationship with User
                entity.HasOne(te => te.User)
                    .WithMany(u => u.TimeEntries)
                    .HasForeignKey(te => te.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Convert enum to int for storage
                entity.Property(te => te.Status)
                    .HasConversion<int>();
            });

            // Additional configurations can be added here
        }
    }
}