using EasyAI.Agents;
using UnityEngine;

namespace EasyAI.Thinking
{
    /// <summary>
    /// Base class for agent states.
    /// </summary>
    public abstract class State : ScriptableObject
    {
        /// <summary>
        /// Called when an agent first enters this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public virtual void Enter(Agent agent) { }

        /// <summary>
        /// Called when an agent is in this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public virtual void Execute(Agent agent) { }

        /// <summary>
        /// Called when an agent exits this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public virtual void Exit(Agent agent) { }

        /// <summary>
        /// Handle receiving an event.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <param name="aiEvent">The event to handle.</param>
        /// <returns>False.</returns>
        public virtual bool HandleEvent(Agent agent, AIEvent aiEvent)
        {
            return false;
        }

        /// <summary>
        /// Override to easily display the type of the component for easy usage in messages.
        /// </summary>
        /// <returns>Name of this type.</returns>
        public override string ToString()
        {
            return GetType().Name;
        }
    }
}