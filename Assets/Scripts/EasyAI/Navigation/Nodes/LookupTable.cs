using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EasyAI.Navigation.Nodes
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
        
        /// <summary>
        /// The connections.
        /// </summary>
        [field: Tooltip("Navigation lookups.")]
        [field: SerializeField]
        public NavigationLookup[] Lookups { get; private set; }

        /// <summary>
        /// Set connections.
        /// </summary>
        /// <param name="nodes">The nodes to write.</param>
        /// <param name="lookups">The lookups to write.</param>
        public void Write(IEnumerable<Vector3> nodes,IEnumerable<NavigationLookup> lookups)
        {
            Nodes = nodes.ToArray();
            Lookups = lookups.ToArray();
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
#endif
        }
    }
}