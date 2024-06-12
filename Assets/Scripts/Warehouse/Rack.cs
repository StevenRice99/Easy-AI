using System.Collections.Generic;
using UnityEngine;

namespace Warehouse
{
    /// <summary>
    /// Manage a rack of storages for parts.
    /// </summary>
    [DisallowMultipleComponent]
    public class Rack : MonoBehaviour
    {
        /// <summary>
        /// All racks instances.
        /// </summary>
        public static readonly HashSet<Rack> Instances = new();
        
        /// <summary>
        /// All storages in this rack.
        /// </summary>
        private Storage[] _storages;

        /// <summary>
        /// Set the ID for all storages in this rack.
        /// </summary>
        /// <param name="id">The ID to set.</param>
        public void SetId(int id)
        {
            foreach (Storage storage in _storages)
            {
                storage.SetId(id);
            }
        }
        
        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        private void OnEnable()
        {
            _storages = GetComponentsInChildren<Storage>();

            Instances.Add(this);
        }

        /// <summary>
        /// This function is called when the behaviour becomes disabled.
        /// </summary>
        private void OnDisable()
        {
            Instances.Remove(this);
        }
    }
}