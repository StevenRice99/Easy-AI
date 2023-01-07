using System.Collections.Generic;
using A2.Agents;
using A2.Managers;
using EasyAI.AgentActions;
using EasyAI.Agents;
using EasyAI.Thinking;
using UnityEngine;

namespace A2.States
{
    /// <summary>
    /// State for microbes that are seeking a mate.
    /// </summary>
    [CreateAssetMenu(menuName = "A2/States/Microbe Seeking Mate State", fileName = "Microbe Seeking Mate State")]
    public class MicrobeSeekingMateState : State
    {
        /// <summary>
        /// Called when an agent first enters this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override ICollection<AgentAction> Enter(Agent agent)
        {
            agent.AddMessage("Looking for a mate.");
            return null;
        }
        
        /// <summary>
        /// Called when an agent is in this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override ICollection<AgentAction> Execute(Agent agent)
        {
            if (agent is not Microbe microbe)
            {
                return null;
            }

            // If the microbe is not tracking another microbe to mate with yet, search for one.
            if (microbe.TargetMicrobe == null)
            {
                // If a potential mate was found, attempt to impress them.
                Microbe potentialMate = MicrobeManager.MicrobeManagerSingleton.FindMate(microbe);
                if (potentialMate != null)
                {
                    agent.AddMessage($"Attempting to impress {potentialMate.name} to mate.");
                    bool accepted = microbe.FireEvent(potentialMate, (int) MicrobeManager.MicrobeEvents.Impress);
                    if (accepted)
                    {
                        // If the other microbe agreed to mate, set them as the target microbe.
                        agent.AddMessage($"{potentialMate.name} accepted advances to mate.");
                        microbe.TargetMicrobe = potentialMate;
                    }
                    else
                    {
                        agent.AddMessage($"Could not mate with {potentialMate.name}.");
                    }
                }
            }

            // If there are no microbes in detection range to mate with, roam.
            if (microbe.TargetMicrobe == null)
            {
                agent.AddMessage("Cannot find a mate, wandering.");
                if (agent.MovesData.Count > 0)
                {
                    return null;
                }

                agent.ClearMoveData();
                agent.Wander = true;
                return null;
            }

            // If close enough to mate with the microbe it is tracking, mate with it.
            if (Vector3.Distance(microbe.transform.position, microbe.TargetMicrobe.transform.position) <= MicrobeManager.MicrobeManagerSingleton.MicrobeInteractRadius)
            {
                if (microbe.FireEvent(microbe.TargetMicrobe, (int) MicrobeManager.MicrobeEvents.Mate))
                {
                    agent.AddMessage($"Mating with {microbe.TargetMicrobe.name}.");
                    microbe.DidMate = true;
                    microbe.PlayMateAudio();
                }
                return null;
            }
            
            // Otherwise move towards the microbe it is tracking.
            agent.AddMessage($"Moving to mate with {microbe.TargetMicrobe.name}.");
            agent.SetMoveData(Agent.MoveType.Pursuit, microbe.TargetMicrobe.transform);
            return null;
        }
        
        /// <summary>
        /// Called when an agent exits this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override ICollection<AgentAction> Exit(Agent agent)
        {
            if (agent is not Microbe microbe)
            {
                return null;
            }

            // Ensure the target microbe is null.
            microbe.TargetMicrobe = null;
            agent.AddMessage("No longer looking for a mate.");
            return null;
        }
        
        /// <summary>
        /// Overridden to handle receiving an event saying a microbe wants to mate with or is mating with this microbe.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <param name="aiEvent">The event to handle.</param>
        /// <returns>True if an event was accepted by the receiver, false otherwise.</returns>
        public override bool HandleEvent(Agent agent, AIEvent aiEvent)
        {
            // Cast the event ID to the microbe events which have been defined for easy identification.
            switch ((MicrobeManager.MicrobeEvents) aiEvent.EventId)
            {
                // If the message is to impress this microbe to mate with them.
                case MicrobeManager.MicrobeEvents.Impress:
                {
                    // Determine if the receiver can mate or not and return true if it can, false otherwise.
                    if (agent is not Microbe { IsAdult: true } microbe || microbe.DidMate || microbe.TargetMicrobe != null || aiEvent.Sender is not Microbe sender)
                    {
                        agent.AddMessage($"Cannot mate with {aiEvent.Sender.name}.");
                        return false;
                    }

                    agent.AddMessage($"Accepted advances of {sender.name}.");
                    microbe.TargetMicrobe = sender;
                    return true;
                }
                // If the message is to mate with this microbe, mate.
                case MicrobeManager.MicrobeEvents.Mate:
                {
                    if (agent is not Microbe microbe)
                    {
                        return false;
                    }

                    microbe.DidMate = true;

                    Microbe sender = aiEvent.Sender as Microbe;
                    if (sender != null)
                    {
                        sender.DidMate = true;
                        int offspring = MicrobeManager.MicrobeManagerSingleton.Mate(microbe, sender);
                        agent.AddMessage(offspring == 0
                            ? $"Failed to have any offspring with {aiEvent.Sender.name}."
                            : $"Have {offspring} offspring with {aiEvent.Sender.name}."
                        );
                    }
                    return true;
                }
                // Otherwise do nothing.
                case MicrobeManager.MicrobeEvents.Eaten:
                case MicrobeManager.MicrobeEvents.Hunted:
                default:
                    return false;
            }
        }
    }
}