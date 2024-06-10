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

            if (w.HasPart)
            {
                Log("Has a part so not searching.");
                return null;
            }

            Vector3 p = w.transform.position;

            // Get all active outbound locations by distance.
            Outbound[] outbounds = Outbound.Instances.Where(x => x.Active).ToArray();

            int[][] ids = new int[outbounds.Length][];
            Vector3[] d = new Vector3[outbounds.Length];
            for (int i = 0; i < outbounds.Length; i++)
            {
                ids[i] = outbounds[i].Requirements();
                d[i] = outbounds[i].transform.position;
            }
            
            // Check to see if any inbounds have a needed part.
            Inbound bestInbound = null;
            int bestInboundId = -1;
            float bestInboundCost = float.MaxValue;
            if (!WarehouseManager.UseRoles)
            {
                for (int i = 0; i < outbounds.Length; i++)
                {
                    foreach (int id in ids[i])
                    {
                        Inbound inbound = Inbound.Instances.Where(x => x.Has(id)).OrderBy(x => x.PickTime(p, d[i], w.moveSpeed)).FirstOrDefault();
                        if (inbound == null)
                        {
                            continue;
                        }

                        float cost = inbound.PickTime(p, d[i], w.moveSpeed);
                        if (cost >= bestInboundCost)
                        {
                            continue;
                        }

                        bestInboundCost = cost;
                        bestInbound = inbound;
                        bestInboundId = id;
                    }
                }
            }
            
            // Check to see if any storages have a needed part.
            Storage bestStorage = null;
            int bestStorageId = -1;
            float bestStorageCost = float.MaxValue;
            if (!WarehouseManager.UseRoles || !w.Inbound)
            {
                for (int i = 0; i < outbounds.Length; i++)
                {
                    foreach (int id in ids[i])
                    {
                        if (!Storage.PickOptions.TryGetValue(id, out HashSet<Storage> option))
                        {
                            continue;
                        }
                    
                        Storage storage = option.Where(x => x.Available(w) && x.Has(id)).OrderBy(x => x.PickTime(p, d[i], w.moveSpeed)).FirstOrDefault();
                        if (storage == null)
                        {
                            continue;
                        }

                        float cost = storage.PickTime(p, d[i], w.moveSpeed);
                        if (cost >= bestStorageCost)
                        {
                            continue;
                        }
                    
                        bestStorageCost = cost;
                        bestStorage = storage;
                        bestStorageId = id;
                    }
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
            w.SetId(-1);
            if (WarehouseManager.UseRoles && !w.Inbound)
            {
                return null;
            }

            Inbound anyInbound = Inbound.Instances.Where(x => !x.Empty).OrderBy(x => x.PickTime(p, x.transform.position, w.moveSpeed)).FirstOrDefault();
            Log(anyInbound == null ? "No inbounds to collect from." : $"No order, will unload {anyInbound.name}.");
            return anyInbound;
        }
    }
}