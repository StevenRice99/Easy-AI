using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Warehouse
{
    /// <summary>
    /// Helper to set the IDs of what storage units can hold at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public class IdSetter : MonoBehaviour
    {
        /// <summary>
        /// Automatically set the IDs of storage spaces in this hierarchy.
        /// </summary>
        [Tooltip("Automatically set the IDs of storage spaces in this hierarchy.")]
        [SerializeField]
        private int[] ids = {};
        
        /// <summary>
        /// Awake is called when an enabled script instance is being loaded.
        /// </summary>
        private void Awake()
        {
            List<Storage> storages = GetComponents<Storage>().ToList();
            storages.AddRange(GetComponentsInChildren<Storage>());

            foreach (Storage storage in storages)
            {
                storage.UpdateIds(ids);
            }
            
            Destroy(this);
        }
    }
}