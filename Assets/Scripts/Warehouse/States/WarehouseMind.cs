using EasyAI;
using UnityEngine;

namespace Warehouse.States
{
    /// <summary>
    /// Mind of a warehouse agent.
    /// </summary>
    [CreateAssetMenu(menuName = "Warehouse/States/Warehouse Mind", fileName = "Warehouse Mind")]
    public class WarehouseMind : EasyState
    {
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
                w.SetState<WarehousePlaceState>();
            }
            else
            {
                w.SetState<WarehousePickState>();
            }
        }
    }
}