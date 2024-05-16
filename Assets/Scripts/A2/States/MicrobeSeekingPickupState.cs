using A2.Pickups;
using A2.Sensors;
using EasyAI;
using UnityEngine;

namespace A2.States
{
    /// <summary>
    /// State for microbes that are seeking a pickup.
    /// </summary>
    [CreateAssetMenu(menuName = "A2/States/Microbe Seeking Pickup State", fileName = "Microbe Seeking Pickup State")]
    public class MicrobeSeekingPickupState : EasyState
    {
        /// <summary>
        /// Called when an agent first enters this state.
        /// </summary>
        /// <param name="easyAgent">The agent.</param>
        public override void Enter(EasyAgent easyAgent)
        {
            easyAgent.Log("Starting searching for a pickup.");
        }
        
        /// <summary>
        /// Called when an agent is in this state.
        /// </summary>
        /// <param name="easyAgent">The agent.</param>
        public override void Execute(EasyAgent easyAgent)
        {
            if (easyAgent is not Microbe microbe)
            {
                return;
            }

            // If the microbe is not tracking a pickup, search for one.
            if (!microbe.HasPickup)
            {
                microbe.SetPickup(easyAgent.Sense<NearestPickupSensor, MicrobeBasePickup>());
                if (microbe.HasPickup)
                {
                    easyAgent.Log($"Moving to {microbe.Pickup.name}.");
                }
            }

            // If there are no pickups in detection range, roam.
            if (!microbe.HasPickup)
            {
                if (easyAgent.Moving)
                {
                    return;
                }

                easyAgent.Log("Cannot find any pickups, roaming.");
                easyAgent.Move(Random.insideUnitCircle * MicrobeManager.FloorRadius);
                return;
            }
            
            // Otherwise move towards the pickup it is tracking.
            easyAgent.Move(microbe.Pickup.transform);
        }
        
        /// <summary>
        /// Called when an agent exits this state.
        /// </summary>
        /// <param name="easyAgent">The agent.</param>
        public override void Exit(EasyAgent easyAgent)
        {
            if (easyAgent is not Microbe microbe)
            {
                return;
            }

            // Ensure the target pickup is null.
            microbe.RemovePickup();
            easyAgent.Log("No longer searching for a pickup.");
        }
    }
}