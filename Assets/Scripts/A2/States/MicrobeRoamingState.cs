using EasyAI;
using UnityEngine;

namespace A2.States
{
    /// <summary>
    /// Roaming state for the microbe, doesn't have any actions and only logs messages.
    /// </summary>
    [CreateAssetMenu(menuName = "A2/States/Microbe Roaming State", fileName = "Microbe Roaming State")]
    public class MicrobeRoamingState : EasyState
    {
        /// <summary>
        /// Called when an agent first enters this state.
        /// </summary>
        /// <param name="easyAgent">The agent.</param>
        public override void Enter(EasyAgent easyAgent)
        {
            easyAgent.Log("Nothing to do, starting to roam.");
        }

        /// <summary>
        /// Called when an agent is in this state.
        /// </summary>
        /// <param name="easyAgent">The agent.</param>
        public override void Execute(EasyAgent easyAgent)
        {
            if (!easyAgent.Moving)
            {
                easyAgent.Move(MicrobeManager.RandomPosition);
            }
        }
        
        /// <summary>
        /// Called when an agent exits this state.
        /// </summary>
        /// <param name="easyAgent">The agent.</param>
        public override void Exit(EasyAgent easyAgent)
        {
            easyAgent.Log("Got something to do, stopping roaming.");
        }
    }
}