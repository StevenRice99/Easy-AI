using EasyAI.Interactions;

namespace A1.Actions
{
    /// <summary>
    /// Action to clean a given floor tile.
    /// </summary>
    public class CleanAgentAction : AgentAction
    {
        /// <summary>
        /// The floor to clean.
        /// </summary>
        public Floor Floor;
    }
}