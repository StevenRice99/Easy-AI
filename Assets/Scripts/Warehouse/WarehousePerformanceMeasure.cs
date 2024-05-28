using EasyAI;
using UnityEngine;

namespace Warehouse
{
    /// <summary>
    /// The score for a warehouse agent.
    /// </summary>
    [DisallowMultipleComponent]
    public class WarehousePerformanceMeasure : EasyPerformanceMeasure
    {
        /// <summary>
        /// Get the score of the warehouse agent.
        /// </summary>
        /// <returns>The score of the warehouse agent.</returns>
        public override float CalculatePerformance()
        {
            return agent is not WarehouseAgent w ? 0 : w.Score;
        }
    }
}