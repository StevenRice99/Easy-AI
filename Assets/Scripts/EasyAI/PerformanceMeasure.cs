using EasyAI.Utility;

namespace EasyAI
{
    /// <summary>
    /// Base class for implementing a formula to calculate the performance of an agent.
    /// </summary>
    public abstract class PerformanceMeasure : IntelligenceComponent
    {
        /// <summary>
        /// The last performance value.
        /// </summary>
        private float _performance;
        
        /// <summary>
        /// Calculate and return the performance value.
        /// </summary>
        /// <returns>Return the performance to the agent.</returns>
        public float GetPerformance()
        {
            if (Agent != null)
            {
                _performance = CalculatePerformance();
            }

            return _performance;
        }
        
        /// <summary>
        /// Implement to calculate the performance.
        /// </summary>
        /// <returns>The calculated performance.</returns>
        protected abstract float CalculatePerformance();

        protected override void Start()
        {
            base.Start();
            CalculatePerformance();
        }
    }
}