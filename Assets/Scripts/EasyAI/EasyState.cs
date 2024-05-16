using UnityEngine;

namespace EasyAI
{
    /// <summary>
    /// Base class for agent states.
    /// </summary>
    public abstract class EasyState : ScriptableObject
    {
        /// <summary>
        /// Called when an agent first enters this state.
        /// </summary>
        /// <param name="easyAgent">The agent.</param>
        public virtual void Enter(EasyAgent easyAgent) { }

        /// <summary>
        /// Called when an agent is in this state.
        /// </summary>
        /// <param name="easyAgent">The agent.</param>
        public virtual void Execute(EasyAgent easyAgent) { }

        /// <summary>
        /// Called when an agent exits this state.
        /// </summary>
        /// <param name="easyAgent">The agent.</param>
        public virtual void Exit(EasyAgent easyAgent) { }

        /// <summary>
        /// Handle a message.
        /// </summary>
        /// <param name="easyAgent">The agent.</param>
        /// <param name="sender">The agent that sent the message.</param>
        /// <param name="id">The message type.</param>
        /// <returns>True if the message was accepted and acted upon, false otherwise.</returns>
        public virtual bool HandleMessage(EasyAgent easyAgent, EasyAgent sender, int id)
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