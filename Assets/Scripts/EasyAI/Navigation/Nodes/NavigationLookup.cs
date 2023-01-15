using System;
using UnityEngine;

namespace EasyAI.Navigation.Nodes
{
    /// <summary>
    /// Hold data for the navigation lookup table.
    /// </summary>
    [Serializable]
    public struct NavigationLookup
    {
        /// <summary>
        /// The current or starting node.
        /// </summary>
        public Vector3 Current;
        
        /// <summary>
        /// Where the end goal of the navigation is.
        /// </summary>
        public Vector3 Goal;
        
        /// <summary>
        /// The node to move to from the current node in order to navigate towards the goal.
        /// </summary>
        public Vector3 Next;

        /// <summary>
        /// Create a data entry for a navigation lookup table.
        /// </summary>
        /// <param name="current">The current or starting node.</param>
        /// <param name="goal">Where the end goal of the navigation is.</param>
        /// <param name="next">The node to move to from the current node in order to navigate towards the goal.</param>
        public NavigationLookup(Vector3 current, Vector3 goal, Vector3 next)
        {
            Current = current;
            Goal = goal;
            Next = next;
        }
    }
}