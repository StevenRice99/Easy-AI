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
    }
}