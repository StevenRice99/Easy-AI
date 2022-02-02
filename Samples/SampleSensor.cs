using System.Collections;
using UnityEngine;

namespace Samples
{
    /// <summary>
    /// This sample sensor "detects" a random location to move to every time the agent gets within one unit of it or its been five seconds.
    /// </summary>
    public class SampleSensor : Sensor
    {
        /// <summary>
        /// Where the agent should move to.
        /// </summary>
        public Vector3 target;

        /// <summary>
        /// Since there are several agents in the scene, this just severs as an offset for each agent's own area.
        /// </summary>
        public Vector3 origin;

        /// <summary>
        /// How far from the center each random position can be generated.
        /// </summary>
        public float size;

        /// <summary>
        /// When it has been move than five seconds this sets to true to automatically choose a new position.
        /// </summary>
        private bool change;
        
        /// <summary>
        /// Create a SamplePercept with the location to return to..
        /// </summary>
        /// <returns>The percept sent back to the agent.</returns>
        protected override Percept Sense()
        {
            // Generate a new random position to move to.
            if (change || Vector2.Distance(new Vector2(Position.x, Position.z), new Vector2(target.x, target.z)) <= 1)
            {
                target = new Vector3(Random.Range(origin.x - size, origin.x + size), 0, Random.Range(origin.z - size, origin.z + size));
                change = false;
                StopAllCoroutines();
                StartCoroutine(WaitForSeconds());
            }

            // Return that position wrapped in a percept.
            return new SamplePercept { Position = target };
        }

        /// <summary>
        /// Simple delay to choose a new position after five seconds if it still has not been reached.
        /// </summary>
        /// <returns>Nothing.</returns>
        private IEnumerator WaitForSeconds()
        {
            yield return new WaitForSeconds(5);
            change = true;
        }
    }
}