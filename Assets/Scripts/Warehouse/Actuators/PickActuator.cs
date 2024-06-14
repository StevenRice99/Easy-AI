using EasyAI;
using UnityEngine;

namespace Warehouse.Actuators
{
    /// <summary>
    /// Actuator to pick up a part.
    /// </summary>
    [DisallowMultipleComponent]
    public class PickActuator : EasyActuator
    {
        /// <summary>
        /// Try to pick up a part.
        /// </summary>
        /// <param name="agentAction">An IPick instance to pick up from.</param>
        /// <returns>True if a part was picked up, false otherwise.</returns>
        public override bool Act(object agentAction)
        {
            if (agent is not WarehouseAgent w || agentAction is not IPick pick)
            {
                return false;
            }

            // No need to try and pick up a new part if already holding a part.
            if (w.HasPart)
            {
                Log("Already have a part.");
                return false;
            }

            // Check if in range to interact.
            if (!w.CanInteract)
            {
                Log("Not in range to pick up.");
                return false;
            }
            
            // Perform the pick.
            Log("Picking up.");
            return pick.Pick(w);
        }
    }
}