using System;
using System.Linq;
using EasyAI.Utility;

namespace EasyAI
{
    /// <summary>
    /// Base sensor class for sensing percepts and sending them back to the agent where they will be processed by its mind.
    /// </summary>
    public abstract class Sensor : IntelligenceComponent
    {
        /// <summary>
        /// Implement what the sensor will send back to the agent.
        /// </summary>
        /// <returns>The percepts sent back to the agent.</returns>
        public abstract object Sense();

        protected override void OnValidate()
        {
            base.OnValidate();

            if (agent == null || agent.sensors.Contains(this))
            {
                return;                
            }

            Array.Resize(ref agent.sensors, agent.sensors.Length + 1);
            agent.sensors[^1] = this;
        }
    }
}