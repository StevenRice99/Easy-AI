namespace Warehouse
{
    /// <summary>
    /// Interface to allow for certain parts to reset in the world.
    /// </summary>
    public interface IReset
    {
        /// <summary>
        /// Reset this object.
        /// </summary>
        public void ResetObject();
    }
}