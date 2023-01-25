using EasyAI;

namespace WestWorld.States
{
    public class DoHousework : State
    {
        public override void Enter(Agent agent)
        {
            agent.Log("Time to do some more housework!");
        }

        public override void Execute(Agent agent)
        {
            switch (new System.Random().Next(4))
            {
                case 3:
                    agent.Log("Washin' the dishes.");
                    break;
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