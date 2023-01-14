using EasyAI;
using UnityEngine;

namespace Project.Sensors
{
    [DisallowMultipleComponent]
    public class NearestHealthPickupSensor : Sensor
    {
        public override object Sense()
        {
            return SoldierManager.NearestHealthPickup(Agent);
        }
    }
}