using EventManagement.Model.Domain;
using EventManagement.Service;
using Microsoft.EntityFrameworkCore;

namespace EventManagement.Model.Data;

public static class DbInitializer
{
    public static async Task SeedAsync(AppDBContext db, IConfiguration config)
    {
        await SeedSuperAdminAsync(db, config);
        await SeedOfficeAdminsAsync(db, config);
        await SeedTestUsersAsync(db, config);
        await SeedSampleEventAsync(db);
    }

    // admin@ustangelicum.edu.ph: resets passwords, posts institutional events, approves any office
    private static async Task SeedSuperAdminAsync(AppDBContext db, IConfiguration config)
    {
        var email = config["Seed:SuperAdmin:Email"]?.Trim().ToLowerInvariant();
        var password = config["Seed:AdminPassword"];
        if (string.IsNullOrEmpty(email) || string.IsNullOrWhiteSpace(password)) return;
        if (await db.Users.AnyAsync(u => u.Email == email)) return;

        db.Users.Add(new User
        {
            Email = email,
            FullName = config["Seed:SuperAdmin:FullName"] ?? "Super Admin",
            PasswordHash = PasswordHasher.Hash(password),
            Role = UserRole.SuperAdmin,
            Position = RequesterRole.Administrator
        });
        await db.SaveChangesAsync();
    }

    private static async Task SeedOfficeAdminsAsync(AppDBContext db, IConfiguration config)
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
                AdminDepartment = dept,
                Position = RequesterRole.Administrator
            });
        }
        await db.SaveChangesAsync();
    }

    // One account per kind of school user. Role and level come from the email format.
    private static async Task SeedTestUsersAsync(AppDBContext db, IConfiguration config)
    {
        foreach (var section in config.GetSection("Seed:Users").GetChildren())
        {
            var email = section["Email"]?.Trim().ToLowerInvariant();
            var password = section["Password"];
            if (string.IsNullOrEmpty(email) || string.IsNullOrWhiteSpace(password)) continue;
            if (await db.Users.AnyAsync(u => u.Email == email)) continue;

            var (position, level) = EmailRoleParser.Parse(email);
            if (Enum.TryParse<RequesterRole>(section["Position"], out var forced)) position = forced;

            db.Users.Add(new User
            {
                Email = email,
                FullName = section["FullName"] ?? email,
                PasswordHash = PasswordHasher.Hash(password),
                Role = UserRole.User,
                Position = position,
                Level = level
            });
        }
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
            RequesterEmail = "juan.delacruz.college@ustangelicum.edu.ph",
            Organization = "Student Council",
            Role = RequesterRole.Student,
            TargetAudience = YearLevel.All,
            Purpose = "Kick-off program for the school intramurals.",
            Capacity = 150,
            StartTime = start,
            EndTime = start.AddHours(3),
            ManageCode = "SEED0001",
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
