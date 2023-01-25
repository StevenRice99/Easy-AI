using EasyAI;
using UnityEngine;
using WestWorld.Agents;

namespace WestWorld.States
{
    [CreateAssetMenu(menuName = "West World/States/Quench Thirst State", fileName = "Quench Thirst State")]
    public class QuenchThirst : State
    {
        public override void Enter(Agent agent)
        {
            Miner miner = agent as Miner;;

            if (miner.Location == Miner.WestWorldLocation.Saloon)
            {
                return;
            }
            
            miner.ChangeLocation(Miner.WestWorldLocation.Saloon);
            miner.Log("Boy, ah sure is thusty! Walkin' to the saloon");
        }

        public override void Execute(Agent agent)
        {
            Miner miner = agent as Miner;;
            
            miner.Drink();
            miner.Log("That's mighty fine sippin liquor.");
            
            miner.SetState<EnterMineAndDigForNugget>();
        }

        public override void Exit(Agent agent)
        {
            agent.Log("Leavin' the saloon, feelin' good.");
        }
    }
}