using System.Linq;
using EasyAI;
using UnityEngine;

namespace Warehouse.Sensors
{
    /// <summary>
    /// Sensor to find where to pick up a part from.
    /// </summary>
    [DisallowMultipleComponent]
    public class PickSensor : EasySensor
    {
        /// <summary>
        /// Find where to pick up a part from.
        /// </summary>
        /// <returns>Where to pick up a part from or null if there is no place to do so.</returns>
        public override object Sense()
        {
            if (agent is not WarehouseAgent w)
            {
                return null;
            }

            if (w.HasPart)
            {
                Log("Has a part so not searching.");
                return null;
            }

            Vector3 p = w.transform.position;

            // Get all active outbound locations by distance.
            Outbound[] outbounds = Outbound.Instances.Where(x => x.Active).ToArray();
            
            // Check to see if any inbounds or storages have a needed part.
            Inbound bestInbound = null;
            Storage bestStorage = null;
            int bestInboundId = -1;
            int bestStorageId = -1;
            float bestInboundCost = float.MaxValue;
            float bestStorageCost = float.MaxValue;
            foreach (Outbound outbound in outbounds)
            {
                int[] ids = outbound.Requirements();
                Vector3 d = outbound.transform.position;
                foreach (int id in ids)
                {
                    float cost;
                    Inbound inbound = Inbound.Instances.Where(x => x.Has(id)).OrderBy(x => x.PickTime(p, d, w.moveSpeed)).FirstOrDefault();
                    if (inbound != null)
                    {
                        cost = inbound.PickTime(p, d, w.moveSpeed);
                        if (cost < bestInboundCost)
                        {
                            bestInboundCost = cost;
                            bestInbound = inbound;
                            bestInboundId = id;
                        }
                    }
                    
                    if (!Storage.Options.ContainsKey(id))
                    {
                        continue;
                    }
                    
                    Storage storage = Storage.Options[id].Where(x => x.Available(w)).OrderBy(x => x.PickTime(p, d, w.moveSpeed)).FirstOrDefault();
                    if (storage == null)
                    {
                        continue;
                    }

                    cost = storage.PickTime(p, d, w.moveSpeed);
                    if (cost >= bestStorageCost)
                    {
                        continue;
                    }
                    
                    bestStorageCost = cost;
                    bestStorage = storage;
                    bestStorageId = id;
                }
            }

            if (bestStorage == null)
            {
                if (bestInbound != null)
                {
                    Log($"Found item type {bestInboundId} at {bestInbound.name}.");
                    w.SetId(bestInboundId);
                    return bestInbound;
                }
            }
            else
            {
                if (bestInbound == null)
                {
                    Log($"Found item type {bestStorageId} at {bestStorage.name}.");
                    w.SetId(bestStorageId);
                    return bestStorage;
                }

                if (bestInboundCost < bestStorageCost)
                {
                    Log($"Found item type {bestInboundId} at {bestInbound.name}.");
                    w.SetId(bestInboundId);
                    return bestInbound;
                }

                Log($"Found item type {bestStorageId} at {bestStorage.name}.");
                w.SetId(bestStorageId);
                return bestStorage;
            }

            // Move towards the nearest inbound if there are no needed parts.
            Log("No particular IDs needed.");
            w.SetId(-1);
            Inbound anyInbound = Inbound.Instances.Where(x => !x.Empty).OrderBy(x => x.PickTime(p, x.transform.position, w.moveSpeed)).FirstOrDefault();
            Log(anyInbound == null ? "No inbounds to collect from." : $"No order, will unload {anyInbound.name}.");
            return anyInbound;
        }
    }
}