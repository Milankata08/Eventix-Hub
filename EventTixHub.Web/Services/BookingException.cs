namespace EventTixHub.Web.Services;

// Message is Bulgarian and shown to the user as-is.
public class BookingException(string message) : Exception(message);
