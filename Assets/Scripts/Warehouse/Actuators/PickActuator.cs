using EasyAI;

namespace Warehouse.Actuators
{
    public class PickActuator : EasyActuator
    {
        public override bool Act(object agentAction)
        {
            if (agent is not WarehouseAgent w || agentAction is not IPick pick)
            {
                return false;
            }

            if (w.HasPart)
            {
                Log("Already have a part.");
                return false;
            }

            if (!w.CanInteract)
            {
                Log("Not in range to pick up.");
                return false;
            }
            
            Log("Picking up.");
            return pick.Pick(w);
        }
    }
}