using System.Collections.Generic;
using A2.Managers;
using EasyAI.AgentActions;
using EasyAI.Agents;
using EasyAI.Navigation;
using EasyAI.Thinking;
using UnityEngine;

namespace A2.States
{
    /// <summary>
    /// Roaming state for the microbe, doesn't have any actions and only logs messages.
    /// </summary>
    [CreateAssetMenu(menuName = "A2/States/Microbe Roaming State", fileName = "Microbe Roaming State")]
    public class MicrobeRoamingState : State
    {
        /// <summary>
        /// Called when an agent first enters this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override ICollection<AgentAction> Enter(Agent agent)
        {
            agent.AddMessage("Nothing to do, starting to roam.");
            return null;
        }

        /// <summary>
        /// Called when an agent is in this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override ICollection<AgentAction> Execute(Agent agent)
        {
            agent.AddMessage("Roaming.");
            
            if (agent.Moves.Count <= 0)
            {
                agent.Move(Steering.Behaviour.Seek, Random.insideUnitCircle * MicrobeManager.FloorRadius);
            }

            return null;
        }

        /// <summary>
        /// Called when an agent exits this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override ICollection<AgentAction> Exit(Agent agent)
        {
            agent.AddMessage("Got something to do, stopping roaming.");
            return null;
        }
    }
}