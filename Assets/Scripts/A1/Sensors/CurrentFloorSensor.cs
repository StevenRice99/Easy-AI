using System.Collections.Generic;
using System.Linq;
using EasyAI;
using UnityEngine;

namespace A1.Sensors
{
    /// <summary>
    /// Sense the dirt state of the current tile the agent is on.
    /// </summary>
    [DisallowMultipleComponent]
    public class CurrentFloorSensor : Sensor
    {
        /// <summary>
        /// Sense the dirt state of the current tile the agent is on.
        /// </summary>
        /// <returns>A DirtyData with the dirt state of the current tile the agent is on.</returns>
        public override object Sense()
        {
            // Get all floors. If there is none, return null as there was nothing sensed.
            List<Floor> floors = CleanerManager.Floors;
            if (floors.Count == 0)
            {
                Log("No floors.");
                return null;
            }

            // Create the percepts with the dirt level of the closest floor.
            Floor floor = floors.OrderBy(f => Vector3.Distance(Agent.transform.position, f.transform.position)).First();
            
            Log(floor.IsDirty ? "Current floor tile is dirty." : "Current floor tile is not dirty.");
            return floor;
        }
    }
}