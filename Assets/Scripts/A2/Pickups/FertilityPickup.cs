using A2.Agents;

namespace A2.Pickups
{
    /// <summary>
    /// Pickup to make a microbe fertile again.
    /// </summary>
    public class FertilityPickup : MicrobeBasePickup
    {
        /// <summary>
        /// The behaviour of the pickup.
        /// </summary>
        /// <param name="microbe">The microbe which picked up this pickup.</param>
        protected override void Execute(Microbe microbe)
        {
            microbe.AddMessage("Powered up -  can now mate again!");
            microbe.DidMate = false;
        }
    }
}