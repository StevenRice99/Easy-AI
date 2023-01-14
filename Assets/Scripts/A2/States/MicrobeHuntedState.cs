using EasyAI;
using EasyAI.Navigation;
using UnityEngine;

namespace A2.States
{
    /// <summary>
    /// State for microbes that are being hunted.
    /// </summary>
    [CreateAssetMenu(menuName = "A2/States/Microbe Hunted State", fileName = "Microbe Hunted State")]
    public class MicrobeHuntedState : State
    {
        /// <summary>
        /// Called when an agent first enters this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override void Enter(Agent agent)
        {
            agent.Log("Being hunted, starting to evade.");
        }

        public override void Execute(Agent agent)
        {
            // If no microbe is not being hunted, return.
            if (agent is not Microbe {BeingHunted: true} microbe)
            {
                agent.SetState<MicrobeRoamingState>();
                return;
            }

            // Check if the microbe can detect its pursuer.
            if (Vector3.Distance(microbe.transform.position, microbe.Hunter.transform.position) > microbe.DetectionRange)
            {
                agent.Log("Have a feeling I am being hunted but don't know where they are.");
                agent.SetState<MicrobeRoamingState>();
                return;
            }
            
            // Otherwise move towards the microbe it is tracking.
            agent.Log($"Evading {microbe.Hunter.name}.");
            agent.Move(microbe.Hunter.transform, Steering.Behaviour.Evade);
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
            agent.Log("No longer being hunted, stopping evading.");
        }
    }
}