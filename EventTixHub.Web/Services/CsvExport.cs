using System.Globalization;
using System.Text;

namespace EventTixHub.Web.Services;

// ';' separator: Bulgarian Excel's list separator. Numbers are invariant ("12.50").
public static class CsvExport
{
    public static string Build(SalesReport report)
    {
        var sb = new StringBuilder();
        void Row(params object[] cells) =>
            sb.Append(string.Join(';', cells.Select(Cell))).Append("\r\n");

        Row($"Справка {report.From:dd.MM.yyyy} - {report.To:dd.MM.yyyy}");
        foreach (var (title, header, rows) in new[] { ("Приход по събития", "Събитие", report.ByEvent), ("Приход по зали", "Зала", report.ByVenue) })
        {
            Row(); Row(title);
            Row(header, "Продадени билети", "Приход (EUR)");
            foreach (var r in rows) Row(r.Name, r.TicketsSold, r.Revenue);
        }
        Row(); Row("Заетост по прожекции");
        Row("Събитие", "Зала", "Начало", "Капацитет", "Заети", "Заетост %");
        foreach (var o in report.Occupancy) Row(o.EventTitle, o.VenueName, o.StartsAt, o.Capacity, o.Taken, o.Percent);
        Row(); Row("Средна заетост по начален час");
        Row("Час", "Прожекции", "Средна заетост %");
        foreach (var h in report.ByHour) Row($"{h.Hour:00}:00", h.Showtimes, h.AveragePercent);
        return sb.ToString();
    }

    private static string Cell(object value)
    {
        var s = value switch
        {
            decimal d => d.ToString("0.00", CultureInfo.InvariantCulture),
            double d => d.ToString("0.0", CultureInfo.InvariantCulture),
            DateTime d => d.ToString("dd.MM.yyyy HH:mm", CultureInfo.InvariantCulture),
            string t => t,
            _ => Convert.ToString(value, CultureInfo.InvariantCulture) ?? ""
        };
        if (value is string && s.Length > 0 && "=+-@".Contains(s[0])) s = "'" + s; // spreadsheet formula injection
        return s.IndexOfAny([';', '"', '\r', '\n']) >= 0 ? $"\"{s.Replace("\"", "\"\"")}\"" : s;
    }
}
