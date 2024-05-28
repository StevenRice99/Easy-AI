using EasyAI;
using UnityEngine;
using Warehouse.Sensors;

namespace Warehouse.States
{
    [CreateAssetMenu(menuName = "Warehouse/States/Warehouse Place State", fileName = "Warehouse Place State")]
    public class WarehousePlaceState : EasyState
    {
        public override void Enter(EasyAgent agent)
        {
            if (agent is not WarehouseAgent w)
            {
                return;
            }
            
            w.Log("Starting to place down.");
            w.SetTarget();
        }
        
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