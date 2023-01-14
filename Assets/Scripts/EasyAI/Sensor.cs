using EasyAI.Utility;

namespace EasyAI
{
    /// <summary>
    /// Base sensor class for sensing percepts and sending them back to the agent where they will be processed by its mind.
    /// </summary>
    public abstract class Sensor : IntelligenceComponent
    {
        /// <summary>
        /// Send the percepts back to the agent where it will be processed by its mind.
        /// </summary>
        /// <returns>The percepts sent back to the agent.</returns>
        public object Read()
        {
            object data = Sense();
            Log(data == null ? "Did not perceive anything." : $"Perceived {data}.");
            return data;
        }
        
        /// <summary>
        /// Implement what the sensor will send back to the agent.
        /// </summary>
        /// <returns>The percepts sent back to the agent.</returns>
        protected abstract object Sense();
    }
}