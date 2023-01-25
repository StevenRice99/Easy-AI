using EasyAI;
using WestWorld.Agents;

namespace WestWorld.States
{
    public class VisitBathroom : State
    {
        public override void Enter(Agent agent)
        {
            agent.Log("Elsa: Walkin' to the can. Need to powda mah pretty li'l nose");
        }

        public override void Execute(Agent agent)
        {
            HouseKeeper houseKeeper = agent as HouseKeeper;
            houseKeeper.Log("Elsa: Ahhhhhh! Sweet relief!");
            houseKeeper.ReturnToLastState();
        }

        public override void Exit(Agent agent)
        {
            agent.Log("Leavin' the john.");
        }
    }
}