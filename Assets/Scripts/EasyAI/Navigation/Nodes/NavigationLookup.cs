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
        [Tooltip("The node trying to reach index.")]
        [SerializeField]
        public int[] goal;
    }
}