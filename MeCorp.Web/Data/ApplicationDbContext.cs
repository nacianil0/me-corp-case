using MeCorp.Web.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace MeCorp.Web.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<LoginAttempt> LoginAttempts => Set<LoginAttempt>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasIndex(u => u.Email).IsUnique();
            entity.HasIndex(u => u.ReferralCode).IsUnique();

            entity.HasOne(u => u.Referrer)
                .WithMany(u => u.Referrals)
                .HasForeignKey(u => u.ReferredBy)
                .OnDelete(DeleteBehavior.Restrict);

            entity.Property(u => u.CreatedAt)
                .HasConversion(
                    v => v.ToUniversalTime(),
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });

        modelBuilder.Entity<LoginAttempt>(entity =>
        {
            entity.HasIndex(l => l.IpAddress);
            entity.HasIndex(l => l.AttemptTime);

            entity.HasOne(l => l.User)
                .WithMany(u => u.LoginAttempts)
                .HasForeignKey(l => l.UserId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.Property(l => l.AttemptTime)
                .HasConversion(
                    v => v.ToUniversalTime(),
                    v => DateTime.SpecifyKind(v, DateTimeKind.Utc));
        });
    }
}

