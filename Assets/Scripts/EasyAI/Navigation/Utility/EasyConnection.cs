using System;
using UnityEngine;

namespace EasyAI.Navigation.Utility
{
    /// <summary>
    /// Hold a connection lookup between two nodes.
    /// </summary>
    [Serializable]
    public struct EasyConnection
    {
        /// <summary>
        /// A node index in the connection.
        /// </summary>
        [field: Tooltip("A node index in the connection.")]
        [field: SerializeField]
        public int A { get; private set; }
        
        /// <summary>
        /// A node index in the connection.
        /// </summary>
        [field: Tooltip("A node index in the connection.")]
        [field: SerializeField]
        public int B { get; private set; }

        /// <summary>
        /// Add a connection for two nodes.
        /// </summary>
        /// <param name="a">The first node index.</param>
        /// <param name="b">The second node index.</param>
        public EasyConnection(int a, int b)
        {
            A = a;
            B = b;
        }
    }
}