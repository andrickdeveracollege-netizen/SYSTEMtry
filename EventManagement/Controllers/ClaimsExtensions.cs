using System.Security.Claims;
using EventManagement.Model.Domain;
using EventManagement.Service;

namespace EventManagement.Controllers;

// Reads the values we put inside the JWT (see AuthService.BuildResponse)
public static class ClaimsExtensions
{
    public static string Email(this ClaimsPrincipal u) => u.FindFirst("email")!.Value;
    public static string FullName(this ClaimsPrincipal u) => u.FindFirst("name")!.Value;
    public static bool IsAnyAdmin(this ClaimsPrincipal u) => u.IsInRole("Admin") || u.IsInRole("SuperAdmin");

    public static RequesterRole Position(this ClaimsPrincipal u) =>
        Enum.TryParse<RequesterRole>(u.FindFirst("position")?.Value, out var p) ? p : RequesterRole.Faculty;

    public static YearLevel Level(this ClaimsPrincipal u) =>
        Enum.TryParse<YearLevel>(u.FindFirst("level")?.Value, out var l) ? l : YearLevel.None;

    public static DepartmentType? Dept(this ClaimsPrincipal u) =>
        Enum.TryParse<DepartmentType>(u.FindFirst("dept")?.Value, out var d) ? d : null;

    // Admins and the super admin only manage events. They do not request or register.
    public static void EnsureNotAdmin(this ClaimsPrincipal u, string what)
    {
        if (u.IsAnyAdmin()) throw new ServiceException($"Admin accounts cannot {what}.", 403);
    }
}
