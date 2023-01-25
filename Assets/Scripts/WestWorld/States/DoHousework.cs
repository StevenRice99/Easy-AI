using EasyAI;

namespace WestWorld.States
{
    public class DoHousework : State
    {
        public override void Execute(Agent agent)
        {
            switch (new System.Random().Next(3))
            {
                case 2:
                    agent.Log("Makin' the bed.");
                    break;
                case 1:
                    agent.Log("Moppin' the floor.");
                    break;
            }
        }
    }
}