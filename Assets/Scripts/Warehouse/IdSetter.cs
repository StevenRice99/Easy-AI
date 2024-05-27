using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Warehouse
{
    [DisallowMultipleComponent]
    public class IdSetter : MonoBehaviour
    {
        [Tooltip("Automatically set the IDs of storage spaces in this hierarchy.")]
        [SerializeField]
        private int[] ids = {};
        
        private void Awake()
        {
            List<Storage> storages = GetComponents<Storage>().ToList();
            storages.AddRange(GetComponentsInChildren<Storage>());

            foreach (Storage storage in storages)
            {
                storage.ids = ids;
            }
            
            Destroy(this);
        }
    }
}