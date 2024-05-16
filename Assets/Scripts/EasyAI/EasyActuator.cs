using System;
using System.Linq;
using EasyAI.Utility;

namespace EasyAI
{
    /// <summary>
    /// Base actuator class for performing actions requested by the agent.
    /// </summary>
    public abstract class EasyActuator : EasyComponent
    {
        /// <summary>
        /// Implement how the actuator will act to any given action, if at all.
        /// </summary>
        /// <param name="agentAction">The action the agent wants to perform.</param>
        /// <returns>True if the action has been completed, false otherwise.</returns>
        public abstract bool Act(object agentAction);
        
        /// <summary>
        /// Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();

            if (easyAgent == null || easyAgent.actuators.Contains(this))
            {
                return;                
            }

            Array.Resize(ref easyAgent.actuators, easyAgent.actuators.Length + 1);
            easyAgent.actuators[^1] = this;
        }
    }
}