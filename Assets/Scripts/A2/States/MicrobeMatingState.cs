using A2.Sensors;
using EasyAI;
using EasyAI.Navigation;
using UnityEngine;

namespace A2.States
{
    /// <summary>
    /// State for microbes that are seeking a mate.
    /// </summary>
    [CreateAssetMenu(menuName = "A2/States/Microbe Mating State", fileName = "Microbe Mating State")]
    public class MicrobeMatingState : EasyState
    {
        /// <summary>
        /// Called when an agent first enters this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override void Enter(EasyAgent agent)
        {
            agent.Log("Looking for a mate.");
        }
        
        /// <summary>
        /// Called when an agent is in this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override void Execute(EasyAgent agent)
        {
            // If the microbe has already mated, return.
            if (agent is not Microbe {DidMate: false} microbe)
            {
                agent.SetState<MicrobeRoamingState>();
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

                agent.Log("Cannot find a mate, roaming.");
                agent.Move(Random.insideUnitCircle * MicrobeManager.FloorRadius);
                return;
            }

            // If close enough to mate with the microbe it is tracking, mate with it.
            if (!microbe.Mate())
            {
                // Otherwise move towards the microbe it is tracking.
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
            agent.Log("No longer looking for a mate.");
        }
    }
}