using EasyAI;
using UnityEngine;
using Warehouse.Sensors;

namespace Warehouse.States
{
    /// <summary>
    /// State for the agent to try and pick up a part.
    /// </summary>
    [CreateAssetMenu(menuName = "Warehouse/States/Warehouse Pick State", fileName = "Warehouse Pick State")]
    public class WarehousePickState : EasyState
    {
        /// <summary>
        /// Called when an agent first enters this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override void Enter(EasyAgent agent)
        {
            if (agent is not WarehouseAgent w)
            {
                return;
            }
            
            w.Log("Starting to pick up.");
            w.SetTarget();
        }

        /// <summary>
        /// Called when an agent is in this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override void Execute(EasyAgent agent)
        {
            if (agent is not WarehouseAgent w)
            {
                return;
            }

            if (w.HasPart)
            {
                return;
            }
            
            if (!w.HasTarget)
            {
                agent.Log("No target to pick up at, looking for one.");
                w.SetTarget(w.Sense<PickSensor, MonoBehaviour>());
            }

            if (w.HasTarget)
            {
                w.Act(w.Target);
            }
        }
    }
}