using A2.Sensors;
using EasyAI;
using EasyAI.Navigation;
using UnityEngine;

namespace A2.States
{
    /// <summary>
    /// State for microbes that are hungry and wanting to seek food.
    /// </summary>
    [CreateAssetMenu(menuName = "A2/States/Microbe Hungry State", fileName = "Microbe Hungry State")]
    public class MicrobeHungryState : EasyState
    {
        /// <summary>
        /// Called when an agent first enters this state.
        /// </summary>
        /// <param name="easyAgent">The agent.</param>
        public override void Enter(EasyAgent easyAgent)
        {
            easyAgent.Log("Starting to search for food.");
        }
        
        /// <summary>
        /// Called when an agent is in this state.
        /// </summary>
        /// <param name="easyAgent">The agent.</param>
        public override void Execute(EasyAgent easyAgent)
        {
            // If the microbe is not hungry, stop hunting.
            if (easyAgent is not Microbe {IsHungry: true} microbe)
            {
                easyAgent.SetState<MicrobeRoamingState>();
                return;
            }

            // If the microbe is not tracking another microbe to eat yet, search for one.
            if (!microbe.HasTarget)
            {
                microbe.StartHunting(easyAgent.Sense<NearestPreySensor, Microbe>());
            }

            // If there are no microbes in detection range to eat, roam.
            if (!microbe.HasTarget)
            {
                if (easyAgent.Moving)
                {
                    return;
                }

                easyAgent.Log("Cannot find any food, roaming.");
                easyAgent.Move(Random.insideUnitCircle * MicrobeManager.FloorRadius);
                return;
            }

            // If close enough to eat the microbe it is tracking, eat it.
            if (!microbe.Eat())
            {
                // Otherwise move towards the microbe it is tracking.
                easyAgent.Move(microbe.TargetMicrobeTransform, EasySteering.Behaviour.Pursue);
            }
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

            // Ensure the target microbe is null.
            microbe.RemoveTargetMicrobe();
            easyAgent.Log("No longer searching for food.");
        }
    }
}