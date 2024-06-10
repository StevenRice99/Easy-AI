using EasyAI;

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
        /// <param name="agent">The agent placing the part.</param>
        /// <returns>The time it would take to place a part at this location.</returns>
        public float PlaceTime(EasyAgent agent);
    }
}