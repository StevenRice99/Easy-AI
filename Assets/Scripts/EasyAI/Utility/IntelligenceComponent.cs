using UnityEngine;

namespace EasyAI.Utility
{
    /// <summary>
    /// Base component for sensors, actuators, minds, and performance measures.
    /// </summary>
    public abstract class IntelligenceComponent : MessageComponent
    {
        /// <summary>
        /// The agent this component is connected to.
        /// </summary>
        [HideInInspector]
        public Agent agent;

        protected virtual void OnValidate()
        {
            // Find the agent to attach to.
            Transform t = transform;
            do
            {
                agent = t.GetComponent<Agent>();
                if (agent != null)
                {
                    return;
                }

                t = t.parent;
            } while (t != null);
        }
    }
}