using EasyAI;

namespace WestWorld.Agents
{
    /// <summary>
    /// Extended agent to store useful information about the West World game.
    /// </summary>
    public class WestWorldAgent : TransformAgent
    {
        /// <summary>
        /// Different locations in West World for the agents to be at.
        /// Note these locations are only logical, the agents do not actually move in this demonstration.
        /// </summary>
        public enum WestWorldLocation
        {
            Undefined,
            GoldMine,
            Bank,
            Saloon,
            Home
        }
        
        /// <summary>
        /// Message types for the agents to communicate.
        /// </summary>
        public enum WestWorldMessage
        {
            HiHoneyImHome,
            StewReady
        }

        /// <summary>
        /// The current location an agent is at.
        /// </summary>
        public WestWorldLocation Location { get; private set; } = WestWorldLocation.Undefined;

        /// <summary>
        /// Move the agent to a new location.
        /// </summary>
        /// <param name="location">The location to move to.</param>
        public void ChangeLocation(WestWorldLocation location)
        {
            Location = location;
        }
    }
}