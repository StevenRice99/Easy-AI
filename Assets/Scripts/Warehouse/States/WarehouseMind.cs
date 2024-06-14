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

            // If information is needed, we need to find an information terminal.
            if (w.NeedsInfo)
            {
                // Use role-specific terminals only if in a role-based mode.
                HashSet<InfoStation> infoStations = WarehouseManager.Roles ? InfoStation.Instances.Where(x => x.Inbound == w.Inbound).ToHashSet() : InfoStation.Instances;

                // Cache the agent's position.
                Vector3 p = w.transform.position;
                
                // Determine the info station.
                InfoStation infoStation;
                switch (infoStations.Count)
                {
                    // Nothing to do if there are no info stations.
                    case < 1:
                        return;
                    // Go to the only inbound station if only one exists.
                    case 1:
                        infoStation = infoStations.First();
                        break;
                    // Go to the nearest inbound station if there are multiple.
                    default:
                        infoStation = infoStations.OrderBy(x => EasyManager.PathLength(EasyManager.LookupPath(p, x.transform.position), p)).First();
                        break;
                }

                // Move towards the info station.
                w.Move(infoStation.transform.position);
                
                // If close enough, get the information.
                if (Vector3.Distance(p, infoStation.MainVisual.position) <= w.InteractDistance && Inbound.Instances.Any(x => x.GetNext() >= 0))
                {
                    w.NeedsInfo = false;
                }
                // Otherwise, not at the info station yet so perform no other actions.
                else
                {
                    return;
                }
            }

            // Try to place or pick depending on whether the agent has a part.
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