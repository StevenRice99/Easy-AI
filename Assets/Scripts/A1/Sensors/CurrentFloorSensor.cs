using System.Collections.Generic;
using System.Linq;
using A1.Managers;
using EasyAI.Sensors;
using UnityEngine;

namespace A1.Sensors
{
    /// <summary>
    /// Sense the dirt state of the current tile the agent is on.
    /// </summary>
    public class CurrentFloorSensor : Sensor
    {
        /// <summary>
        /// Sense the dirt state of the current tile the agent is on.
        /// </summary>
        /// <returns>A DirtyData with the dirt state of the current tile the agent is on.</returns>
        protected override object Sense()
        {
            // Get all floors. If there is none, return null as there was nothing sensed.
            List<Floor> floors = CleanerManager.Floors;
            if (floors.Count == 0)
            {
                AddMessage("No floors.");
                return null;
            }

            // Create the percepts with the dirt level of the closest floor.
            Floor floor = floors.OrderBy(f => Vector3.Distance(Agent.transform.position, f.transform.position)).First();
            
            AddMessage(floor.IsDirty ? "Current floor tile is dirty." : "Current floor tile is not dirty.");
            return floor;
        }
    }
}