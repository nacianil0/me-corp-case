using MeCorp.Web.Data;
using MeCorp.Web.Data.Entities;
using MeCorp.Web.Domain.Enums;
using MeCorp.Web.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MeCorp.Web.Extensions;

public static class DatabaseSeeder
{
    private static readonly Dictionary<string, (string Password, UserRole Role, string ReferralCode)> SeedUsers = new()
    {
        ["admin@mecorp.com"] = ("Admin123!", UserRole.Admin, "ADMIN2026SEED"),
        ["manager@mecorp.com"] = ("Manager123!", UserRole.Manager, "MGR2026SEED01"),
        ["customer@mecorp.com"] = ("Customer123!", UserRole.Customer, "CUST2026SEED")
    };

    public static async Task SeedDataAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hashingService = scope.ServiceProvider.GetRequiredService<IHashingService>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ApplicationDbContext>>();

        await EnsureSeedUsersAsync(context, hashingService, logger);
    }

    private static async Task EnsureSeedUsersAsync(
        ApplicationDbContext context,
        IHashingService hashingService,
        ILogger logger)
    {
        bool hasChanges = false;

        foreach (var (email, (password, role, referralCode)) in SeedUsers)
        {
            var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (existingUser is null)
            {
                string newHash = hashingService.HashPassword(password);
                var newUser = new User
                {
                    Email = email,
                    PasswordHash = newHash,
                    Role = role,
                    ReferralCode = referralCode,
                    CreatedAt = DateTime.UtcNow
                };

                context.Users.Add(newUser);
                hasChanges = true;
                logger.LogInformation("Seed user created: {Email}", email);
            }
            else
            {
                bool passwordValid = false;
                try
                {
                    passwordValid = hashingService.VerifyPassword(password, existingUser.PasswordHash);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Password verification failed for {Email}, will update hash", email);
                }

                // Only update if the password hash is invalid
                if (!passwordValid)
                {
                    string newHash = hashingService.HashPassword(password);
                    existingUser.PasswordHash = newHash;
                    hasChanges = true;
                    logger.LogInformation("Seed user password hash updated: {Email}", email);
                }
                else
                {
                    logger.LogInformation("Seed user password hash is valid: {Email}", email);
                }
            }
        }

        if (hasChanges)
        {
            await context.SaveChangesAsync();
            logger.LogInformation("Seed data saved successfully");
        }
    }
}
