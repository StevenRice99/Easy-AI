using EasyAI;
using UnityEngine;

namespace Warehouse.Actuators
{
    public class PickActuator : EasyActuator
    {
        public override bool Act(object agentAction)
        {
            if (agent is not WarehouseAgent {HasPart: false} w || agentAction is not IPick pick)
            {
                return false;
            }

            if (!w.CanInteract)
            {
                Log("Not in range to pick up.");
                return false;
            }
            
            Log($"Picking up.");
            return w.CanInteract && pick.Pick(w);
        }
    }
}