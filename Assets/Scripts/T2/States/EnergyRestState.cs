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
        /// <param name="easyAgent">The agent.</param>
        public override void Enter(EasyAgent easyAgent)
        {
            easyAgent.Log("I've got to recharge.");
        }

        /// <summary>
        /// Called when an agent is in this state.
        /// </summary>
        /// <param name="easyAgent">The agent.</param>
        public override void Execute(EasyAgent easyAgent)
        {
            easyAgent.Log("Replenishing...");
            
            // Create deplete energy action.
            easyAgent.Act(new RestoreEnergyAction(easyAgent.Sense<EnergyEasySensor, EnergyComponent>()));
            
            // Get the energy component.
            EnergyComponent energyComponent = easyAgent.Sense<EnergyEasySensor, EnergyComponent>();
            
            // If energy has fully recharged, go into the move state.
            if (energyComponent.Energy >= energyComponent.MaxEnergy)
            {
                easyAgent.SetState<EnergyMoveState>();
            }
        }

        /// <summary>
        /// Called when an agent exits this state.
        /// </summary>
        /// <param name="easyAgent">The agent.</param>
        public override void Exit(EasyAgent easyAgent)
        {
            easyAgent.Log("Got all energy back.");
        }
    }
}