using System.Collections.Generic;
using System.Linq;
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

            if (w.NeedsInfo)
            {
                HashSet<InfoStation> infoStations = WarehouseManager.Roles ? InfoStation.Instances.Where(x => x.Inbound == w.Inbound).ToHashSet() : InfoStation.Instances;

                Vector3 p = w.transform.position;
                Vector3 target;
                switch (infoStations.Count)
                {
                    case < 1:
                        return;
                    case 1:
                        target = infoStations.First().transform.position;
                        break;
                    default:
                        target = infoStations.OrderBy(x => EasyManager.PathLength(EasyManager.LookupPath(p, x.transform.position), p)).First().transform.position;
                        break;
                }

                w.Move(target);
                if (Vector3.Distance(p, target) <= w.InteractDistance && Inbound.Instances.Any(x => x.GetRandom() >= 0))
                {
                    w.NeedsInfo = false;
                }
                else
                {
                    return;
                }
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