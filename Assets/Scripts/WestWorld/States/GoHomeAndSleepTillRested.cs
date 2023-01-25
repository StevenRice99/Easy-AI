using EasyAI;
using UnityEngine;
using WestWorld.Sensors;

namespace WestWorld.States
{
    [CreateAssetMenu(menuName = "West World/States/Go Home And Sleep Till Rested State", fileName = "Go Home And Sleep Till Rested State")]
    public class GoHomeAndSleepTillRested : State
    {
        public override void Enter(Agent agent)
        {
            Miner miner = agent.Sense<Miner, Miner>();

            if (miner.Location == Miner.WestWorldLocation.Home)
            {
                return;
            }
            
            miner.ChangeLocation(Miner.WestWorldLocation.Home);
            agent.Log("Walkin' home.");
        }

        public override void Execute(Agent agent)
        {
            Miner miner = agent.Sense<Miner, Miner>();
            
            miner.Rest();
            agent.Log("ZZZZ...");

            if (miner.Rested)
            {
                agent.SetState<EnterMineAndDigForNugget>();
            }
        }

        public override void Exit(Agent agent)
        {
            agent.Log("What a God-darn fantastic nap! Time to find more gold.");
        }
    }
}