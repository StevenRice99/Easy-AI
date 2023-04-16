using System.Collections.Generic;
using System.Linq;
using EasyAI.Navigation.Nodes;
using UnityEngine;

namespace EasyAI.Navigation
{
    /// <summary>
    /// Helper class to easily get successor nodes for positions.
    /// </summary>
    public class AStarPaths
    {
        /// <summary>
        /// Reference to the connections.
        /// </summary>
        private readonly List<Connection> _connections;

        /// <summary>
        /// Set the connections for this helper class.
        /// </summary>
        /// <param name="connections">Reference to the connections.</param>
        public AStarPaths(List<Connection> connections)
        {
            _connections = connections;
        }

        /// <summary>
        /// Get all successor positions.
        /// </summary>
        /// <param name="current">The current node.</param>
        /// <returns>All successor positions.</returns>
        public IEnumerable<Vector3> Successors(AStarNode current)
        {
            return Successors(current.Position);
        }

        /// <summary>
        /// Get all successor positions.
        /// </summary>
        /// <param name="current">The current node position.</param>
        /// <returns>All successor positions.</returns>
        public IEnumerable<Vector3> Successors(Vector3 current)
        {
            return _connections.Where(c => c.A == current || c.B == current).Select(connection => connection.A == current ? connection.B : connection.A);
        }
    }
}