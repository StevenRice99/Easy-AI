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
    public class EnergyRestState : State
    {
        public override void Enter(Agent agent)
        {
            agent.Log("I've got to recharge.");
        }

        public override void Execute(Agent agent)
        {
            agent.Log("Replenishing...");
            
            // Create deplete energy action.
            agent.Act(new RestoreEnergyAction(agent.Sense<EnergySensor, EnergyComponent>()));
            
            // Get the energy component.
            EnergyComponent energyComponent = agent.Sense<EnergySensor, EnergyComponent>();
            
            // If energy has fully recharged, go into the move state.
            if (energyComponent.Energy >= energyComponent.MaxEnergy)
            {
                agent.SetState<EnergyMoveState>();
            }
        }

        public override void Exit(Agent agent)
        {
            agent.Log("Got all energy back.");
        }
    }
}