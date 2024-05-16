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
    public class EasyLookupTable : ScriptableObject
    {
        /// <summary>
        /// Navigation nodes.
        /// </summary>
        [field: Tooltip("Navigation nodes.")]
        [field: SerializeField]
        public Vector3[] Nodes { get; private set; }
        
        /// <summary>
        /// Connection lookups.
        /// </summary>
        [field: Tooltip("Connection lookups.")]
        [field: SerializeField]
        public EasyConnection[] Connections { get; private set; }
        
        /// <summary>
        /// Path lookups.
        /// </summary>
        [field: Tooltip("Path lookups.")]
        [field: SerializeField]
        public EasyPath[] Paths { get; private set; }

        /// <summary>
        /// Set connections.
        /// </summary>
        /// <param name="nodes">The nodes to write.</param>
        /// <param name="connections">The connection to write.</param>
        /// <param name="paths">The paths to write.</param>
        public void Write(IEnumerable<Vector3> nodes, IEnumerable<EasyConnection> connections, IEnumerable<EasyPath> paths)
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