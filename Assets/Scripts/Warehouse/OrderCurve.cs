using System;
using UnityEngine;

namespace Warehouse
{
    /// <summary>
    /// An order curve a warehouse can follow.
    /// </summary>
    [Serializable]
    public struct OrderCurve
    {
        /// <summary>
        /// The orders for each time period.
        /// </summary>
        [Tooltip("The orders for each time period.")]
        public Order[] orders;
    }
}