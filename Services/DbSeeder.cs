using KABILIN.Data;
using KABILIN.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KABILIN.Services
{
    public static class DbSeeder
    {
        /// <summary>Applies pending migrations and creates the initial administrator if missing.</summary>
        public static async Task InitialiseAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var hasher = scope.ServiceProvider.GetRequiredService<IPasswordHasher<User>>();
            var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");

            await db.Database.MigrateAsync();

            var email = config["SeedAdmin:Email"]?.Trim().ToLowerInvariant();
            var password = config["SeedAdmin:Password"];
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                logger.LogWarning("SeedAdmin is not configured; no administrator account was created.");
                return;
            }

            if (await db.Users.AnyAsync(u => u.EmailAddress == email)) return;

            var admin = new User
            {
                FirstName = config["SeedAdmin:FirstName"] ?? "KABILIN",
                LastName = config["SeedAdmin:LastName"] ?? "Administrator",
                ContactNum = "N/A",
                EmailAddress = email,
                Address = "KABILIN Office",
                City = "Cebu City",
                Role = AppRoles.Admin,
                IsActive = true,
                MustChangePassword = true
            };
            admin.PasswordHash = hasher.HashPassword(admin, password);
            db.Users.Add(admin);
            await db.SaveChangesAsync();
            logger.LogWarning("Seeded initial administrator {Email}. Change this password before production deployment.", email);
        }
    }
}
