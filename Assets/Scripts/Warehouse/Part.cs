using UnityEngine;

namespace Warehouse
{
    /// <summary>
    /// Warehouse parts for agents to work with.
    /// </summary>
    [DisallowMultipleComponent]
    public class Part : MonoBehaviour, IReset
    {
        /// <summary>
        /// The ID for this part.
        /// </summary>
        [field: Tooltip("The ID for this part.")]
        [field: Min(0)]
        [field: SerializeField]
        public int ID { get; private set; }

        /// <summary>
        /// Reset this object.
        /// </summary>
        public void ResetObject()
        {
            
        }
    }
}