using A2.Agents;
using A2.Managers;
using Sensor = EasyAI.Sensors.Sensor;

namespace A2.Actuators
{
    public class NearestPreySensor : Sensor
    {
        protected override object Sense()
        {
            return MicrobeManager.FindFood(Agent as Microbe);
        }
    }
}