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
        foreach (var (email, (password, role, referralCode)) in SeedUsers)
        {
            var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Email == email);

            if (existingUser is null)
            {
                var newUser = new User
                {
                    Email = email,
                    PasswordHash = hashingService.HashPassword(password),
                    Role = role,
                    ReferralCode = referralCode,
                    CreatedAt = DateTime.UtcNow
                };

                context.Users.Add(newUser);
                logger.LogInformation("Seed user created: {Email}", email);
            }
            else
            {
                bool passwordValid = hashingService.VerifyPassword(password, existingUser.PasswordHash);

                if (!passwordValid)
                {
                    existingUser.PasswordHash = hashingService.HashPassword(password);
                    logger.LogInformation("Seed user password hash updated: {Email}", email);
                }
            }
        }

        await context.SaveChangesAsync();
    }
}
