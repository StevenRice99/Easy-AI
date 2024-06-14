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
        /// Whether this is for inbound or outbound agents.
        /// </summary>
        [field: Tooltip("Whether this is for inbound or outbound agents.")]
        [field: SerializeField]
        public bool Inbound { get; private set; }
        
        /// <summary>
        /// The visual meshes of this.
        /// </summary>
        private MeshRenderer[] _meshes;

        /// <summary>
        /// The transform of the main visual element.
        /// </summary>
        public Transform MainVisual { get; private set; }

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        private void OnEnable()
        {
            Instances.Add(this);
            
            // Collect visuals.
            List<MeshRenderer> meshes = GetComponents<MeshRenderer>().ToList();
            meshes.AddRange(GetComponentsInChildren<MeshRenderer>());
            _meshes = meshes.ToArray();

            // Get the main element for movement and visual purposes.
            Transform t = transform;
            
            if (t.childCount > 0)
            {
                MainVisual = t.GetChild(0);
                return;
            }

            GameObject child = new("Child")
            {
                transform =
                {
                    parent = t,
                    localPosition = Vector3.zero,
                    localRotation = Quaternion.identity
                }
            };

            MainVisual = child.transform;
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
            bool active = !WarehouseManager.Wireless;
            foreach (MeshRenderer mesh in _meshes)
            {
                mesh.enabled = active;
            }
        }
    }
}