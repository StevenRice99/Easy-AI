using EasyAI;

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
            agent.Log("Elsa: Ahhhhhh! Sweet relief!");
            agent.SetState<DoHousework>();
        }

        public override void Exit(Agent agent)
        {
            agent.Log("Leavin' the john.");
        }
    }
}