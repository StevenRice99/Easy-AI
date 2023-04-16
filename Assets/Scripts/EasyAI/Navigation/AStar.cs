using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyAI.Navigation
{
    /// <summary>
    /// A* pathfinding.
    /// </summary>
    public static class AStar
    {
        /// <summary>
        /// Perform A* pathfinding.
        /// </summary>
        /// <param name="nodes">The list of open and closed nodes, seeded with the starting node.</param>
        /// <param name="goal">The end goal position.</param>
        /// <param name="paths">All paths the algorithm can take.</param>
        /// <returns>The path of nodes to take to get from the starting position to the ending position.</returns>
        public static AStarNode Perform(List<AStarNode> nodes, Vector3 goal, AStarPaths paths)
        {
            // No best node by default.
            AStarNode best = null;

            // Loop until there are no options left meaning the path cannot be completed.
            while (nodes.Any(n => n.IsOpen))
            {
                // Get the open node with the lowest F cost and then by the lowest H cost if there is a tie in F cost.
                AStarNode node = nodes.Where(n => n.IsOpen).OrderBy(n => n.CostF).ThenBy(n => n.CostH).First();
            
                // Close the current node.
                node.Close();
            
                // Update this to the best node if it is.
                if (best == null || node.CostF < best.CostF)
                {
                    best = node;
                }
            
                // Loop through all nodes which connect to the current node.
                foreach (Vector3 position in paths.Successors(node))
                {
                    // Create the A* node.
                    AStarNode successor = new(position, goal, node);

                    // If this node is the goal destination, A* is done so set it as the best and clear the node list so the loop ends.
                    if (position == goal)
                    {
                        return successor;
                    }

                    // If the node is not yet in the list, add it.
                    AStarNode existing = nodes.FirstOrDefault(n => n.Position == position);
                    if (existing == null)
                    {
                        nodes.Add(successor);
                        continue;
                    }

                    // If it did already exist in the list but this path takes longer do nothing. 
                    if (existing.CostF <= successor.CostF)
                    {
                        continue;
                    }

                    // If the new path is shorter, update its previous node and open it again.
                    existing.UpdatePrevious(node);
                    existing.Open();
                }
            }

            return best;
        }
    }
}