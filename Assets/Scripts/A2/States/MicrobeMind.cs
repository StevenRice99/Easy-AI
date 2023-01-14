using EasyAI;
using UnityEngine;

namespace A2.States
{
    /// <summary>
    /// The global state which microbes are always in.
    /// </summary>
    [CreateAssetMenu(menuName = "A2/States/Microbe Mind", fileName = "Microbe Mind")]
    public class MicrobeMind : State
    {
        /// <summary>
        /// Called when an agent is in this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override void Execute(Agent agent)
        {
            if (agent is not Microbe microbe)
            {
                return;
            }

            // If the microbe is hungry, set the microbe to seek food.
            if (microbe.IsHungry)
            {
                microbe.SetState<MicrobeHungryState>();
                return;
            }

            // If the microbe is an adult, look for either a made or a pickup.
            if (microbe.IsAdult)
            {
                // If the microbe is an adult and has not yet mated, set the microbe to seek a mate.
                if (!microbe.DidMate)
                {
                    microbe.SetState<MicrobeMatingState>();
                    return;
                }

                // Lastly, if the microbe is not hungry, is an adult, and has mated, set the microbe to look for pickups.
                microbe.SetState<MicrobeSeekingPickupState>();
                return;
            }

            // If the microbe is being hunted, evade it.
            if (microbe.BeingHunted)
            {
                microbe.SetState<MicrobeHuntedState>();
                return;
            }
            
            // Otherwise the microbe goes to sleep.
            microbe.SetState<MicrobeRoamingState>();
        }
    }
}