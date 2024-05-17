using System.Linq;
using EasyAI;
using UnityEngine;

namespace T1
{
    /// <summary>
    /// Calculate how well the box collector is doing based on how many boxes it has collected.
    /// </summary>
    [DisallowMultipleComponent]
    public class BoxCollectorPerformance : EasyPerformanceMeasure
    {
        /// <summary>
        /// The total boxes at the start of the scene.
        /// </summary>
        private int _totalBoxes;
        
        /// <summary>
        /// The total number of boxes left in the scene.
        /// </summary>
        private static int BoxCount => FindObjectsByType<Transform>(FindObjectsSortMode.None).Count(t => t.name.Contains("Box"));
        
        /// <summary>
        /// Calculate the performance as a percentage of the number of boxes which have been collected.
        /// </summary>
        /// <returns>The percentage of boxes which have been collected.</returns>
        public override float CalculatePerformance() => (float) (_totalBoxes - BoxCount) / _totalBoxes * 100;

        /// <summary>
        /// Start is called on the frame when a script is enabled just before any of the Update methods are called the first time.
        /// </summary>
        protected void Start()
        {
            _totalBoxes = BoxCount;
        }
    }
}