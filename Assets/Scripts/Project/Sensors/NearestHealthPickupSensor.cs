using EasyAI;
using UnityEngine;

namespace Project.Sensors
{
    [DisallowMultipleComponent]
    public class NearestHealthPickupSensor : Sensor
    {
        protected override object Sense()
        {
            return SoldierManager.NearestHealthPickup(Agent);
        }
    }
}