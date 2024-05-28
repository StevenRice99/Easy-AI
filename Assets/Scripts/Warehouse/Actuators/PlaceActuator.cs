using EasyAI;

namespace Warehouse.Actuators
{
    public class PlaceActuator : EasyActuator
    {
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
            return place.Place(w);
        }
    }
}