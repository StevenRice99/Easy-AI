using EasyAI;
using UnityEngine;
using WestWorld.Sensors;

namespace WestWorld.States
{
    [CreateAssetMenu(menuName = "West World/States/Quench Thirst State", fileName = "Quench Thirst State")]
    public class QuenchThirst : State
    {
        public override void Enter(Agent agent)
        {
            Miner miner = agent.Sense<Miner, Miner>();

            if (miner.Location == Miner.WestWorldLocation.Saloon)
            {
                return;
            }
            
            miner.ChangeLocation(Miner.WestWorldLocation.Saloon);
            agent.Log("Boy, ah sure is thusty! Walkin' to the saloon");
        }

        public override void Execute(Agent agent)
        {
            Miner miner = agent.Sense<Miner, Miner>();
            
            miner.Drink();
            agent.Log("That's mighty fine sippin liquor.");
            
            agent.SetState<EnterMineAndDigForNugget>();
        }

        public override void Exit(Agent agent)
        {
            agent.Log("Leavin' the saloon, feelin' good.");
        }
    }
}