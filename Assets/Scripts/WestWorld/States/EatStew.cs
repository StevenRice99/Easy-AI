using EasyAI;

namespace WestWorld.States
{
    public class EatStew : State
    {
        public override void Enter(Agent agent)
        {
            agent.Log("Okay hun, ahm a-comin'!");
            agent.Log("Smells reaaal goood, Elsa!");
        }

        public override void Execute(Agent agent)
        {
            agent.Log("Tastes real good too!");
            agent.SetState<GoHomeAndSleepTillRested>();
        }

        public override void Exit(Agent agent)
        {
            agent.Log("Thank ya li'l lady. Ah better get back to whatever ah wuz doin'.");
        }
    }
}