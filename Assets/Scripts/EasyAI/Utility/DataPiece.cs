namespace EasyAI.Utility
{
    /// <summary>
    /// Base class for non-MonoBehaviour classes being actions and percepts.
    /// </summary>
    public abstract class DataPiece
    {
        /// <summary>
        /// Override to easily display the type of the component for easy usage in messages.
        /// </summary>
        /// <returns>Name of this type.</returns>
        public override string ToString()
        {
            return GetType().Name;
        }
    }
}