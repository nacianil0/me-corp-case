using MeCorp.Web.Data;
using MeCorp.Web.Data.Entities;
using MeCorp.Web.Domain.Enums;
using MeCorp.Web.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace MeCorp.Web.Extensions;

public static class DatabaseSeeder
{
    public static async Task SeedDataAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var hashingService = scope.ServiceProvider.GetRequiredService<IHashingService>();

        if (await context.Users.AnyAsync())
        {
            return;
        }

        var users = new List<User>
        {
            new User
            {
                Email = "admin@mecorp.com",
                PasswordHash = hashingService.HashPassword("Admin123!"),
                Role = UserRole.Admin,
                ReferralCode = "ADMIN2026SEED",
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Email = "manager@mecorp.com",
                PasswordHash = hashingService.HashPassword("Manager123!"),
                Role = UserRole.Manager,
                ReferralCode = "MGR2026SEED01",
                CreatedAt = DateTime.UtcNow
            },
            new User
            {
                Email = "customer@mecorp.com",
                PasswordHash = hashingService.HashPassword("Customer123!"),
                Role = UserRole.Customer,
                ReferralCode = "CUST2026SEED",
                CreatedAt = DateTime.UtcNow
            }
        };

        context.Users.AddRange(users);
        await context.SaveChangesAsync();
    }
}

