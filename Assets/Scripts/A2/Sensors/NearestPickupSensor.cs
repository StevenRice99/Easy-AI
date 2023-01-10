using System.Linq;
using A2.Pickups;
using EasyAI.Sensors;
using UnityEngine;

namespace A2.Sensors
{
    public class NearestPickupSensor : Sensor
    {
        protected override object Sense()
        {
            MicrobeBasePickup[] pickups = FindObjectsOfType<MicrobeBasePickup>();
            return pickups.Length == 0 ? null : pickups.OrderBy(p => Vector3.Distance(Agent.transform.position, p.transform.position)).FirstOrDefault();
        }
    }
}