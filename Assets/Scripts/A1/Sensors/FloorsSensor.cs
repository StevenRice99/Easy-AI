using System.Collections.Generic;
using A1.Managers;
using A1.Percepts;
using EasyAI.Interactions;
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
        /// <returns>A FloorsPercept with positions, dirt levels, and if they are likely to get dirty for all floor tiles in the scene.</returns>
        protected override Percept Sense()
        {
            // Get all floors.
            List<Floor> floors = CleanerAgentManager.CleanerAgentManagerSingleton.Floors;
            
            // Build the percept.
            FloorsPercept percept = new()
            {
                Positions = new Vector3[floors.Count],
                Dirty = new bool[floors.Count],
                LikelyToGetDirty = new bool[floors.Count]
            };
            
            // Fill the percept with data.
            for (int i = 0; i < floors.Count; i++)
            {
                percept.Positions[i] = floors[i].transform.position;
                percept.Dirty[i] = floors[i].State >= Floor.DirtLevel.Dirty;
                percept.LikelyToGetDirty[i] = floors[i].LikelyToGetDirty;
            }
            
            AddMessage($"Perceived {floors.Count} floor tiles.");
            
            return percept;
        }
    }
}