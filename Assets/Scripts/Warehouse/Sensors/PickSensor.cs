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

            // Get all active outbound locations by distance.
            Outbound[] outbounds = Outbound.Instances.Where(x => x.Active).ToArray();
            
            // Check to see if any inbounds have a needed part.
            Inbound bestInbound = null;
            int bestInboundId = -1;
            float bestInboundCost = float.MaxValue;
            foreach (Outbound outbound in outbounds)
            {
                int[] ids = outbound.Requirements();
                Vector3 p = outbound.transform.position;
                foreach (int id in ids)
                {
                    Inbound inbound = Inbound.Instances.Where(x => x.Has(id)).OrderBy(x => x.PickTime(w, p)).FirstOrDefault();
                    if (inbound == null)
                    {
                        continue;
                    }

                    float cost = inbound.PickTime(w, p);
                    if (cost >= bestInboundCost)
                    {
                        continue;
                    }

                    bestInboundCost = cost;
                    bestInbound = inbound;
                    bestInboundId = id;
                }
            }

            // Check to see if any storages have a needed part.
            Storage bestStorage = null;
            int bestStorageId = -1;
            float bestStorageCost = float.MaxValue;
            foreach (Outbound outbound in outbounds)
            {
                int[] ids = outbound.Requirements();
                Vector3 p = outbound.transform.position;
                foreach (int id in ids)
                {
                    Storage storage = Storage.Instances.Where(x => x.Available(w) && x.Has(id)).OrderBy(x => x.PickTime(w, p)).FirstOrDefault();
                    if (storage == null)
                    {
                        continue;
                    }

                    float cost = storage.PickTime(w, p);
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
                else
                {
                    Log($"Found item type {bestStorageId} at {bestStorage.name}.");
                    w.SetId(bestStorageId);
                    return bestStorage;
                }
            }

            // Move towards the nearest inbound if there are no needed parts.
            Log("No particular IDs needed.");
            w.SetId(-1);
            Inbound anyInbound = Inbound.Instances.OrderBy(x => x.Empty).ThenByDescending(x => x.ElapsedTime).ThenBy(x => x.PickTime(agent, x.transform.position)).FirstOrDefault();
            Log(anyInbound == null ? "No inbounds to collect from." : $"No order, going to {anyInbound.name}.");
            return anyInbound;
        }
    }
}