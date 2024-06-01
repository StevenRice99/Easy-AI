using EasyAI;
using T2.Actions;
using T2.Sensors;
using UnityEngine;

namespace T2.States
{
    /// <summary>
    /// The state which the energy demo agent rests in.
    /// </summary>
    [CreateAssetMenu(menuName = "T2/States/Energy Rest State", fileName = "Energy Rest State")]
    public class EnergyRestState : EasyState
    {
        /// <summary>
        /// Called when an agent first enters this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override void Enter(EasyAgent agent)
        {
            agent.Log("I've got to recharge.");
        }

        /// <summary>
        /// Called when an agent is in this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override void Execute(EasyAgent agent)
        {
            agent.Log("Replenishing...");
            
            // Get the energy component.
            EnergyComponent energyComponent = agent.Sense<EnergySensor, EnergyComponent>();
            
            // Create deplete energy action.
            agent.Act(new RestoreEnergyAction(energyComponent));
            
            // If energy has fully recharged, go into the move state.
            if (energyComponent.Energy >= energyComponent.MaxEnergy)
            {
                agent.SetState<EnergyMoveState>();
            }
        }

        /// <summary>
        /// Called when an agent exits this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override void Exit(EasyAgent agent)
        {
            agent.Log("Got all energy back.");
        }
    }
}