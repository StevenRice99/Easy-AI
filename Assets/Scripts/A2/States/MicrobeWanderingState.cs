using EasyAI.Agents;
using EasyAI.Thinking;
using UnityEngine;

namespace A2.States
{
    /// <summary>
    /// Wandering state for the microbe, doesn't have any actions and only logs messages.
    /// </summary>
    [CreateAssetMenu(menuName = "A2/States/Microbe Wandering State")]
    public class MicrobeWanderingState : State
    {
        /// <summary>
        /// Called when an agent first enters this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override void Enter(Agent agent)
        {
            agent.AddMessage("Nothing to do, starting to wander.");
            agent.ClearMoveData();
            agent.Wander = true;
        }

        /// <summary>
        /// Called when an agent is in this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override void Execute(Agent agent)
        {
            agent.AddMessage("Wandering.");
        }

        /// <summary>
        /// Called when an agent exits this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override void Exit(Agent agent)
        {
            agent.AddMessage("Got something to do, stopping wandering.");
            agent.Wander = false;
        }
    }
}