using System.Collections.Generic;
using System.Linq;
using A2.Agents;
using A2.Managers;
using A2.Pickups;
using EasyAI.AgentActions;
using EasyAI.Agents;
using EasyAI.Navigation;
using EasyAI.Thinking;
using UnityEngine;

namespace A2.States
{
    /// <summary>
    /// State for microbes that are seeking a pickup.
    /// </summary>
    [CreateAssetMenu(menuName = "A2/States/Microbe Seeking Pickup State", fileName = "Microbe Seeking Pickup State")]
    public class MicrobeSeekingPickupState : State
    {
        /// <summary>
        /// Called when an agent first enters this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override ICollection<AgentAction> Enter(Agent agent)
        {
            agent.AddMessage("Starting searching for a pickup.");
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

            // If the microbe is not tracking a pickup, search for one.
            if (microbe.TargetPickup == null)
            {
                MicrobeBasePickup[] pickups = FindObjectsOfType<MicrobeBasePickup>();
                if (pickups.Length > 0)
                {
                    microbe.TargetPickup = pickups.OrderBy(p => Vector3.Distance(agent.transform.position, p.transform.position)).FirstOrDefault();
                }
                
            }

            // If there are no pickups in detection range, roam.
            if (microbe.TargetPickup == null)
            {
                agent.AddMessage("Cannot find any pickups, roaming.");
                if (agent.MovesData.Count <= 0)
                {
                    agent.Move(Steering.Behaviour.Seek, Random.insideUnitCircle * MicrobeManager.FloorRadius);
                }

                return null;
            }
            
            // Otherwise move towards the pickup it is tracking.
            agent.AddMessage($"Moving to {microbe.TargetPickup.name}.");
            agent.Move(Steering.Behaviour.Seek, microbe.TargetPickup.transform);
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

            // Ensure the target pickup is null.
            microbe.TargetPickup = null;
            agent.AddMessage("No longer searching for a pickup.");
            return null;
        }
    }
}