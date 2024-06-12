using UnityEngine;

namespace Warehouse
{
    /// <summary>
    /// Interface for locations that can be placed down at.
    /// </summary>
    public interface IPlace
    {
        /// <summary>
        /// Place a part at this location.
        /// </summary>
        /// <param name="agent">The agent placing the part.</param>
        /// <returns>True if the part was added, false otherwise.</returns>
        public bool Place(WarehouseAgent agent);

        /// <summary>
        /// Get the time it would take to place a part at this location.
        /// </summary>
        /// <param name="position">The position the placer is currently at.</param>
        /// <param name="speed">How fast the placer can move.</param>
        /// <returns>The time it would take to place a part at this location.</returns>
        public float PlaceTime(Vector3 position, float speed);
        
        /// <summary>
        /// Check if this can take a part with an ID.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <param name="id">The ID to check for.</param>
        /// <returns>True if it can take a part with the ID, false otherwise.</returns>
        public bool PlaceAvailable(WarehouseAgent agent, int id);

        /// <summary>
        /// Claim an ID for an agent.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <param name="id"></param>
        /// <returns>True if it can be claimed, false otherwise.</returns>
        public bool PlaceClaim(WarehouseAgent agent, int id);
    }
}