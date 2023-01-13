using System;
using A2.Agents;
using A2.Managers;
using EasyAI.Thinking;
using UnityEngine;

namespace A2.PerformanceMeasures
{
    /// <summary>
    /// Determine what microbe is the best my age and how many offspring it has had.
    /// </summary>
    public class MicrobePerformance : PerformanceMeasure
    {
        /// <summary>
        /// How long in seconds the 
        /// </summary>
        private float _timeAlive;
        
        /// <summary>
        /// Return how long the agent has been alive plus a score for how many offspring it has had.
        /// </summary>
        /// <returns>The score for the microbe.</returns>
        protected override float CalculatePerformance()
        {
            return Agent is not Microbe microbe
                ? 0
                : _timeAlive * MicrobeManager.ScoreSeconds + microbe.Offspring * MicrobeManager.ScoreOffspring;
        }

        private void Update()
        {
            _timeAlive += Time.deltaTime;
        }
    }
}