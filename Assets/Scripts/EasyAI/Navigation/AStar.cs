using System.Collections.Generic;
using System.Linq;
using EasyAI.Navigation.Nodes;
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
        /// <param name="current">The starting position.</param>
        /// <param name="goal">The end goal position.</param>
        /// <param name="connections">All node connections in the scene.</param>
        /// <returns>The path of nodes to take to get from the starting position to the ending position.</returns>
        public static List<Vector3> Perform(Vector3 current, Vector3 goal, List<Connection> connections)
        {
            AStarNode best = null;
        
            // Add the starting position to the list of nodes.
            List<AStarNode> aStarNodes = new() { new(current, goal) };

            // Loop until there are no options left meaning the path cannot be completed.
            while (aStarNodes.Any(n => n.IsOpen))
            {
                // Get the open node with the lowest F cost and then by the lowest H cost if there is a tie in F cost.
                AStarNode node = aStarNodes.Where(n => n.IsOpen).OrderBy(n => n.CostF).ThenBy(n => n.CostH).First();
            
                // Close the current node.
                node.Close();
            
                // Update this to the best node if it is.
                if (best == null || node.CostF < best.CostF)
                {
                    best = node;
                }
            
                // Loop through all nodes which connect to the current node.
                foreach (Connection connection in connections.Where(c => c.A == node.Position || c.B == node.Position))
                {
                    // Get the other position in the connection so we do not work with the exact same node again and get stuck.
                    Vector3 position = connection.A == node.Position ? connection.B : connection.A;
                
                    // Create the A* node.
                    AStarNode successor = new(position, goal, node);

                    // If this node is the goal destination, A* is done so set it as the best and clear the node list so the loop ends.
                    if (position == goal)
                    {
                        best = successor;
                        aStarNodes.Clear();
                        break;
                    }

                    // If the node is not yet in the list, add it.
                    AStarNode existing = aStarNodes.FirstOrDefault(n => n.Position == position);
                    if (existing == null)
                    {
                        aStarNodes.Add(successor);
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

            // If there was no best node which should never happen, simply return a line between the start and end positions.
            if (best == null)
            {
                return new() { current, goal };
            }

            // Go from the last to node to the first adding all positions to the path.
            List<Vector3> path = new();
            while (best != null)
            {
                path.Add(best.Position);
                best = best.Previous;
            }

            // Reverse the path so it is from start to goal and return it.
            path.Reverse();
        
            return path;
        }
    }
}