using EasyAI;
using UnityEngine;

namespace A2.States
{
    /// <summary>
    /// Roaming state for the microbe, doesn't have any actions and only logs messages.
    /// </summary>
    [CreateAssetMenu(menuName = "A2/States/Microbe Roaming State", fileName = "Microbe Roaming State")]
    public class MicrobeRoamingState : State
    {
        public override void Enter(Agent agent)
        {
            agent.Log("Nothing to do, starting to roam.");
        }

        public override void Execute(Agent agent)
        {
            if (!agent.Moving)
            {
                agent.Move(MicrobeManager.RandomPosition);
            }
        }
        
        public override void Exit(Agent agent)
        {
            agent.Log("Got something to do, stopping roaming.");
        }
    }
}