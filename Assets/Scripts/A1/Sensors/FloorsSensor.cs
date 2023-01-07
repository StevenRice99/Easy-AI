using System.Collections.Generic;
using A1.Managers;
using A1.Percepts;
using EasyAI.Percepts;
using EasyAI.Sensors;
using UnityEngine;

namespace A1.Sensors
{
    /// <summary>
    /// Sense positions, dirt levels, and if they are likely to get dirty for all floor tiles in the scene.
    /// </summary>
    public class FloorsSensor : Sensor
    {
        /// <summary>
        /// Sense positions, dirt levels, and if they are likely to get dirty for all floor tiles in the scene.
        /// </summary>
        /// <returns>A FloorsData with positions, dirt levels, and if they are likely to get dirty for all floor tiles in the scene.</returns>
        protected override PerceivedData Sense()
        {
            // Get all floors.
            List<Floor> floors = CleanerAgentManager.CleanerAgentManagerSingleton.Floors;
            
            // Build the percepts.
            FloorsData data = new()
            {
                Positions = new Vector3[floors.Count],
                Dirty = new bool[floors.Count],
                LikelyToGetDirty = new bool[floors.Count]
            };
            
            // Fill the percepts with data.
            for (int i = 0; i < floors.Count; i++)
            {
                data.Positions[i] = floors[i].transform.position;
                data.Dirty[i] = floors[i].State >= Floor.DirtLevel.Dirty;
                data.LikelyToGetDirty[i] = floors[i].LikelyToGetDirty;
            }
            
            AddMessage($"Perceived {floors.Count} floor tiles.");
            
            return data;
        }
    }
}