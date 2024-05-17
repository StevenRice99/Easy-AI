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
        /// <param name="agent">The agent.</param>
        public override void Enter(EasyAgent agent)
        {
            agent.Log("Ready to move.");
        }

        /// <summary>
        /// Called when an agent is in this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override void Execute(EasyAgent agent)
        {
            agent.Log("Moving randomly to burn this energy.");
            Vector2 random = Random.insideUnitCircle;
            agent.Move(agent.transform.position + new Vector3(random.x, 0, random.y));
            
            // Create deplete energy action.
            agent.Act(new DepleteEnergyAction(agent.Sense<EnergySensor, EnergyComponent>()));
            
            // Get the energy component.
            EnergyComponent energyComponent = agent.Sense<EnergySensor, EnergyComponent>();
            
            // If out of energy, go into the rest state.
            if (energyComponent.Energy <= 0)
            {
                agent.SetState<EnergyRestState>();
            }
        }

        /// <summary>
        /// Called when an agent exits this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override void Exit(EasyAgent agent)
        {
            agent.Log("Been moving for a while, getting tired.");
        }
    }
}