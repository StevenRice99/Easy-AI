using System.Linq;
using UnityEngine;

namespace A1.Percepts
{
    /// <summary>
    /// Hold positions, dirt levels, and if they are likely to get dirty for all floor tiles in the scene.
    /// </summary>
    public sealed class FloorsData
    {
        /// <summary>
        /// Positions of all floor tiles.
        /// </summary>
        public readonly Vector3[] Positions;

        /// <summary>
        /// The floor tiles which are likely to get dirty.
        /// </summary>
        public readonly bool[] Dirty;

        /// <summary>
        /// If each floor tile is likely to get dirty or not.
        /// </summary>
        public readonly bool[] LikelyToGetDirty;

        /// <summary>
        /// Assign floor details
        /// </summary>
        /// <param name="positions">Floor positions.</param>
        /// <param name="dirty">If a floor tile is dirty or not.</param>
        /// <param name="likelyToGetDirty">If a floor tile is likely to get dirty or not.</param>
        public FloorsData(Vector3[] positions, bool[] dirty, bool[] likelyToGetDirty)
        {
            Positions = positions;
            Dirty = dirty;
            LikelyToGetDirty = likelyToGetDirty;
        }

        /// <summary>
        /// Display the details of the percepts.
        /// </summary>
        /// <returns>String with the details of the percepts.</returns>
        public string DetailsDisplay()
        {
            int dirtyCount = Dirty.Count(dirty => dirty);
            int likelyCount = LikelyToGetDirty.Count(likely => likely);
            return $"{Positions.Length} floor tiles, {dirtyCount} dirty, {likelyCount} likely to get dirty.";
        }
    }
}