using EasyAI.Percepts;

namespace A1.Percepts
{
    /// <summary>
    /// Hold the dirt state of the current tile the agent is on.
    /// </summary>
    public class DirtyData : PerceivedData
    {
        /// <summary>
        /// The floor closest to the agent.
        /// </summary>
        public Floor Floor;

        /// <summary>
        /// Getter for if the closest floor tile is dirty or not.
        /// </summary>
        public bool IsDirty => Floor != null && Floor.State >= Floor.DirtLevel.Dirty;

        /// <summary>
        /// Assign the floor.
        /// </summary>
        /// <param name="floor">The floor.</param>
        public DirtyData(Floor floor)
        {
            Floor = floor;
        }

        /// <summary>
        /// Display the details of the percepts.
        /// </summary>
        /// <returns>String with the details of the percepts.</returns>
        public override string DetailsDisplay()
        {
            return IsDirty ? "Dirty." : "Clean.";
        }
    }
}