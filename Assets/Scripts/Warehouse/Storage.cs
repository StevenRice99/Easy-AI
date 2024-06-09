using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

namespace Warehouse
{
    /// <summary>
    /// Store a part.
    /// </summary>
    [DisallowMultipleComponent]
    public class Storage : MonoBehaviour, IPick, IPlace
    {
        /// <summary>
        /// All storage instances.
        /// </summary>
        public static readonly HashSet<Storage> Instances = new();
        
        /// <summary>
        /// The types of parts that can be stored here.
        /// </summary>
        [Tooltip("The types of parts that can be stored here.")]
        public int[] ids = { };

        /// <summary>
        /// Offset to perform moves relative to.
        /// </summary>
        [Tooltip("Offset to perform moves relative to.")]
        [SerializeField]
        private Vector3 moveTarget;

        /// <summary>
        /// Where to move to for using this storage.
        /// </summary>
        public Vector3 MoveTarget => transform.position + moveTarget;

        /// <summary>
        /// How much it costs to access this.
        /// </summary>
        public float Cost => transform.position.y;
        
        /// <summary>
        /// The part currently being stored.
        /// </summary>
        private Part _part;

        /// <summary>
        /// If the storage space is currently empty.
        /// </summary>
        public bool Empty
        {
            get
            {
                // If there are no children then it is empty.
                if (transform.childCount < 1)
                {
                    return true;
                }

                // If there is a part properly linked, it is not empty.
                if (_part != null)
                {
                    return false;
                }
                
                // Otherwise, it should not be empty so find the part.
                _part = transform.GetChild(0).GetComponent<Part>();
                return _part == null;
            }
        }

        /// <summary>
        /// Check if this has a part with an ID.
        /// </summary>
        /// <param name="id">The ID to check for.</param>
        /// <returns>True if it has a part with the ID, false otherwise.</returns>
        public bool Has(int id) => !Empty && _part.ID == id;

        /// <summary>
        /// Check if this can store a part with an ID.
        /// </summary>
        /// <param name="id">The ID to check for.</param>
        /// <returns>True if it can store a part with the ID, false otherwise.</returns>
        public bool CanTake(int id) => Empty && ids.Contains(id);

        /// <summary>
        /// Place a part at this location.
        /// </summary>
        /// <param name="agent">The agent placing the part.</param>
        /// <returns>True if the part was added, false otherwise.</returns>
        public bool Place(WarehouseAgent agent)
        {
            if (!agent.HasPart || !CanTake(agent.Id))
            {
                return false;
            }

            Part part = agent.Remove();
            if (part == null)
            {
                return false;
            }

            agent.AddStoreScore();
            _part = part;
            _part.transform.parent = transform;
            _part.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            WarehouseAgent.WarehouseUpdated(this);
            return true;
        }

        /// <summary>
        /// Pick a part to an agent.
        /// </summary>
        /// <param name="agent">The agent picking the part.</param>
        /// <returns>True if it was picked up, false otherwise.</returns>
        public bool Pick(WarehouseAgent agent)
        {
            if (Empty || agent.HasPart || (agent.Id >= 0 && _part.ID != agent.Id))
            {
                return false;
            }

            _part.transform.parent = agent.HoldLocation;
            _part.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            _part = null;
            WarehouseAgent.WarehouseUpdated(this);
            return true;
        }

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        private void OnEnable()
        {
            Instances.Add(this);
        }

        /// <summary>
        /// This function is called when the behaviour becomes disabled.
        /// </summary>
        private void OnDisable()
        {
            Instances.Remove(this);
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