using EasyAI;
using UnityEngine;
using WestWorld.Agents;

namespace WestWorld.States
{
    [CreateAssetMenu(menuName = "West World/States/Enter Mine And Dig For Nugget State", fileName = "Enter Mine And Dig For Nugget State")]
    public class EnterMineAndDigForNugget : State
    {
        public override void Enter(Agent agent)
        {
            Miner miner = agent as Miner;;

            if (miner.Location == Miner.WestWorldLocation.GoldMine)
            {
                return;
            }

            miner.ChangeLocation(Miner.WestWorldLocation.GoldMine);
            miner.Log("Walkin' to the gold mine.");
        }

        public override void Execute(Agent agent)
        {
            Miner miner = agent as Miner;

            miner.IncreaseFatigue(1);
            miner.AddToGoldCarried(1);
            miner.Log("Pickin' up a nugget.");

            if (miner.PocketsFull)
            {
                miner.SetState<VisitBankAndDepositGold>();
                return;
            }

            if (miner.Thirsty)
            {
                miner.SetState<QuenchThirst>();
            }
        }

        public override void Exit(Agent agent)
        {
            agent.Log("Ah'm leavin' the gold mine with mah pockets full o' sweet gold.");
        }
    }
}