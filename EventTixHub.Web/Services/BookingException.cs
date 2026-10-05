namespace EventTixHub.Web.Services;

// Message is Bulgarian and shown to the user as-is.
public class BookingException(string message) : Exception(message);

// A poster upload problem; the controller shows it under the Poster field.
public class PosterException(string message) : BookingException(message);
