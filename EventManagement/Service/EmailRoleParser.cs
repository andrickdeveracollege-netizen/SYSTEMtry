using EventManagement.Model.Domain;

namespace EventManagement.Service;

// Angelicum email formats:
//   students:             firstname.surname.college | .shs | .jhs | .elem  @ustangelicum.edu.ph
//   faculty / staff:      firstname.surname                                @ustangelicum.edu.ph
public static class EmailRoleParser
{
    private static readonly Dictionary<string, YearLevel> Levels = new(StringComparer.OrdinalIgnoreCase)
    {
        ["college"] = YearLevel.College,
        ["shs"] = YearLevel.SHS,
        ["jhs"] = YearLevel.JHS,
        ["elem"] = YearLevel.Elementary
    };

    public static (RequesterRole Position, YearLevel Level) Parse(string email)
    {
        var local = email.Split('@')[0];
        var parts = local.Split('.', StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length >= 3 && Levels.TryGetValue(parts[^1], out var level))
            return (RequesterRole.Student, level);
        if (parts.Length == 2)
            return (RequesterRole.Faculty, YearLevel.None);

        throw new ServiceException(
            "Use your Angelicum email: firstname.surname.college / .shs / .jhs / .elem for students, " +
            "or firstname.surname for faculty and staff.");
    }
}
