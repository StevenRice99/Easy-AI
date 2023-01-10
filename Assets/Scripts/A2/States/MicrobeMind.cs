using A2.Agents;
using A2.Managers;
using EasyAI.Agents;
using EasyAI.Thinking;
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
                microbe.SetState<MicrobeSeekingFoodState>();
                return;
            }

            // If the microbe is an adult, look for either a made or a pickup.
            if (microbe.IsAdult)
            {
                // If the microbe is an adult and has not yet mated, set the microbe to seek a mate.
                if (!microbe.DidMate)
                {
                    microbe.SetState<MicrobeSeekingMateState>();
                    return;
                }

                // Lastly, if the microbe is not hungry, is an adult, and has mated, set the microbe to look for pickups.
                microbe.SetState<MicrobeSeekingPickupState>();
                return;
            }

            // If the microbe is being hunted, evade it.
            if (microbe.PursuerMicrobe != null)
            {
                microbe.SetState<MicrobeEvadeState>();
                return;
            }
            
            // Otherwise the microbe goes to sleep.
            microbe.SetState<MicrobeRoamingState>();
        }
        
        /// <summary>
        /// Overridden to handle receiving an event saying a microbe was eaten.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <param name="stateEvent">The event to handle.</param>
        /// <returns>True if it was an eaten message, false otherwise.</returns>
        public override bool HandleEvent(Agent agent, StateEvent stateEvent)
        {
            // Cast the event ID to the microbe events which have been defined for easy identification.
            switch ((MicrobeManager.MicrobeEvents) stateEvent.Id)
            {
                // If the message is that this microbe is now being hunted.
                case MicrobeManager.MicrobeEvents.Hunted:
                {
                    if (agent is not Microbe microbe || stateEvent.Sender is not Microbe sender)
                    {
                        return false;
                    }
                    
                    // Update that the microbe is being hunted.
                    microbe.SetPursuerMicrobe(sender);
                    return true;
                }
                // If the message is that this microbe has been eaten.
                case MicrobeManager.MicrobeEvents.Eaten:
                {
                    if (agent is not Microbe microbe || stateEvent.Sender is not Microbe sender)
                    {
                        return false;
                    }
            
                    // Have the sender microbe eat the receiving microbe.
                    sender.Eat(microbe);
                    microbe.Die();
                    return true;
                }
                // Otherwise do nothing.
                case MicrobeManager.MicrobeEvents.Impress:
                case MicrobeManager.MicrobeEvents.Mate:
                default:
                    return false;
            }
        }
    }
}