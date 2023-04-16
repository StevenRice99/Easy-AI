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
        /// <summary>
        /// The connections.
        /// </summary>
        [Tooltip("Navigation data.")]
        [field: SerializeField]
        public NavigationLookup[] Data { get; private set; }

        /// <summary>
        /// Set connections.
        /// </summary>
        /// <param name="data">The data to write.</param>
        public void Write(IEnumerable<NavigationLookup> data)
        {
            Data = data.ToArray();
#if UNITY_EDITOR
            EditorUtility.SetDirty(this);
            AssetDatabase.SaveAssets();
#endif
        }
    }
}