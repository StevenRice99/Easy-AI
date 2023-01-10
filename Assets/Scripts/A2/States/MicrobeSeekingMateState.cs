using A2.Agents;
using A2.Managers;
using A2.Sensors;
using EasyAI.Agents;
using EasyAI.Navigation;
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
        public override void Enter(Agent agent)
        {
            agent.AddMessage("Looking for a mate.");
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

            // If the microbe is not tracking another microbe to mate with yet, search for one.
            if (!microbe.HasTarget)
            {
                microbe.AttractMate(agent.Sense<NearestMateSensor, Microbe>());
            }

            // If there are no microbes in detection range to mate with or the microbe was rejected, roam.
            if (!microbe.HasTarget)
            {
                if (agent.Moving)
                {
                    return;
                }

                agent.AddMessage("Cannot find a mate, roaming.");
                agent.Move(Random.insideUnitCircle * MicrobeManager.FloorRadius);
                return;
            }

            // If close enough to mate with the microbe it is tracking, mate with it.
            if (!microbe.Mate())
            {
                // Otherwise move towards the microbe it is tracking.
                agent.Move(microbe.TargetMicrobeTransform, Steering.Behaviour.Pursue);
            }
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

            // Ensure the target microbe is null.
            microbe.RemoveTargetMicrobe();
            agent.AddMessage("No longer looking for a mate.");
        }
    }
}