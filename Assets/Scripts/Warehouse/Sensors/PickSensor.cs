using System.Collections.Generic;
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

            // No need to find something to pick up if already holding something.
            if (w.HasPart)
            {
                Log("Has a part so not searching.");
                return null;
            }

            // Cache the agent's position.
            Vector3 p = w.transform.position;

            // Get all active outbound locations.
            Outbound[] outbounds = Outbound.Instances.Where(x => x.Active).ToArray();

            // Cache all IDs and positions of outbounds.
            int[][] ids = new int[outbounds.Length][];
            Vector3[] d = new Vector3[outbounds.Length];
            for (int i = 0; i < outbounds.Length; i++)
            {
                ids[i] = outbounds[i].AllAvailable();
                d[i] = outbounds[i].transform.position;
            }
            
            // Check to see if any inbounds have a needed part when not using roles.
            Inbound bestInbound = null;
            int bestInboundId = -1;
            float bestInboundCost = float.MaxValue;
            if (!WarehouseManager.Roles)
            {
                // Check every outbound.
                for (int i = 0; i < outbounds.Length; i++)
                {
                    // Check every ID this outbound has.
                    foreach (int id in ids[i])
                    {
                        // See if there is an inbound which has this ID.
                        Inbound inbound = Inbound.Instances.Where(x => x.PickAvailable(w, id)).OrderBy(x => x.PickTime(p, d[i], w.moveSpeed)).FirstOrDefault();
                        if (inbound == null)
                        {
                            continue;
                        }

                        // Check the cost of this inbound.
                        float cost = inbound.PickTime(p, d[i], w.moveSpeed);
                        if (cost >= bestInboundCost)
                        {
                            continue;
                        }

                        // Save this inbound if it is the best option.
                        bestInboundCost = cost;
                        bestInbound = inbound;
                        bestInboundId = id;
                    }
                }
            }
            
            // Check to see if any storages have a needed part, either if no roles or a picker.
            Storage bestStorage = null;
            int bestStorageId = -1;
            float bestStorageCost = float.MaxValue;
            if (!WarehouseManager.Roles || !w.Inbound)
            {
                // Check every outbound.
                for (int i = 0; i < outbounds.Length; i++)
                {
                    // Check every ID this outbound has.
                    foreach (int id in ids[i])
                    {
                        // If there are no storages which have this ID, don't bother searching.
                        if (!Storage.PickOptions.TryGetValue(id, out HashSet<Storage> option))
                        {
                            continue;
                        }
                    
                        // See if any of the storages are available.
                        Storage storage = option.Where(x => x.PickAvailable(w, id)).OrderBy(x => x.PickTime(p, d[i], w.moveSpeed)).ThenBy(x => x.transform.position.y).FirstOrDefault();
                        if (storage == null)
                        {
                            continue;
                        }

                        // Check the cost of this storage.
                        float cost = storage.PickTime(p, d[i], w.moveSpeed);
                        if (cost >= bestStorageCost)
                        {
                            continue;
                        }
                    
                        // Save this storage if it is the best option.
                        bestStorageCost = cost;
                        bestStorage = storage;
                        bestStorageId = id;
                    }
                }
            }

            // If no storage...
            if (bestStorage == null)
            {
                // And an inbound...
                if (bestInbound != null)
                {
                    // Target the inbound.
                    Log($"Found item type {bestInboundId} at {bestInbound.name}.");
                    w.SetId(bestInboundId);
                    return bestInbound;
                }
            }
            // Otherwise, there is a storage so...
            else
            {
                // If there is no inbound...
                if (bestInbound == null)
                {
                    // Target the storage.
                    Log($"Found item type {bestStorageId} at {bestStorage.name}.");
                    w.SetId(bestStorageId);
                    return bestStorage;
                }

                // Otherwise we have both so check the costs and move towards the best.
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

            // If there is nothing needed and this is an order packer, do nothing.
            if (WarehouseManager.Roles && !w.Inbound)
            {
                w.SetId(-1);
                return null;
            }

            // If there is nothing needed and this is a shipment worker or there are no roles...
            foreach (Inbound inbound in Inbound.Instances.Where(x => !x.Empty).OrderBy(x => x.PickTime(p, x.transform.position, w.moveSpeed)))
            {
                // Try and get the most commonly demanded item from an inbound.
                int id = inbound.GetNext();
                if (id < 0)
                {
                    continue;
                }

                // Get this most commonly demanded item.
                Log($"No order, will unload {inbound.name}.");
                w.SetId(id);
                return inbound;
            }

            // Otherwise, there are no items at inbounds so nothing to do.
            Log("No inbounds to collect from.");
            w.SetId(-1);
            return null;
        }
    }
}