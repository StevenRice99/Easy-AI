using EasyAI.Utility;
using UnityEngine;

namespace EasyAI
{
    /// <summary>
    /// Base class for implementing a formula to calculate the performance of an agent.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class EasyPerformanceMeasure : EasyComponent
    {
        /// <summary>
        /// Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();

            if (easyAgent != null && easyAgent.performanceMeasure == null)
            {
                easyAgent.performanceMeasure = this;
            }
        }

        /// <summary>
        /// Implement to calculate the performance.
        /// </summary>
        /// <returns>The calculated performance.</returns>
        public abstract float CalculatePerformance();
    }
}