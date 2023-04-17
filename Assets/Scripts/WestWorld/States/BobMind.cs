using EasyAI;
using UnityEngine;
using WestWorld.Agents;

namespace WestWorld.States
{
    /// <summary>
    /// The state which the miner is always in.
    /// </summary>
    [CreateAssetMenu(menuName = "West World/States/Bob Mind", fileName = "Bob Mind")]
    public class BobMind : State
    {
        public override void Execute(Agent agent)
        {
            Miner miner = agent as Miner;
            if (miner != null)
            {
                miner.IncreaseThirst();
            }
        }
    }
}