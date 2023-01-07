using EasyAI.Interactions;
using EasyAI.Utility;

namespace EasyAI.Thinking
{
    /// <summary>
    /// The mind which will decide on what actions an agent's actuators will perform based on the percepts it sensed.
    /// </summary>
    public abstract class Mind : IntelligenceComponent
    {
        /// <summary>
        /// Implement to decide what actions the agent's actuators will perform based on the percepts it sensed.
        /// </summary>
        /// <returns>The actions the agent's actuators will perform.</returns>
        public virtual Action[] Think()
        {
            return null;
        }

        /// <summary>
        /// Add a message to this component.
        /// </summary>
        /// <param name="message">The message to add.</param>
        public override void AddMessage(string message)
        {
            Agent.AddMessage(message);
        }
    }
}