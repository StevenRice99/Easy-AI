using System.Linq;
using EasyAI;
using UnityEngine;

namespace Warehouse.Sensors
{
    public class PlaceSensor : EasySensor
    {
        public override object Sense()
        {
            if (agent is not WarehouseAgent w)
            {
                return null;
            }

            if (!w.HasPart)
            {
                Log("Has no part so not searching.");
                return null;
            }

            Vector3 p = w.transform.position;
            Outbound outbound = Outbound.Instances.Where(x => x.Requires(w.Id)).OrderBy(x => EasyManager.PathLength(EasyManager.LookupPath(p, new(x.transform.position.x, p.magnitude, x.transform.position.z))) * w.moveSpeed).FirstOrDefault();
            if (outbound != null)
            {
                Log($"{outbound.name} needs {w.Id}.");
                return outbound;
            }

            Storage storage = Storage.Instances.Where(x => x.CanTake(w.Id)).OrderBy(x => EasyManager.PathLength(EasyManager.LookupPath(p, new(x.transform.position.x, p.magnitude, x.transform.position.z))) * w.moveSpeed + x.transform.position.y).FirstOrDefault();
            Log(storage == null ? $"No storage can hold {w.Id}" : $"{storage.name} can hold {w.Id}.");
            return storage;
        }
    }
}