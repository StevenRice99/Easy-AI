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
        [field: Tooltip("The current node index.")]
        [field: SerializeField]
        public int Current { get; private set; }
        
        [field: Tooltip("The node trying to reach index.")]
        [field: SerializeField]
        public int Goal { get; private set; }
        
        [field: Tooltip("The next node to move to index.")]
        [field: SerializeField]
        public int Next { get; private set; }

        /// <summary>
        /// Create a data entry for a navigation lookup table.
        /// </summary>
        /// <param name="current">The current node index.</param>
        /// <param name="goal">The node trying to reach index.</param>
        /// <param name="next">The next node to move to index.</param>
        public NavigationLookup(int current, int goal, int next)
        {
            Current = current;
            Goal = goal;
            Next = next;
        }
    }
}