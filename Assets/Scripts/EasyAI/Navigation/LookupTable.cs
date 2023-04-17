using System.Collections.Generic;
using System.Linq;
using EasyAI.Navigation.Utility;
using UnityEditor;
using UnityEngine;

namespace EasyAI.Navigation
{
    /// <summary>
    /// The connections path data for pathfinding.
    /// </summary>
    [CreateAssetMenu(menuName = "Easy-AI/Lookup Table", fileName = "Lookup Table", order = 0)]
    public class LookupTable : ScriptableObject
    {
        [field: Tooltip("Navigation nodes.")]
        [field: SerializeField]
        public Vector3[] Nodes { get; private set; }
        
        [field: Tooltip("Connection lookups.")]
        [field: SerializeField]
        public Connection[] Connections { get; private set; }
        
        [field: Tooltip("Path lookups.")]
        [field: SerializeField]
        public Path[] Paths { get; private set; }

        /// <summary>
        /// Set connections.
        /// </summary>
        /// <param name="nodes">The nodes to write.</param>
        /// <param name="connections">The connection to write.</param>
        /// <param name="paths">The paths to write.</param>
        public void Write(IEnumerable<Vector3> nodes, IEnumerable<Connection> connections, IEnumerable<Path> paths)
        {
            Nodes = nodes.ToArray();
            Connections = connections.ToArray();
            Paths = paths.ToArray();
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
#endif
        }
    }
}