using EasyAI;

namespace Project.Sensors
{
    public class NearestHealthPickupSensor : Sensor
    {
        protected override object Sense()
        {
            return SoldierManager.NearestHealthPickup(Agent);
        }
    }
}