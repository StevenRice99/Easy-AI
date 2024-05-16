using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("agent")] [HideInInspector]
        public EasyAgent easyAgent;

        /// <summary>
        /// Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
        /// </summary>
        protected virtual void OnValidate()
        {
            // Find the agent to attach to.
            Transform t = transform;
            do
            {
                easyAgent = t.GetComponent<EasyAgent>();
                if (easyAgent != null)
                {
                    return;
                }

                t = t.parent;
            } while (t != null);
        }
    }
}