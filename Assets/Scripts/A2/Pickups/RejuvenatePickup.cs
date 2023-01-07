using A2.Agents;

namespace A2.Pickups
{
    /// <summary>
    /// Pickup to make a microbe younger again to extend its life.
    /// </summary>
    public class RejuvenatePickup : MicrobeBasePickup
    {
        /// <summary>
        /// The behaviour of the pickup.
        /// </summary>
        /// <param name="microbe">The microbe which picked up this pickup.</param>
        protected override void Execute(Microbe microbe)
        {
            microbe.AddMessage("Powered up - has extended life and is now a young adult again!");
            
            // Set back to right when the microbe became an adult.
            microbe.ElapsedLifespan = microbe.LifeSpan / 2;
        }
    }
}