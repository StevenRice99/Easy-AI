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
                InfoStation infoStation;
                switch (infoStations.Count)
                {
                    case < 1:
                        return;
                    case 1:
                        infoStation = infoStations.First();
                        break;
                    default:
                        infoStation = infoStations.OrderBy(x => EasyManager.PathLength(EasyManager.LookupPath(p, x.transform.position), p)).First();
                        break;
                }

                w.Move(infoStation.transform.position);
                if (Vector3.Distance(p, infoStation.MainVisual.position) <= w.InteractDistance && Inbound.Instances.Any(x => x.GetNext() >= 0))
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