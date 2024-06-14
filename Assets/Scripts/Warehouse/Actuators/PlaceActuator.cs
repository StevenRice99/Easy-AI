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

            // Cannot place if not holding a part.
            if (!w.HasPart)
            {
                Log("Does not have a part to place.");
                return false;
            }

            // Check if in range to interact.
            if (!w.CanInteract)
            {
                Log("Not in range to place down.");
                return false;
            }

            // Try and place the item down.
            Log("Placing down.");
            if (!place.Place(w))
            {
                return false;
            }

            // If in wireless communication, we are able to get info to make the next decision immediately.
            if (WarehouseManager.Wireless)
            {
                return true;
            }

            // Otherwise, flag that the agent must visit a terminal to get new information.
            w.NeedsInfo = true;
            return true;
        }
    }
}