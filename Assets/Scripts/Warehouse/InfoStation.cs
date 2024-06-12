using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Warehouse
{
    /// <summary>
    /// Location used when agents cannot get information from anywhere to get their next action.
    /// </summary>
    [DisallowMultipleComponent]
    public class InfoStation : MonoBehaviour, IReset
    {
        /// <summary>
        /// All info station instances.
        /// </summary>
        public static readonly HashSet<InfoStation> Instances = new();

        /// <summary>
        /// Where to move to for using this info station.
        /// </summary>
        [Tooltip("Where to move to for using this info station.")]
        [SerializeField]
        private Vector3 moveTarget;

        /// <summary>
        /// Where to move to for using this info station.
        /// </summary>
        public Vector3 MoveTarget => transform.position + moveTarget;
        
        /// <summary>
        /// The visual meshes of this.
        /// </summary>
        private MeshRenderer[] _meshes;

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        private void OnEnable()
        {
            Instances.Add(this);
            
            List<MeshRenderer> meshes = GetComponents<MeshRenderer>().ToList();
            meshes.AddRange(GetComponentsInChildren<MeshRenderer>());
            _meshes = meshes.ToArray();
        }

        /// <summary>
        /// This function is called when the behaviour becomes disabled.
        /// </summary>
        private void OnDisable()
        {
            Instances.Remove(this);
        }

        /// <summary>
        /// Reset this object.
        /// </summary>
        public void ResetObject()
        {
            bool active = !WarehouseManager.Communication;
            foreach (MeshRenderer mesh in _meshes)
            {
                mesh.enabled = active;
            }
        }

        /// <summary>
        /// Implement OnDrawGizmosSelected to draw a gizmo if the object is selected.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(MoveTarget, 0.1f);
        }
    }
}