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
        /// <param name="easyAgent">The agent.</param>
        public override void Enter(EasyAgent easyAgent)
        {
            easyAgent.Log("Looking for a mate.");
        }
        
        /// <summary>
        /// Called when an agent is in this state.
        /// </summary>
        /// <param name="easyAgent">The agent.</param>
        public override void Execute(EasyAgent easyAgent)
        {
            // If the microbe has already mated, return.
            if (easyAgent is not Microbe {DidMate: false} microbe)
            {
                easyAgent.SetState<MicrobeRoamingState>();
                return;
            }

            // If the microbe is not tracking another microbe to mate with yet, search for one.
            if (!microbe.HasTarget)
            {
                microbe.AttractMate(easyAgent.Sense<NearestMateSensor, Microbe>());
            }

            // If there are no microbes in detection range to mate with or the microbe was rejected, roam.
            if (!microbe.HasTarget)
            {
                if (easyAgent.Moving)
                {
                    return;
                }

                easyAgent.Log("Cannot find a mate, roaming.");
                easyAgent.Move(Random.insideUnitCircle * MicrobeManager.FloorRadius);
                return;
            }

            // If close enough to mate with the microbe it is tracking, mate with it.
            if (!microbe.Mate())
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
            easyAgent.Log("No longer looking for a mate.");
        }
    }
}