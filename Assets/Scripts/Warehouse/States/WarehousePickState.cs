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

            // Cannot pick if they have a part.
            if (w.HasPart)
            {
                return;
            }
            
            // If there is no target to pick up from, look for one.
            if (!w.HasTarget)
            {
                agent.Log("No target to pick up from, looking for one.");
                w.SetTarget(w.Sense<PickSensor, MonoBehaviour>());
            }

            // Try and pick up from the target if it has one.
            if (w.HasTarget)
            {
                w.Act(w.Target);
            }
        }
    }
}