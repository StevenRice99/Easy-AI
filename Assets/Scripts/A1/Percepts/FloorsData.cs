using System.Linq;
using EasyAI.Percepts;
using UnityEngine;

namespace A1.Percepts
{
    /// <summary>
    /// Hold positions, dirt levels, and if they are likely to get dirty for all floor tiles in the scene.
    /// </summary>
    public class FloorsData : PerceivedData
    {
        /// <summary>
        /// Positions of all floor tiles.
        /// </summary>
        public Vector3[] Positions;

        /// <summary>
        /// The floor tiles which are likely to get dirty.
        /// </summary>
        public bool[] Dirty;

        /// <summary>
        /// If each floor tile is likely to get dirty or not.
        /// </summary>
        public bool[] LikelyToGetDirty;

        /// <summary>
        /// Display the details of the percepts.
        /// </summary>
        /// <returns>String with the details of the percepts.</returns>
        public override string DetailsDisplay()
        {
            int dirtyCount = Dirty.Count(dirty => dirty);
            int likelyCount = LikelyToGetDirty.Count(likely => likely);
            return $"{Positions.Length} floor tiles, {dirtyCount} dirty, {likelyCount} likely to get dirty.";
        }
    }
}