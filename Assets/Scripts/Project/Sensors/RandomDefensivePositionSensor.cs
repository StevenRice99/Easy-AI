using EasyAI;
using UnityEngine;

namespace Project.Sensors
{
    [DisallowMultipleComponent]
    public class RandomDefensivePositionSensor : Sensor
    {
        public override object Sense()
        {
            return Agent is not Soldier soldier ? null : SoldierManager.RandomStrategicPosition(soldier, true);
        }
    }
}