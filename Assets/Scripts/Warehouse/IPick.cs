using UnityEngine;

namespace Warehouse
{
    /// <summary>
    /// Interface for locations that can be picked up from.
    /// </summary>
    public interface IPick
    {
        /// <summary>
        /// Pick a part to an agent.
        /// </summary>
        /// <param name="agent">The agent picking the part.</param>
        /// <returns>True if it was picked up, false otherwise.</returns>
        public bool Pick(WarehouseAgent agent);

        /// <summary>
        /// How long it would for this agent to pick up from this and then deliver to a spot to place it.
        /// </summary>
        /// <param name="position">The position the picker is currently at.</param>
        /// <param name="place">The place to deliver to.</param>
        /// <param name="speed">How fast the picker can move.</param>
        /// <returns>The time it would take an agent to collect from this and deliver it to the outbound.</returns>
        public float PickTime(Vector3 position, Vector3 place, float speed);
    }
}