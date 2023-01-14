using EasyAI;

namespace Project
{
    public class SoldierPerformance : PerformanceMeasure
    {
        protected override float CalculatePerformance()
        {
            return Agent is not Soldier {Alive: true} soldier
                ? int.MinValue
                : soldier.Captures * SoldierManager.ScoreCapture + soldier.Returns * SoldierManager.ScoreReturn + (soldier.Kills - soldier.Deaths) * SoldierManager.ScoreKillsDeaths;
        }
    }
}