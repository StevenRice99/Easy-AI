using System.Linq;
using EasyAI;
using UnityEngine;

namespace Warehouse.Sensors
{
    public class PickSensor : EasySensor
    {
        public override object Sense()
        {
            if (agent is not WarehouseAgent { HasPart: false } w)
            {
                return null;
            }

            Vector3 p = w.transform.position;
            Outbound[] outbounds = Outbound.Instances.Where(x => x.Active).OrderBy(x => EasyManager.PathLength(EasyManager.LookupPath(p, new(x.transform.position.x, p.magnitude, x.transform.position.z))) * w.moveSpeed).ToArray();
            foreach (Outbound outbound in outbounds)
            {
                float bestCost = float.MaxValue;
                Inbound bestInbound = null;
                int bestId = -1;

                int[] ids = outbound.Requirements();
                foreach (int id in ids)
                {
                    Inbound inbound = Inbound.Instances.Where(x => x.Has(id)).OrderBy(x => EasyManager.PathLength(EasyManager.LookupPath(p, new(x.transform.position.x, p.magnitude, x.transform.position.z))) * w.moveSpeed).FirstOrDefault();
                    if (inbound == null)
                    {
                        continue;
                    }

                    float cost = EasyManager.PathLength(EasyManager.LookupPath(p, new(inbound.transform.position.x, p.magnitude, inbound.transform.position.z))) * w.moveSpeed;
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

            foreach (Outbound outbound in outbounds)
            {
                float bestCost = float.MaxValue;
                Storage bestStorage = null;
                int bestId = -1;

                int[] ids = outbound.Requirements();
                foreach (int id in ids)
                {
                    Storage storage = Storage.Instances.Where(x => x.Has(id)).OrderBy(x => EasyManager.PathLength(EasyManager.LookupPath(p, new(x.transform.position.x, p.magnitude, x.transform.position.z))) * w.moveSpeed + x.Delay).FirstOrDefault();
                    if (storage == null)
                    {
                        continue;
                    }
                    
                    float cost = EasyManager.PathLength(EasyManager.LookupPath(p, new(storage.transform.position.x, p.magnitude, storage.transform.position.z))) * w.moveSpeed + storage.Delay;
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

            Log("No particular IDs needed.");
            w.SetId(-1);
            Inbound anyInbound = Inbound.Instances.OrderBy(x => EasyManager.PathLength(EasyManager.LookupPath(p, new(x.transform.position.x, p.magnitude, x.transform.position.z))) * w.moveSpeed).FirstOrDefault();
            Log(anyInbound == null ? "No inbounds to collect from." : $"No order needs anything, closest inbound is {anyInbound.name}.");
            return anyInbound;
        }
    }
}