using EasyAI;

namespace Project.Sensors
{
    public class RandomStrategicPositionSensor : Sensor
    {
        public override object Sense()
        {
            return Agent is not Soldier soldier
                ? null
                : SoldierManager.RandomStrategicPosition(soldier, soldier.Role == Soldier.SoliderRole.Defender);
        }
    }
}