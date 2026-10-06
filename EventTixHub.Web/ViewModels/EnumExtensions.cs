using System.ComponentModel.DataAnnotations;
using System.Reflection;
using EventTixHub.Web.Models;

namespace EventTixHub.Web.ViewModels;

public static class EnumExtensions
{
    // Bulgarian [Display(Name)] of an enum member, falling back to its name.
    public static string DisplayName(this Enum value) =>
        value.GetType().GetField(value.ToString())?.GetCustomAttribute<DisplayAttribute>()?.Name ?? value.ToString();

    public static string TypeClass(this EventType type) => type switch
    {
        EventType.Concert => "type-concert",
        EventType.Theatre => "type-theatre",
        _ => "type-movie"
    };

    public static string ShowtimeNoun(this EventType type, bool plural = false) => (type, plural) switch
    {
        (EventType.Concert, false) => "концерт",
        (EventType.Concert, true) => "концерти",
        (EventType.Theatre, false) => "представление",
        (EventType.Theatre, true) => "представления",
        (_, false) => "прожекция",
        _ => "прожекции"
    };

    public static string ShowtimeThis(this EventType type) => type switch
    {
        EventType.Concert => "този",
        EventType.Theatre => "това",
        _ => "тази"
    };

    public static string HallFront(this EventType type) => type == EventType.Movie ? "ЕКРАН" : "СЦЕНА";

    public static string InvitationLine(this EventType type) => type switch
    {
        EventType.Concert => "Очакваме ви на концерта",
        EventType.Theatre => "Очакваме ви на представлението",
        _ => "Очакваме ви на прожекцията"
    };
}
