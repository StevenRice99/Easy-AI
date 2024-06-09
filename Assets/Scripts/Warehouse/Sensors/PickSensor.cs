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
            Vector3 p = w.transform.position;
            Outbound[] outbounds = Outbound.Instances.Where(x => x.Active).OrderBy(x => EasyManager.PathLength(EasyManager.LookupPath(p, x.transform.position), p)).ToArray();
            
            // Check to see if any inbounds have a needed part.
            foreach (Outbound outbound in outbounds)
            {
                float bestCost = float.MaxValue;
                Inbound bestInbound = null;
                int bestId = -1;

                int[] ids = outbound.Requirements();
                foreach (int id in ids)
                {
                    Inbound inbound = Inbound.Instances.Where(x => x.Has(id)).OrderBy(x => EasyManager.PathLength(EasyManager.LookupPath(p, x.transform.position), p)).FirstOrDefault();
                    if (inbound == null)
                    {
                        continue;
                    }

                    float cost = EasyManager.PathLength(EasyManager.LookupPath(p, inbound.transform.position), p);
                    if (cost >= bestCost)
                    {
                        continue;
                    }

                    bestCost = cost;
                    bestInbound = inbound;
                    bestId = id;
                }

                if (bestInbound == null)
                {
                    continue;
                }

                Log($"Found item type {bestId} at {bestInbound.name}.");
                w.SetId(bestId);
                return bestInbound;
            }

            // Check to see if any storages have a needed part.
            foreach (Outbound outbound in outbounds)
            {
                float bestCost = float.MaxValue;
                Storage bestStorage = null;
                int bestId = -1;

                int[] ids = outbound.Requirements();
                foreach (int id in ids)
                {
                    Storage storage = Storage.Instances.Where(x => x.Available(w) && x.Has(id)).OrderBy(x => x.Cost).ThenBy(x => EasyManager.PathLength(EasyManager.LookupPath(p, x.MoveTarget), p)).FirstOrDefault();
                    if (storage == null)
                    {
                        continue;
                    }
                    
                    float cost = EasyManager.PathLength(EasyManager.LookupPath(p, storage.MoveTarget), p) + storage.Cost;
                    if (cost >= bestCost)
                    {
                        continue;
                    }
                    
                    bestCost = cost;
                    bestStorage = storage;
                    bestId = id;
                }

                if (bestStorage == null)
                {
                    continue;
                }

                Log($"Found item type {bestId} at {bestStorage.name}.");
                w.SetId(bestId);
                return bestStorage;
            }

            // Move towards the nearest inbound if there are no needed parts.
            Log("No particular IDs needed.");
            w.SetId(-1);
            Inbound anyInbound = Inbound.Instances.OrderBy(x => x.Empty).ThenBy(x => x.ElapsedTime).ThenBy(x => EasyManager.PathLength(EasyManager.LookupPath(p, x.transform.position), p)).FirstOrDefault();
            Log(anyInbound == null ? "No inbounds to collect from." : $"No order needs anything, closest inbound is {anyInbound.name}.");
            return anyInbound;
        }
    }
}