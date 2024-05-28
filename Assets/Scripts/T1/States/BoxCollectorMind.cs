using EasyAI;
using T1.Sensors;
using UnityEngine;

namespace T1.States
{
    /// <summary>
    /// The global state which the box collector is always in.
    /// </summary>
    [CreateAssetMenu(menuName = "T1/States/Box Collector Mind", fileName = "Box Collector Mind")]
    public class BoxCollectorMind : EasyState
    {
        /// <summary>
        /// Called when an agent is in this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override void Execute(EasyAgent agent)
        {
            // Sense the nearest box.
            Transform box = agent.Sense<NearestBoxSensor, Transform>();
            
            // If there are no boxes left, do nothing.
            if (box == null)
            {
                agent.Log("Collected all boxes.");
                return;
            }
            
            // Move towards the box and try to pick it up.
            agent.Log($"Collecting {box.name} next.");
            agent.Move(box.position);
            agent.Act(box);
        }
    }
}