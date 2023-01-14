using System.Collections.Generic;
using EasyAI;
using UnityEngine;

namespace A1.Sensors
{
    /// <summary>
    /// Sense positions, dirt levels, and if they are likely to get dirty for all floor tiles in the scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class AllFloorsSensor : Sensor
    {
        /// <summary>
        /// Sense positions, dirt levels, and if they are likely to get dirty for all floor tiles in the scene.
        /// </summary>
        /// <returns>A FloorsData with positions, dirt levels, and if they are likely to get dirty for all floor tiles in the scene.</returns>
        public override object Sense()
        {
            // Get all floors.
            List<Floor> floors = CleanerManager.Floors;
            
            // Build the percepts.
            FloorsData data = new(new Vector3[floors.Count], new bool[floors.Count], new bool[floors.Count]);

            // Fill the percepts with data.
            for (int i = 0; i < floors.Count; i++)
            {
                data.Positions[i] = floors[i].transform.position;
                data.Dirty[i] = floors[i].State >= Floor.DirtLevel.Dirty;
                data.LikelyToGetDirty[i] = floors[i].LikelyToGetDirty;
            }
            
            Log($"Perceived {floors.Count} floor tiles.");
            
            return data;
        }
    }
}