using System.Collections.Generic;
using System.Linq;
using EasyAI.Navigation.Utility;
using UnityEngine;

namespace EasyAI.Navigation
{
    /// <summary>
    /// A* pathfinding.
    /// </summary>
    public static class EasyAStar
    {
        /// <summary>
        /// Perform A* pathfinding.
        /// </summary>
        /// <param name="nodes">The list of open and closed nodes, seeded with the starting node.</param>
        /// <param name="goal">The end goal position.</param>
        /// <param name="paths">All paths the algorithm can take.</param>
        /// <returns>The destination node or the closest node if A* could not find a complete path.</returns>
        public static EasyAStarNode Perform(List<EasyAStarNode> nodes, Vector3 goal, EasyAStarPaths paths)
        {
            // No best node by default.
            EasyAStarNode best = null;

            // Loop until there are no options left meaning the path cannot be completed.
            while (nodes.Any(n => n.IsOpen))
            {
                // Get the open node with the lowest F cost and then by the lowest H cost if there is a tie in F cost.
                EasyAStarNode node = nodes.Where(n => n.IsOpen).OrderBy(n => n.CostF).ThenBy(n => n.CostH).First();
            
                // Close the current node.
                node.Close();
            
                // Update this to the best node if it is.
                if (best == null || node.CostF < best.CostF)
                {
                    best = node;
                }
            
                // Loop through all nodes which connect to the current node.
                foreach (EasyAStarNode successor in paths.Successors(node, goal))
                {
                    // If this node is the goal destination, A* is done.
                    if (successor.Reached)
                    {
                        return successor;
                    }

                    // If the node is not yet in the list, add it.
                    EasyAStarNode existing = nodes.FirstOrDefault(n => n.Position == successor.Position);
                    if (existing == null)
                    {
                        nodes.Add(successor);
                        continue;
                    }

                    // If the new path is shorter, update its previous node which opens it again.
                    if (existing.CostF > successor.CostF)
                    {
                        existing.UpdatePrevious(node);
                    }
                }
            }

            return best;
        }
    }
}