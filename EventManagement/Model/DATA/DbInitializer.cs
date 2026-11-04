using EventManagement.Model.Domain;
using EventManagement.Service;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Model.Data;

public static class DbInitializer
{
    public static async Task SeedAdminsAsync(AppDBContext db, IConfiguration config)
    {
        var password = config["Seed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(password)) return;

        foreach (var section in config.GetSection("Seed:Admins").GetChildren())
        {
            var email = section["Email"]?.Trim().ToLowerInvariant();
            if (string.IsNullOrEmpty(email)) continue;
            if (await db.Users.AnyAsync(u => u.Email == email)) continue;
            if (!Enum.TryParse<DepartmentType>(section["Department"], out var dept)) continue;

            db.Users.Add(new User
            {
                Email = email,
                FullName = section["FullName"] ?? email,
                PasswordHash = PasswordHasher.Hash(password),
                Role = UserRole.Admin,
                AdminDepartment = dept
            });
        }
        await db.SaveChangesAsync();
    }
}