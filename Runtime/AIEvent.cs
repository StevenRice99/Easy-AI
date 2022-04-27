/// <summary>
/// Class which acts as a wrapper for transmitting data between agents.
/// </summary>
public class AIEvent
{
    /// <summary>
    /// The event ID which the receivers will use to identify the type of message.
    /// </summary>
    public readonly int EventId;

    /// <summary>
    /// The agent who sent the event.
    /// </summary>
    public readonly Agent Sender;

    /// <summary>
    /// Used to hold any data within the message.
    /// </summary>
    public object Details;

    /// <summary>
    /// Create a new event.
    /// </summary>
    /// <param name="eventId">The event ID which the receivers will use to identify the type of message.</param>
    /// <param name="sender">The agent who sent the event.</param>
    /// <param name="details">Used to hold any data within the message.</param>
    public AIEvent(int eventId, Agent sender, object details)
    {
        EventId = eventId;
        Sender = sender;
        Details = details;
    }
}