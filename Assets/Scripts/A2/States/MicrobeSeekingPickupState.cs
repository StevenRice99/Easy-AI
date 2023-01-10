using A2.Agents;
using A2.Managers;
using A2.Pickups;
using A2.Sensors;
using EasyAI.Agents;
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
        public override void Enter(Agent agent)
        {
            agent.AddMessage("Starting searching for a pickup.");
        }

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

            // If the microbe is not tracking a pickup, search for one.
            if (!microbe.HasPickup)
            {
                microbe.SetPickup(agent.Sense<NearestPickupSensor, MicrobeBasePickup>());
                if (microbe.HasPickup)
                {
                    agent.AddMessage($"Moving to {microbe.Pickup.name}.");
                }
            }

            // If there are no pickups in detection range, roam.
            if (!microbe.HasPickup)
            {
                if (agent.Moving)
                {
                    return;
                }

                agent.AddMessage("Cannot find any pickups, roaming.");
                agent.Move(Random.insideUnitCircle * MicrobeManager.FloorRadius);
                return;
            }
            
            // Otherwise move towards the pickup it is tracking.
            agent.Move(microbe.Pickup.transform);
        }

        /// <summary>
        /// Called when an agent exits this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override void Exit(Agent agent)
        {
            if (agent is not Microbe microbe)
            {
                return;
            }

            // Ensure the target pickup is null.
            microbe.RemovePickup();
            agent.AddMessage("No longer searching for a pickup.");
        }
    }
}