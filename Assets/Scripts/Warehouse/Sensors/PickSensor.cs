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
            
            // Check to see if any inbounds have a needed part.
            Inbound bestInbound = null;
            int bestInboundId = -1;
            float bestInboundCost = float.MaxValue;
            foreach (Outbound outbound in outbounds)
            {
                int[] ids = outbound.Requirements();
                Vector3 d = outbound.transform.position;
                foreach (int id in ids)
                {
                    Inbound inbound = Inbound.Instances.Where(x => x.Has(id)).OrderBy(x => x.PickTime(p, d, w.moveSpeed)).FirstOrDefault();
                    if (inbound == null)
                    {
                        continue;
                    }

                    float cost = inbound.PickTime(p, d, w.moveSpeed);
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
                Vector3 d = outbound.transform.position;
                foreach (int id in ids)
                {
                    Storage storage = Storage.Instances.Where(x => x.Available(w) && x.Has(id)).OrderBy(x => x.PickTime(p, d, w.moveSpeed)).FirstOrDefault();
                    if (storage == null)
                    {
                        continue;
                    }

                    float cost = storage.PickTime(p, d, w.moveSpeed);
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

            Inbound[] inbounds = Inbound.Instances.Where(x => !x.Empty).ToArray();
            if (inbounds.Length > 0)
            {
                HashSet<int> options = new();
                int[][] ids = new int[inbounds.Length][];
                for (int i = 0; i < inbounds.Length; i++)
                {
                    ids[i] = inbounds[i].Ids;
                    for (int j = 0; j < ids[i].Length; j++)
                    {
                        options.Add(ids[i][j]);
                    }
                }

                Dictionary<int, Storage[]> storages = options.ToDictionary(id => id, id => Storage.Instances.Where(x => x.CanTake(id)).ToArray());

                bestInbound = null;
                bestInboundCost = float.MaxValue;

                for (int i = 0; i < inbounds.Length; i++)
                {
                    for (int j = 0; j < ids[i].Length; j++)
                    {
                        Storage[] idStorages = storages[ids[i][j]];
                        for (int k = 0; k < idStorages.Length; k++)
                        {
                            float cost = inbounds[i].PickTime(p, idStorages[k].transform.position, w.moveSpeed);
                            if (cost >= bestInboundCost)
                            {
                                continue;
                            }

                            bestInbound = inbounds[i];
                            bestInboundCost = cost;
                        }
                    }
                }

                if (bestInbound != null)
                {
                    Log($"No order, going to {bestInbound.name}.");
                    return bestInbound;
                }
            }

            // Move towards the nearest inbound if there are no needed parts.
            Log("No particular IDs needed.");
            return null;
        }
    }
}