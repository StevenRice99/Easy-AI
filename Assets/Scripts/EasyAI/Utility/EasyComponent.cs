using UnityEngine;

namespace EasyAI.Utility
{
    /// <summary>
    /// Base component for sensors, actuators, minds, and performance measures.
    /// </summary>
    public abstract class EasyComponent : EasyMessage
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
    }
}