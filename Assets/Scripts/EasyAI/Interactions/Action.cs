using EasyAI.Utility;

namespace EasyAI.Interactions
{
    /// <summary>
    /// Base class for actions which will be performed by actuators.
    /// </summary>
    public abstract class Action : DataPiece
    {
        /// <summary>
        /// If the action has been completed or not.
        /// </summary>
        public bool Complete;
    }
}