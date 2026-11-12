using EventManagement.Model.Domain;

namespace EventManagement.Service;

public static class EmailRoleParser
{
    public static (RequesterRole Position, YearLevel Level) Parse(string email)
    {
        var localPart = email.Trim().ToLowerInvariant().Split('@')[0];
        var suffix = localPart.Split('.').LastOrDefault();
        var level = suffix switch
        {
            "elem" or "elementary" => YearLevel.Elementary,
            "jhs" => YearLevel.JHS,
            "shs" => YearLevel.SHS,
            "college" => YearLevel.College,
            _ => YearLevel.None
        };

        return level == YearLevel.None
            ? (RequesterRole.Faculty, YearLevel.None)
            : (RequesterRole.Student, level);
    }
}
