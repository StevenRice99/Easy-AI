using System;
using UnityEngine;

namespace EasyAI.Navigation.Utility
{
    /// <summary>
    /// Hold data for a path lookup segment.
    /// </summary>
    [Serializable]
    public struct Path
    {
        [Tooltip("The node trying to reach index.")]
        [SerializeField]
        public int[] goal;
    }
}