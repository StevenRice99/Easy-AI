using System.Collections.Generic;
using System.Linq;
using EasyAI;
using UnityEngine;

namespace Warehouse
{
    /// <summary>
    /// Store a part.
    /// </summary>
    [DisallowMultipleComponent]
    public class Storage : MonoBehaviour, IPick, IPlace, IReset
    {
        /// <summary>
        /// All storage instances.
        /// </summary>
        public static readonly HashSet<Storage> Instances = new();

        public static Dictionary<int, HashSet<Storage>> Options = new();

        /// <summary>
        /// How much does interacting take scaled with the Y position of this storage.
        /// </summary>
        [Tooltip("How much does interacting take scaled with the Y position of this storage.")]
        [Min(0)]
        [SerializeField]
        private float interactTimeScale = 1;

        /// <summary>
        /// The height offset to account for
        /// </summary>
        [Tooltip("The height offset to account for.")]
        [SerializeField]
        private float offset = 0.7f;
        
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
        public float Cost => (transform.position.y - offset) * interactTimeScale;
        
        /// <summary>
        /// The part currently being stored.
        /// </summary>
        private Part _part;

        /// <summary>
        /// The agent interacting with this.
        /// </summary>
        private WarehouseAgent _interacting;

        /// <summary>
        /// The time which has been spent interacting.
        /// </summary>
        private float _interactingTime;

        /// <summary>
        /// If enough time has elapsed to interact with this.
        /// </summary>
        private bool InteractionComplete => _interactingTime >= Cost;

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
        /// Get the time it would take to place a part at this location.
        /// </summary>
        /// <param name="position">The position the placer is currently at.</param>
        /// <param name="speed">How fast the placer can move.</param>
        /// <returns>The time it would take to place a part at this location.</returns>
        public float PlaceTime(Vector3 position, float speed)
        {
            return EasyManager.PathLength(EasyManager.LookupPath(position, MoveTarget), position) / speed + Cost;
        }

        /// <summary>
        /// How long it would for this agent to pick up from this and then deliver to a spot to place it.
        /// </summary>
        /// <param name="position">The position the picker is currently at.</param>
        /// <param name="place">The place to deliver to.</param>
        /// <param name="speed">How fast the picker can move.</param>
        /// <returns>The time it would take an agent to collect from this and deliver it to the outbound.</returns>
        public float PickTime(Vector3 position, Vector3 place, float speed)
        {
            Vector3 storage = MoveTarget;
            float pickup = EasyManager.PathLength(EasyManager.LookupPath(position, storage), position) / speed + Cost;
            return pickup + EasyManager.PathLength(EasyManager.LookupPath(storage, place), storage) / speed;
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
        /// Check if this storage is available or not.
        /// </summary>
        /// <param name="agent">The agent checking.</param>
        /// <returns>True if it is available, false otherwise.</returns>
        public bool Available(WarehouseAgent agent) => _interacting == null || IsInteracting(agent);
        
        /// <summary>
        /// Check if this storage is currently being interacted with.
        /// </summary>
        /// <param name="agent">The agent checking.</param>
        /// <returns>True if they are interacting, false otherwise.</returns>
        public bool IsInteracting(WarehouseAgent agent) => _interacting == agent;

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

            if (_interacting == null)
            {
                _interacting = agent;
                _interactingTime = 0;
                WarehouseAgent.WarehouseUpdated(this);
            }
            else if (_interacting != agent)
            {
                return false;
            }

            if (!InteractionComplete)
            {
                _interactingTime += Time.deltaTime;
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
            _interacting = null;
            _interactingTime = 0;
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
            
            if (_interacting == null)
            {
                _interacting = agent;
                _interactingTime = 0;
                WarehouseAgent.WarehouseUpdated(this);
            }
            else if (_interacting != agent)
            {
                return false;
            }
            
            if (!InteractionComplete)
            {
                _interactingTime += Time.deltaTime;
                return false;
            }

            _part.transform.parent = agent.HoldLocation;
            _part.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            _part = null;
            _interacting = null;
            _interactingTime = 0;
            WarehouseAgent.WarehouseUpdated(this);
            return true;
        }

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        private void OnEnable()
        {
            Instances.Add(this);

            foreach (int id in ids)
            {
                if (Options.ContainsKey(id))
                {
                    Options[id].Add(this);
                }
                else
                {
                    Options[id] = new() {this};
                }
            }
        }

        /// <summary>
        /// This function is called when the behaviour becomes disabled.
        /// </summary>
        private void OnDisable()
        {
            Instances.Remove(this);

            foreach (int id in ids)
            {
                if (!Options.ContainsKey(id) || !Options[id].Contains(this))
                {
                    continue;
                }

                Options[id].Remove(this);
                if (Options[id].Count < 1)
                {
                    Options.Remove(id);
                }
            }
        }

        /// <summary>
        /// Implement OnDrawGizmosSelected to draw a gizmo if the object is selected.
        /// </summary>
        private void OnDrawGizmosSelected()
        {
            Gizmos.DrawWireSphere(MoveTarget, 0.1f);
        }

        /// <summary>
        /// Reset this object.
        /// </summary>
        public void ResetObject()
        {
            if (_part != null)
            {
                Destroy(_part.gameObject);
            }

            _interacting = null;
            _interactingTime = 0;
        }
    }
}