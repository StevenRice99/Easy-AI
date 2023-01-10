using EasyAI.Agents;

namespace EasyAI.Thinking
{
    /// <summary>
    /// Class which acts as a wrapper for transmitting data between agents.
    /// </summary>
    public class StateEvent
    {
        /// <summary>
        /// The event ID which the receivers will use to identify the type of message.
        /// </summary>
        public readonly int Id;

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
        /// <param name="id">The event ID which the receivers will use to identify the type of message.</param>
        /// <param name="sender">The agent who sent the event.</param>
        /// <param name="details">Used to hold any data within the message.</param>
        public StateEvent(int id, Agent sender, object details)
        {
            Id = id;
            Sender = sender;
            Details = details;
        }
    }
}