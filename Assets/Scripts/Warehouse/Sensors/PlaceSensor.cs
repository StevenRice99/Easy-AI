using System.Linq;
using EasyAI;
using UnityEngine;

namespace Warehouse.Sensors
{
    /// <summary>
    /// Sensor to find where to place a part down at.
    /// </summary>
    [DisallowMultipleComponent]
    public class PlaceSensor : EasySensor
    {
        /// <summary>
        /// Find where to place down a part to.
        /// </summary>
        /// <returns>Where to place down a part to or null if there is no place to do so.</returns>
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

            // Look for outbound locations that need this first.
            Outbound outbound = Outbound.Instances.Where(x => x.Requires(w.Id)).OrderBy(x => x.PlaceTime(p, w.moveSpeed)).FirstOrDefault();
            if (outbound != null)
            {
                Log($"{outbound.name} needs {w.Id}.");
                return outbound;
            }

            // If no outbound locations need it, store it.
            Storage storage = Storage.Instances.Where(x => x.Available(w) && x.CanTake(w.Id)).OrderBy(x => x.PlaceTime(p, w.moveSpeed)).FirstOrDefault();
            Log(storage == null ? $"No storage can hold {w.Id}." : $"{storage.name} can hold {w.Id}.");
            return storage;
        }
    }
}