using System;
using EasyAI;
using UnityEngine;
using WestWorld.Agents;

namespace WestWorld.States
{
    [CreateAssetMenu(menuName = "West World/States/West World Mind", fileName = "West World Mind")]
    public class WestWorldMind : State
    {
        public override void Enter(Agent agent)
        {
            if (agent is Miner)
            {
                agent.SetState<EnterMineAndDigForNugget>();
            }
            else
            {
                agent.SetState<DoHousework>();
            }
        }

        public override void Execute(Agent agent)
        {
            if (agent is HouseKeeper)
            {
                if (new System.Random().Next(10) < 1)
                {
                    //agent.SetState<VisitBathroom>();
                }
            }
        }
    }
}