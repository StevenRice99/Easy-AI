using System.Collections.Generic;
using System.Linq;
using A1.Managers;
using A1.Percepts;
using EasyAI.Interactions;
using UnityEngine;

namespace A1.Sensors
{
    /// <summary>
    /// Sense the dirt state of the current tile the agent is on.
    /// </summary>
    public class DirtySensor : Sensor
    {
        /// <summary>
        /// Sense the dirt state of the current tile the agent is on.
        /// </summary>
        /// <returns>A DirtyPercept with the dirt state of the current tile the agent is on.</returns>
        protected override Percept Sense()
        {
            // Get all floors. If there is none, return null as there was nothing sensed.
            List<Floor> floors = CleanerAgentManager.CleanerAgentManagerSingleton.Floors;
            if (floors.Count == 0)
            {
                AddMessage("No floors.");
                return null;
            }

            // Create the percept with the dirt level of the closest floor.
            DirtyPercept percept = new()
            {
                Floor = floors.OrderBy(f => Vector3.Distance(Agent.transform.position, f.transform.position)).First()
            };
            
            AddMessage(percept.IsDirty ? "Current floor tile is dirty." : "Current floor tile is not dirty.");
            return percept;
        }
    }
}