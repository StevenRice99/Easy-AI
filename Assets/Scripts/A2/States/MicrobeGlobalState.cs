using A2.Agents;
using A2.Managers;
using EasyAI;
using EasyAI.Agents;
using EasyAI.Thinking;
using UnityEngine;

namespace A2.States
{
    /// <summary>
    /// The global state which microbes are always in.
    /// </summary>
    [CreateAssetMenu(menuName = "A2/States/Microbe Global State")]
    public class MicrobeGlobalState : State
    {
        /// <summary>
        /// Called when an agent is in this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override void Execute(Agent agent)
        {
            base.Execute(agent);

            if (agent is not Microbe microbe)
            {
                return;
            }

            // Determine if the microbe's hunger should increase.
            if (Random.value <= MicrobeManager.MicrobeManagerSingleton.hungerChance * agent.DeltaTime)
            {
                microbe.Hunger++;
            }

            // If the microbe is hungry, set the microbe to seek food.
            if (microbe.IsHungry)
            {
                if (microbe.State.GetType() == typeof(MicrobeSeekingFoodState))
                {
                    return;
                }
                microbe.State = AgentManager.Lookup(typeof(MicrobeSeekingFoodState));
                microbe.SetStateVisual(microbe.State);
                return;
            }

            // If the microbe is an adult, look for either a made or a pickup.
            if (microbe.IsAdult)
            {
                // If the microbe is an adult and has not yet mated, set the microbe to seek a mate.
                if (!microbe.DidMate)
                {
                    if (microbe.State.GetType() == typeof(MicrobeSeekingMateState))
                    {
                        return;
                    }
                    microbe.State = AgentManager.Lookup(typeof(MicrobeSeekingMateState));
                    microbe.SetStateVisual(microbe.State);
                    return;
                }

                // Lastly, if the microbe is not hungry, is an adult, and has mated, set the microbe to look for pickups.
                if (microbe.State.GetType() == typeof(MicrobeSeekingPickupState))
                {
                    return;
                }
                
                microbe.State = AgentManager.Lookup(typeof(MicrobeSeekingPickupState));
                microbe.SetStateVisual(microbe.State);
                return;
            }

            // If the microbe is being hunted, evade it.
            if (microbe.PursuerMicrobe != null)
            {
                if (microbe.State.GetType() == typeof(MicrobeEvadeState))
                {
                    return;
                }
                microbe.State = AgentManager.Lookup(typeof(MicrobeEvadeState));
                microbe.SetStateVisual(microbe.State);
                return;
            }
            
            // Otherwise the microbe goes to sleep.
            if (microbe.State.GetType() == typeof(MicrobeWanderingState))
            {
                return;
            }
            microbe.State = AgentManager.Lookup(typeof(MicrobeWanderingState));
            microbe.SetStateVisual(microbe.State);
        }
        
        /// <summary>
        /// Overridden to handle receiving an event saying a microbe was eaten.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <param name="aiEvent">The event to handle.</param>
        /// <returns>True if it was an eaten message, false otherwise.</returns>
        public override bool HandleEvent(Agent agent, AIEvent aiEvent)
        {
            // Cast the event ID to the microbe events which have been defined for easy identification.
            switch ((MicrobeManager.MicrobeEvents) aiEvent.EventId)
            {
                // If the message is that this microbe is now being hunted.
                case MicrobeManager.MicrobeEvents.Hunted:
                {
                    if (agent is not Microbe microbe || aiEvent.Sender is not Microbe sender)
                    {
                        return false;
                    }
                    
                    // Update that the microbe is being hunted.
                    microbe.PursuerMicrobe = sender;
                    return true;
                }
                // If the message is that this microbe has been eaten.
                case MicrobeManager.MicrobeEvents.Eaten:
                {
                    if (agent is not Microbe microbe || aiEvent.Sender is not Microbe sender)
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