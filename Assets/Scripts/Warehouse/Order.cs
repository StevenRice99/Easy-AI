using System;
using UnityEngine;

namespace Warehouse
{
    /// <summary>
    /// The details for an order.
    /// </summary>
    [Serializable]
    public struct Order
    {
        /// <summary>
        /// The requirements of the order.
        /// </summary>
        [Tooltip("The requirements of the order.")]
        public int[] requirements;
    }
}