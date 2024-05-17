using UnityEngine;

namespace EasyAI.Utility
{
    /// <summary>
    /// Base component for sensors, actuators, minds, and performance measures.
    /// </summary>
    public abstract class EasyComponent : MonoBehaviour
    {
        /// <summary>
        /// The agent this component is connected to.
        /// </summary>
        [HideInInspector]
        public EasyAgent agent;

        /// <summary>
        /// Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
        /// </summary>
        protected virtual void OnValidate()
        {
            // Find the agent to attach to.
            Transform t = transform;
            do
            {
                agent = t.GetComponent<EasyAgent>();
                if (agent != null)
                {
                    return;
                }

                t = t.parent;
            } while (t != null);
        }

        /// <summary>
        /// Log a message to the agent.
        /// </summary>
        /// <param name="message">The message to log.</param>
        public void Log(string message)
        {
            agent.Log($"{ToString()} - {message}");
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