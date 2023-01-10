using System.Collections.Generic;
using System.Linq;
using EasyAI.AgentActions;
using EasyAI.Utility;

namespace EasyAI.Actuators
{
    /// <summary>
    /// Base actuator class for performing actions requested by the agent.
    /// </summary>
    public abstract class Actuator : IntelligenceComponent
    {
        /// <summary>
        /// Implement how the actuator will act to any given action, if at all.
        /// </summary>
        /// <param name="agentAction">The action the agent wants to perform.</param>
        /// <returns>True if the action has been completed, false if it has not been acted upon or it is not yet complete.</returns>
        public abstract bool Act(AgentAction agentAction);
    }
}