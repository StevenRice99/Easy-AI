using EasyAI;
using WestWorld.Agents;

namespace WestWorld.States
{
    public class CookStew : State
    {
        public override void Enter(Agent agent)
        {
            agent.Log("Puttin' the stew in the oven.");
        }

        public override void Execute(Agent agent)
        {
            agent.Log("Fussin' over food.");
            
            if (new System.Random().Next(5) == 0)
            {
                agent.SetState<DoHousework>();
            }
        }

        public override void Exit(Agent agent)
        {
            HouseKeeper houseKeeper = agent as HouseKeeper;
            houseKeeper.Log("Stew ready! Let's eat.");
            houseKeeper.SendMessage(WestWorldAgent.WestWorldMessage.StewReady);
            houseKeeper.Log("Puttin' the stew on the table.");
        }
    }
}