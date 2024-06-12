using EasyAI;
using UnityEngine;

namespace Warehouse.Actuators
{
    /// <summary>
    /// Actuator to place down a part.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlaceActuator : EasyActuator
    {
        /// <summary>
        /// Try to place down a part.
        /// </summary>
        /// <param name="agentAction">An IPlace instance to place down to.</param>
        /// <returns>True if a part was placed down, false otherwise.</returns>
        public override bool Act(object agentAction)
        {
            if (agent is not WarehouseAgent w || agentAction is not IPlace place)
            {
                return false;
            }

            if (!w.HasPart)
            {
                Log("Does not have a part to place.");
                return false;
            }

            if (!w.CanInteract)
            {
                Log("Not in range to place down.");
                return false;
            }

            Log("Placing down.");
            if (!place.Place(w))
            {
                return false;
            }

            if (WarehouseManager.Communication)
            {
                return true;
            }

            w.NeedsInfo = true;
            return true;
        }
    }
}