using EasyAI;
using EasyAI.Navigation;
using UnityEngine;

namespace A2.States
{
    /// <summary>
    /// State for microbes that are being hunted.
    /// </summary>
    [CreateAssetMenu(menuName = "A2/States/Microbe Hunted State", fileName = "Microbe Hunted State")]
    public class MicrobeHuntedState : EasyState
    {
        /// <summary>
        /// Called when an agent first enters this state.
        /// </summary>
        /// <param name="easyAgent">The agent.</param>
        public override void Enter(EasyAgent easyAgent)
        {
            easyAgent.Log("Being hunted, starting to evade.");
        }

        /// <summary>
        /// Called when an agent is in this state.
        /// </summary>
        /// <param name="easyAgent">The agent.</param>
        public override void Execute(EasyAgent easyAgent)
        {
            // If no microbe is not being hunted, return.
            if (easyAgent is not Microbe {BeingHunted: true} microbe)
            {
                easyAgent.SetState<MicrobeRoamingState>();
                return;
            }

            // Check if the microbe can detect its pursuer.
            if (Vector3.Distance(microbe.transform.position, microbe.Hunter.transform.position) > microbe.DetectionRange)
            {
                easyAgent.Log("Have a feeling I am being hunted but don't know where they are.");
                easyAgent.SetState<MicrobeRoamingState>();
                return;
            }
            
            // Otherwise move towards the microbe it is tracking.
            easyAgent.Log($"Evading {microbe.Hunter.name}.");
            easyAgent.Move(microbe.Hunter.transform, EasySteering.Behaviour.Evade);
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
            easyAgent.Log("No longer being hunted, stopping evading.");
        }
    }
}