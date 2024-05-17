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
        /// <param name="agent">The agent.</param>
        public override void Enter(EasyAgent agent)
        {
            agent.Log("Starting to search for food.");
        }
        
        /// <summary>
        /// Called when an agent is in this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override void Execute(EasyAgent agent)
        {
            // If the microbe is not hungry, stop hunting.
            if (agent is not Microbe {IsHungry: true} microbe)
            {
                agent.SetState<MicrobeRoamingState>();
                return;
            }

            // If the microbe is not tracking another microbe to eat yet, search for one.
            if (!microbe.HasTarget)
            {
                microbe.StartHunting(agent.Sense<NearestPreySensor, Microbe>());
            }

            // If there are no microbes in detection range to eat, roam.
            if (!microbe.HasTarget)
            {
                if (agent.Moving)
                {
                    return;
                }

                agent.Log("Cannot find any food, roaming.");
                agent.Move(Random.insideUnitCircle * MicrobeManager.FloorRadius);
                return;
            }

            // If close enough to eat the microbe it is tracking, eat it.
            if (microbe.Eat())
            {
                agent.AddReward(1);
            }
            // Otherwise move towards the microbe it is tracking.
            else
            {
                agent.Move(microbe.TargetMicrobeTransform, EasySteering.Behaviour.Pursue);
            }
        }
        
        /// <summary>
        /// Called when an agent exits this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override void Exit(EasyAgent agent)
        {
            if (agent is not Microbe microbe)
            {
                return;
            }

            // Ensure the target microbe is null.
            microbe.RemoveTargetMicrobe();
            agent.Log("No longer searching for food.");
        }
    }
}