using EasyAI;
using UnityEngine;
using WestWorld.Sensors;

namespace WestWorld.States
{
    [CreateAssetMenu(menuName = "West World/States/West World Mind", fileName = "West World Mind")]
    public class WestWorldMind : State
    {
        public override void Enter(Agent agent)
        {
            Miner miner = agent.Sense<Miner, Miner>();
            if (miner != null)
            {
                agent.SetState<EnterMineAndDigForNugget>();
            }
        }
    }
}