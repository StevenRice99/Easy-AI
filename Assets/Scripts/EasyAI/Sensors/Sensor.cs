using EasyAI.Percepts;
using EasyAI.Utility;

namespace EasyAI.Sensors
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
        public PerceivedData Read()
        {
            PerceivedData data = Sense();
            AddMessage(data == null ? "Did not perceive anything." : $"Perceived {data}.");
            return data;
        }
        
        /// <summary>
        /// Implement what the sensor will send back to the agent.
        /// </summary>
        /// <returns>The percept sent back to the agent.</returns>
        protected abstract PerceivedData Sense();
    }
}