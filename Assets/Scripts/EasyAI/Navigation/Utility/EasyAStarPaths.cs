using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyAI.Navigation.Utility
{
    /// <summary>
    /// Helper class to easily get successor nodes for positions.
    /// </summary>
    public class EasyAStarPaths
    {
        /// <summary>
        /// Reference to the connections.
        /// </summary>
        private readonly List<EasyVectorConnection> _connections;

        /// <summary>
        /// Set the connections for this helper class.
        /// </summary>
        /// <param name="connections">Reference to the connections.</param>
        public EasyAStarPaths(List<EasyVectorConnection> connections)
        {
            _connections = connections;
        }

        /// <summary>
        /// Get all successor positions.
        /// </summary>
        /// <param name="current">The current node position.</param>
        /// <param name="goal">The goal for the A* to reach.</param>
        /// <returns>All successor positions.</returns>
        public IEnumerable<EasyAStarNode> Successors(EasyAStarNode current, Vector3 goal)
        {
            return _connections.Where(c => c.A == current.Position || c.B == current.Position).Select(connection => new EasyAStarNode(connection.A == current.Position ? connection.B : connection.A, goal, current));
        }
    }
}