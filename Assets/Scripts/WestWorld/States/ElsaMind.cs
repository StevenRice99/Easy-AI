using EasyAI;
using UnityEngine;
using WestWorld.Agents;

namespace WestWorld.States
{
    /// <summary>
    /// The state which the housekeeper is always in.
    /// </summary>
    [CreateAssetMenu(menuName = "West World/States/Elsa Mind", fileName = "Elsa Mind")]
    public class ElsaMind : State
    {
        public override void Execute(Agent agent)
        {
            if (new System.Random().Next(10) != 0)
            {
                return;
            }
            
            // Go to the bathroom.
            agent.SetState<VisitBathroom>();
        }

        public override bool HandleMessage(Agent agent, Agent sender, int id)
        {
            // If the miner got home, start cooking stew.
            if (id == (int) WestWorldAgent.WestWorldMessage.HiHoneyImHome)
            {
                agent.Log("Hi honey. Let me make you some of mah fine country stew.");
                agent.SetState<CookStew>();
            }
            // Otherwise, the only other message type is that the stew is ready, so pass the message to the miner.
            else
            {
                agent.FirstResponseMessage((int) WestWorldAgent.WestWorldMessage.StewReady);
            }

            return true;
        }
    }
}