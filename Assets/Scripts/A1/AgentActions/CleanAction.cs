using EasyAI.AgentActions;

namespace A1.AgentActions
{
    /// <summary>
    /// Action to clean a given floor tile.
    /// </summary>
    public class CleanAction : AgentAction
    {
        /// <summary>
        /// The floor to clean.
        /// </summary>
        public readonly Floor Floor;

        /// <summary>
        /// Assign the floor.
        /// </summary>
        /// <param name="floor">The floor.</param>
        public CleanAction(Floor floor)
        {
            Floor = floor;
        }
    }
}