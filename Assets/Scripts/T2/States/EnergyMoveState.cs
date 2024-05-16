using EasyAI;
using T2.Actions;
using T2.Sensors;
using UnityEngine;

namespace T2.States
{
    /// <summary>
    /// The state which the energy demo agent moves in.
    /// </summary>
    [CreateAssetMenu(menuName = "T2/States/Energy Move State", fileName = "Energy Move State")]
    public class EnergyMoveState : EasyState
    {
        /// <summary>
        /// Called when an agent first enters this state.
        /// </summary>
        /// <param name="easyAgent">The agent.</param>
        public override void Enter(EasyAgent easyAgent)
        {
            easyAgent.Log("Ready to move.");
        }

        /// <summary>
        /// Called when an agent is in this state.
        /// </summary>
        /// <param name="easyAgent">The agent.</param>
        public override void Execute(EasyAgent easyAgent)
        {
            easyAgent.Log("Moving randomly to burn this energy.");
            Vector2 random = Random.insideUnitCircle;
            easyAgent.Move(easyAgent.transform.position + new Vector3(random.x, 0, random.y));
            
            // Create deplete energy action.
            easyAgent.Act(new DepleteEnergyAction(easyAgent.Sense<EnergyEasySensor, EnergyComponent>()));
            
            // Get the energy component.
            EnergyComponent energyComponent = easyAgent.Sense<EnergyEasySensor, EnergyComponent>();
            
            // If out of energy, go into the rest state.
            if (energyComponent.Energy <= 0)
            {
                easyAgent.SetState<EnergyRestState>();
            }
        }

        /// <summary>
        /// Called when an agent exits this state.
        /// </summary>
        /// <param name="easyAgent">The agent.</param>
        public override void Exit(EasyAgent easyAgent)
        {
            easyAgent.Log("Been moving for a while, getting tired.");
        }
    }
}