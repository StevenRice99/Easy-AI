using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace EasyAI.Navigation.Utility
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
        
        [field: Tooltip("Navigation lookups.")]
        [field: SerializeField]
        public Lookup[] Lookups { get; private set; }

        /// <summary>
        /// Set connections.
        /// </summary>
        /// <param name="nodes">The nodes to write.</param>
        /// <param name="connectionLookups">The connection lookups to write.</param>
        /// <param name="lookups">The lookups to write.</param>
        public void Write(IEnumerable<Vector3> nodes, IEnumerable<Connection> connectionLookups, IEnumerable<Lookup> lookups)
        {
            Nodes = nodes.ToArray();
            Connections = connectionLookups.ToArray();
            Lookups = lookups.ToArray();
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
#endif
        }
    }
}