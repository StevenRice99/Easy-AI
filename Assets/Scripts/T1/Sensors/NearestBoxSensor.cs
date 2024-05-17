using System.Linq;
using EasyAI;
using UnityEngine;

namespace T1.Sensors
{
    /// <summary>
    /// Sensor to sense the nearest box to the agent.
    /// </summary>
    [DisallowMultipleComponent]
    public class NearestBoxSensor : EasySensor
    {
        /// <summary>
        /// Sense the nearest box to the agent.
        /// </summary>
        /// <returns>The transform of the nearest box or null if there are no boxes.</returns>
        public override object Sense()
        {
            // Find all boxes in the scene.
            // Constantly finding objects is inefficient, in actual use look for ways to store values.
            Transform[] boxes = FindObjectsByType<Transform>(FindObjectsSortMode.None).Where(t => t.name.Contains("Box")).ToArray();
            
            // Return null if there are no boxes.
            if (boxes.Length == 0)
            {
                Log("No boxes left to collect.");
                return null;
            }

            // Return the nearest box otherwise.
            Log("Getting nearest box.");
            return boxes.OrderBy(b => Vector3.Distance(agent.transform.position, b.transform.position)).First();
        }
    }
}