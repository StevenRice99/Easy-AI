using EasyAI;
using UnityEngine;

namespace Warehouse.States
{
    [CreateAssetMenu(menuName = "Warehouse/States/Warehouse Mind", fileName = "Warehouse Mind")]
    public class WarehouseMind : EasyState
    {
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