using System.Collections.Generic;
using System.Linq;
using A1.AgentActions;
using A1.Percepts;
using A1.Sensors;
using EasyAI.Agents;
using EasyAI.Thinking;
using UnityEngine;

namespace A1.States
{
    [CreateAssetMenu(menuName = "A1/States/Cleaner Mind", fileName = "Cleaner Mind")]
    public class CleanerMind : State
    {
        /// <summary>
        /// Called when an agent is in this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override void Execute(Agent agent)
        {
            // If currently cleaning, no need to make a new decision.
            if (agent.HasAction<CleanAction>())
            {
                return;
            }
            
            // Determine if the current floor tile needs to be cleaned.
            Floor floorToClean = CanClean(agent);
            
            // If there are no floor tiles to clean, determine where to move which will be the closest floor with the highest dirt level or the weighted midpoint.
            if (floorToClean == null)
            {
                agent.AddMessage("Nothing to clean, preparing for more dirt.");
                agent.Move(DetermineLocationToMove(agent));
                return;
            }

            // Otherwise we are on a dirty floor to so stop movement and start cleaning the current floor tile.
            agent.AddMessage("Cleaning current floor tile.");
            agent.StopMoving();
            agent.Act(new CleanAction(floorToClean));
        }

        /// <summary>
        /// Determine if the current floor tile needs to be cleaned or not.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <returns>The current floor if it was detected as needing to be cleaned, null otherwise.</returns>
        private static Floor CanClean(Agent agent)
        {
            // Read the dirty sensor to get the current tile.
            DirtyData dirtyData = agent.Sense<DirtySensor, DirtyData>();

            // Return the tile to clean if it needs to, otherwise null.
            return dirtyData is { IsDirty: true } ? dirtyData.Floor : null;
        }

        /// <summary>
        /// Determine where the cleaner agent should move to.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <returns>The position of the closest dirtiest floor tile or the weighted midpoint if all floor tiles are clean.</returns>
        private static Vector3 DetermineLocationToMove(Agent agent)
        {
            // Read the dirty sensor to get the current tile.
            FloorsData floorData = agent.Sense<FloorsSensor, FloorsData>();
            if (floorData == null)
            {
                return Vector3.zero;
            }
            
            List<Vector3> all = new();
            List<Vector3> dirty = new();
            List<Vector3> likelyToGetDirty = new();

            // Add the dirty status of all tiles to their respective arrays.
            for (int i = 0; i < floorData.Positions.Length; i++)
            {
                all.Add(floorData.Positions[i]);
                
                if (floorData.LikelyToGetDirty[i])
                {
                    likelyToGetDirty.Add(floorData.Positions[i]);
                }

                if (floorData.Dirty[i])
                {
                    dirty.Add(floorData.Positions[i]);
                }
            }

            Vector3 position = agent.transform.position;

            // If there are dirty floor tiles, return the position of the closest one.
            return dirty.Count > 0
                ? NearestPosition(position, dirty)
                // Else if there are tiles more likely to get dirty than others, return the weighted midpoint.
                : likelyToGetDirty.Count > 0
                    ? CalculateMidPoint(all, likelyToGetDirty)
                    // Otherwise there are no tiles more likely to get dirty than others so simply return (0, 0, 0).
                    : Vector3.zero;
        }

        /// <summary>
        /// Find the closest floor tile position to the cleaner agent.
        /// </summary>
        /// <param name="position">The position of the agent.</param>
        /// <param name="positions">The floor tile positions to search through.</param>
        /// <returns>The closest floor tile position to the cleaner agent.</returns>
        private static Vector3 NearestPosition(Vector3 position, IReadOnlyCollection<Vector3> positions)
        {
            return positions.Count == 0 ? Vector3.zero : positions.OrderBy(p => Vector3.Distance(position, p)).First();
        }

        /// <summary>
        /// Calculated the weighted midpoint which sums all floor tile positions once and then a second time for all likely to get dirty floor tiles again.
        /// </summary>
        /// <param name="all">All floor tiles.</param>
        /// <param name="likelyToGetDirty">Likely to get dirty floor tiles.</param>
        /// <returns>The weighted midpoint which sums all floor tile positions once and then a second time for all likely to get dirty floor tiles again.</returns>
        private static Vector3 CalculateMidPoint(IReadOnlyCollection<Vector3> all, IEnumerable<Vector3> likelyToGetDirty)
        {
            return (all.Aggregate(Vector3.zero, (current, p) => current + p) + likelyToGetDirty.Aggregate(Vector3.zero, (current, p) => current + p)) / all.Count;
        }
    }
}