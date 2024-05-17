using EasyAI;
using UnityEngine;

namespace Project.Sensors
{
    /// <summary>
    /// Sensor to sense a random offensive position.
    /// </summary>
    [DisallowMultipleComponent]
    public class RandomOffensivePositionSensor : EasySensor
    {
        /// <summary>
        /// Sense a random offensive position.
        /// </summary>
        /// <returns>A random offensive position.</returns>
        public override object Sense()
        {
            return agent is not Soldier soldier ? null : SoldierManager.RandomStrategicPosition(soldier, false);
        }
    }
}