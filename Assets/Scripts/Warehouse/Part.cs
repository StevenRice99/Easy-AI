using UnityEngine;

namespace Warehouse
{
    /// <summary>
    /// Warehouse parts for agents to work with.
    /// </summary>
    [DisallowMultipleComponent]
    public class Part : MonoBehaviour
    {
        /// <summary>
        /// The ID for this part.
        /// </summary>
        [field: Tooltip("The ID for this part.")]
        [field: Min(0)]
        [field: SerializeField]
        public int ID { get; private set; }
        
        /// <summary>
        /// The visuals for this part.
        /// </summary>
        [field: Tooltip("The visuals for this part.")]
        [field: SerializeField]
        public MeshRenderer Visuals { get; private set; }

        /// <summary>
        /// Set the ID of this part.
        /// </summary>
        /// <param name="id">The ID to set.</param>
        public void SetId(int id)
        {
            ID = id;
            Visuals.material.color = WarehouseManager.Parts[id].color;
        }

        /// <summary>
        /// Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
        /// </summary>
        private void OnValidate()
        {
            if (Visuals != null)
            {
                return;
            }

            Visuals = GetComponent<MeshRenderer>();
            if (Visuals != null)
            {
                return;
            }

            Visuals = GetComponentInChildren<MeshRenderer>();
            if (Visuals != null)
            {
                return;
            }

            Visuals = gameObject.AddComponent<MeshRenderer>();
        }
    }
}