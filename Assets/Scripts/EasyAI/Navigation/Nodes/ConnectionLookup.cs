using System;
using UnityEngine;

namespace EasyAI.Navigation.Nodes
{
    /// <summary>
    /// Hold a connection lookup between two nodes.
    /// </summary>
    [Serializable]
    public struct ConnectionLookup
    {
        [field: Tooltip("A node index in the connection.")]
        [field: SerializeField]
        public int A { get; private set; }
        
        [field: Tooltip("A node index in the connection.")]
        [field: SerializeField]
        public int B { get; private set; }

        /// <summary>
        /// Add a connection for two nodes.
        /// </summary>
        /// <param name="a">The first node index.</param>
        /// <param name="b">The second node index.</param>
        public ConnectionLookup(int a, int b)
        {
            A = a;
            B = b;
        }
    }
}