namespace EventTixHub.Web.ViewModels;

// Page links reuse the current action; Route holds the filters to keep.
public record PagerVM(int Page, int TotalPages, IReadOnlyDictionary<string, string?> Route)
{
    public Dictionary<string, string> LinkFor(int page)
    {
        var data = Route.Where(kv => !string.IsNullOrEmpty(kv.Value)).ToDictionary(kv => kv.Key, kv => kv.Value!);
        data["page"] = page.ToString();
        return data;
    }
}
