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
        bool Place(WarehouseAgent agent);
    }
}