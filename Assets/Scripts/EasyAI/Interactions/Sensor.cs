using EasyAI.Utility;

namespace EasyAI.Interactions
{
    /// <summary>
    /// Base sensor class for sensing percepts and sending them back to the agent where they will be processed by its mind.
    /// </summary>
    public abstract class Sensor : IntelligenceComponent
    {
        /// <summary>
        /// Send the percept back to the agent where it will be processed by its mind.
        /// </summary>
        /// <returns>The percept sent back to the agent.</returns>
        public Percept Read()
        {
            Percept percept = Sense();
            AddMessage(percept == null ? "Did not perceive anything." : $"Perceived {percept}.");
            return percept;
        }
        
        /// <summary>
        /// Implement what the sensor will send back to the agent.
        /// </summary>
        /// <returns>The percept sent back to the agent.</returns>
        protected abstract Percept Sense();
    }
}