using UnityEngine;

namespace Samples
{
    /// <summary>
    /// Use percepts to hold and pass data from the sensors back to the agent.
    /// </summary>
    public class SamplePercept : Percept
    {
        /// <summary>
        /// Since this sample is randomly moving agents, pass the position the agent should move to.
        /// </summary>
        public Vector3 Position;
    }
}