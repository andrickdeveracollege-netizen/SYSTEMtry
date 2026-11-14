using EventManagement.Model.DTO;

namespace EventManagement.Service;

public static class RegistrantExporter
{
    public const string ContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";

    public static (byte[] Bytes, string FileName) ToExcel(RegistrantListDto list)
    {
        var rows = new List<string[]>
        {
            new[] { "Event", list.Title },
            new[] { "Date", list.StartTime.ToString("MMMM d, yyyy h:mm tt") },
            new[] { "Registered", $"{list.Rows.Count} of {list.Capacity}" },
            Array.Empty<string>(),
            new[] { "No.", "Name", "Email", "Level", "Confirmed", "Registered on" }
        };

        int n = 1;
        foreach (var r in list.Rows)
            rows.Add(new[]
            {
                n++.ToString(), r.FullName, r.Email, r.Level,
                r.Confirmed ? "Yes" : "No", r.RegisteredAt.ToString("yyyy-MM-dd h:mm tt")
            });

        var bytes = XlsxWriter.Build("Registrants", rows, new HashSet<int> { 4 }, new double[] { 6, 30, 38, 16, 11, 22 });

        var safe = new string(list.Title.Where(c => char.IsLetterOrDigit(c) || c == ' ' || c == '-').ToArray()).Trim();
        if (safe.Length == 0) safe = "event";
        if (safe.Length > 40) safe = safe[..40];
        return (bytes, $"registrants-{safe.Replace(' ', '-')}-{list.EventId}.xlsx");
    }
}
