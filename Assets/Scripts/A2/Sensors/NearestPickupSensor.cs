using System.Linq;
using A2.Pickups;
using EasyAI.Sensors;
using UnityEngine;

namespace A2.Sensors
{
    /// <summary>
    /// Sensor to find the nearest pickup for a microbe.
    /// </summary>
    public class NearestPickupSensor : Sensor
    {
        /// <summary>
        /// Sense the nearest pickup of the microbe.
        /// </summary>
        /// <returns>The nearest pickup of the microbe or null if none is found.</returns>
        protected override object Sense()
        {
            MicrobeBasePickup[] pickups = FindObjectsOfType<MicrobeBasePickup>();
            return pickups.Length == 0 ? null : pickups.OrderBy(p => Vector3.Distance(Agent.transform.position, p.transform.position)).FirstOrDefault();
        }
    }
}