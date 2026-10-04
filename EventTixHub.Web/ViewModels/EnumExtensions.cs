using System.ComponentModel.DataAnnotations;
using System.Reflection;

namespace EventTixHub.Web.ViewModels;

public static class EnumExtensions
{
    // Bulgarian [Display(Name)] of an enum member, falling back to its name.
    public static string DisplayName(this Enum value) =>
        value.GetType().GetField(value.ToString())?.GetCustomAttribute<DisplayAttribute>()?.Name ?? value.ToString();
}
