using EasyAI;
using UnityEngine;

namespace T2.States
{
    /// <summary>
    /// The global state which the energy demo agent is always in.
    /// </summary>
    [CreateAssetMenu(menuName = "T2/States/Energy Mind", fileName = "Energy Mind")]
    public class EnergyMind : State
    {
        public override void Enter(Agent agent)
        {
            agent.SetState<EnergyMoveState>();
        }
    }
}