using EventManagement.Model.Domain;
using EventManagement.Service;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Model.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDBContext db, IConfiguration config)
    {
        await SeedAdminsAsync(db, config);
        await SeedTestUserAsync(db, config);
        await SeedSampleEventAsync(db);
    }

    private static async Task SeedAdminsAsync(AppDBContext db, IConfiguration config)
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

    // A regular (attendee / organizer) account so every role can be tried right away
    private static async Task SeedTestUserAsync(AppDBContext db, IConfiguration config)
    {
        var email = config["Seed:TestUser:Email"]?.Trim().ToLowerInvariant();
        var password = config["Seed:TestUser:Password"];
        if (string.IsNullOrEmpty(email) || string.IsNullOrWhiteSpace(password)) return;
        if (await db.Users.AnyAsync(u => u.Email == email)) return;

        db.Users.Add(new User
        {
            Email = email,
            FullName = config["Seed:TestUser:FullName"] ?? "Test User",
            PasswordHash = PasswordHasher.Hash(password),
            Role = UserRole.User
        });
        await db.SaveChangesAsync();
    }

    // One approved upcoming event so the bulletin board and registration have something to show
    private static async Task SeedSampleEventAsync(AppDBContext db)
    {
        if (await db.Events.AnyAsync()) return;

        var auditorium = await db.Facilities.FirstAsync(f => f.Name == "Auditorium");
        var start = DateTime.Today.AddDays(7).AddHours(9);

        db.Events.Add(new CampusEvent
        {
            Title = "Intramurals Opening Ceremony",
            RequesterName = "Sample Organizer",
            RequesterEmail = "student@ustangelicum.edu.ph",
            Organization = "Student Council",
            Role = RequesterRole.Student,
            TargetAudience = YearLevel.All,
            Purpose = "Kick-off program for the school intramurals.",
            Capacity = 200,
            StartTime = start,
            EndTime = start.AddHours(3),
            Status = EventStatus.Approved,
            DepartmentRequests =
            {
                new DepartmentRequest
                {
                    Department = DepartmentType.Auxiliary,
                    Status = EventStatus.Approved,
                    ReviewedBy = "seed",
                    ReviewedAt = DateTime.Now,
                    Facilities = { new EventFacility { FacilityId = auditorium.Id } }
                }
            }
        });
        await db.SaveChangesAsync();
    }
}
