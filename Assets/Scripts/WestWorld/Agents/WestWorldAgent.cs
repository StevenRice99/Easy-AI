using EasyAI;

namespace WestWorld.Agents
{
    public class WestWorldAgent : TransformAgent
    {
        public enum WestWorldLocation
        {
            Undefined,
            GoldMine,
            Bank,
            Saloon,
            Home
        }

        public WestWorldLocation Location { get; private set; } = WestWorldLocation.Undefined;

        public void ChangeLocation(WestWorldLocation location)
        {
            Location = location;
        }
    }
}