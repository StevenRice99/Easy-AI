using System.Collections.Generic;
using System.Linq;
using A1.Managers;
using EasyAI.Thinking;

namespace A1.PerformanceMeasures
{
    /// <summary>
    /// Calculate how well the cleaner agent is performing based on how clean the floor is.
    /// </summary>
    public class CleanerPerformance : PerformanceMeasure
    {
        /// <summary>
        /// Calculate how well the cleaner agent is performing based on how clean the floor is.
        /// </summary>
        /// <returns>A percentage of how much of the tiles are clean.</returns>
        protected override float CalculatePerformance()
        {
            // Get all floors. If there are none return 100 as there is technically perfect performance if there is nothing to clean.
            List<Floor> floors = CleanerManager.Floors;
            if (floors == null || floors.Count == 0)
            {
                return 100;
            }

            // Otherwise, since there is three dirt levels, determine the max number of potential points.
            int maxPerformance = floors.Count * 3;
            
            // Now, calculate the performance by subtracting every dirty tile from the max performance.
            int performance = maxPerformance;
            performance = floors.Aggregate(performance, (current, floor) => current - (int) floor.State);

            // Convert to a percentage out of 100.
            return (float) performance / maxPerformance * 100f;
        }
    }
}