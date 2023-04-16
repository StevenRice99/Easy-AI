using System;
using UnityEngine;

namespace EasyAI.Navigation.Nodes
{
    /// <summary>
    /// Hold data for the navigation lookup.
    /// </summary>
    [Serializable]
    public struct Lookup
    {
        [Tooltip("The node trying to reach index.")]
        [SerializeField]
        public int[] goal;
    }
}