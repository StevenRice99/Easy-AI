using A2.Agents;
using EasyAI.Agents;
using EasyAI.Thinking;
using UnityEngine;

namespace A2.States
{
    /// <summary>
    /// State for microbes that are evading being hunted.
    /// </summary>
    [CreateAssetMenu(menuName = "A2/States/Microbe Evade State")]
    public class MicrobeEvadeState : State
    {
        /// <summary>
        /// Called when an agent first enters this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override void Enter(Agent agent)
        {
            agent.AddMessage("Being hunted, starting to evade.");
        }

        public override void Execute(Agent agent)
        {
            // If no microbe is pursuing this microbe, return.
            if (agent is not Microbe microbe || microbe.PursuerMicrobe == null)
            {
                return;
            }

            // Check if the microbe can detect its pursuer.
            if (Vector3.Distance(microbe.transform.position, microbe.PursuerMicrobe.transform.position) > microbe.DetectionRange)
            {
                agent.AddMessage("Have a feeling I am being hunted but don't know where they are.");
                agent.ClearMoveData();
            }
            
            // Otherwise move towards the microbe it is tracking.
            agent.AddMessage($"Evading {microbe.PursuerMicrobe.name}.");
            agent.SetMoveData(Agent.MoveType.Evade, microbe.PursuerMicrobe.transform);
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
            microbe.TargetMicrobe = null;
            agent.AddMessage("No longer being hunted, stopping evading.");
        }
    }
}