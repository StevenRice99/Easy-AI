using EasyAI;
using UnityEngine;
using Warehouse.Sensors;

namespace Warehouse.States
{
    /// <summary>
    /// State for the agent to try and place down a part.
    /// </summary>
    [CreateAssetMenu(menuName = "Warehouse/States/Warehouse Place State", fileName = "Warehouse Place State")]
    public class WarehousePlaceState : EasyState
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
            
            w.Log("Starting to place down.");
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

            if (!w.HasPart)
            {
                return;
            }
            
            if (!w.HasTarget)
            {
                w.Log("No target to put down at, looking for one.");
                w.SetTarget(w.Sense<PlaceSensor, MonoBehaviour>());
            }

            if (w.HasTarget)
            {
                w.Act(w.Target);
                return;
            }

            w.Log("Nowhere to place this item. Destroying it to ensure the simulation does not get locked.");
            w.Destroy();
        }
    }
}