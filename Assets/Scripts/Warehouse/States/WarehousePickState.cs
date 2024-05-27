using EasyAI;
using UnityEngine;
using Warehouse.Sensors;

namespace Warehouse.States
{
    [CreateAssetMenu(menuName = "Warehouse/States/Warehouse Pick State", fileName = "Warehouse Pick State")]
    public class WarehousePickState : EasyState
    {
        public override void Enter(EasyAgent agent)
        {
            agent.Log("Starting to pick up.");
        }

        public override void Execute(EasyAgent agent)
        {
            if (agent is not WarehouseAgent w)
            {
                return;
            }
            
            if (!w.HasTarget)
            {
                agent.Log("No target to pick up at, looking for one.");
                w.SetTarget(w.Sense<PickSensor, MonoBehaviour>());
            }

            if (w.HasTarget)
            {
                w.Act(w.Target);
            }
        }
    }
}