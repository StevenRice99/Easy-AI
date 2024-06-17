using System.Collections.Generic;
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

        /// <summary>
        /// Locations you can place certain IDs.
        /// </summary>
        public static readonly Dictionary<int, HashSet<Storage>> PlaceOptions = new();

        /// <summary>
        /// Locations you can pick certain IDs.
        /// </summary>
        public static readonly Dictionary<int, HashSet<Storage>> PickOptions = new();

        /// <summary>
        /// The height offset to account for
        /// </summary>
        [Tooltip("The height offset to account for.")]
        [SerializeField]
        private float offset = 0.7f;

        /// <summary>
        /// The part this can store.
        /// </summary>
        public int ID { get; private set; }

        /// <summary>
        /// Whether this has been claimed or not.
        /// </summary>
        public bool Claimed => _interacting != null;

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
        private float Cost => (transform.position.y - offset) * WarehouseManager.InteractTimeScale;
        
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
        /// Set new the ID for this to use.
        /// </summary>
        /// <param name="set">The new ID to set.</param>
        public void SetId(int set)
        {
            // Remove the previous ID from the lookup table.
            if (PlaceOptions.ContainsKey(ID) && PlaceOptions[ID].Contains(this))
            {
                PlaceOptions[ID].Remove(this);
                if (PlaceOptions[ID].Count < 1)
                {
                    PlaceOptions.Remove(ID);
                }
            }

            // Set the new ID.
            ID = set;

            // If this has a part which does not match the new ID, destroy it.
            if (!Empty && ID != _part.ID)
            {
                if (PickOptions.TryGetValue(_part.ID, out HashSet<Storage> option))
                {
                    option.Remove(this);
                    if (option.Count < 1)
                    {
                        PickOptions.Remove(_part.ID);
                    }
                }
                
                Destroy(_part.gameObject);
            }

            // Update information about what is placeable here.
            UpdatePlaceable();
        }

        /// <summary>
        /// Indicate that this is now placeable.
        /// </summary>
        private void UpdatePlaceable()
        {
            if (PlaceOptions.TryGetValue(ID, out HashSet<Storage> placeable))
            {
                placeable.Add(this);
            }
            else
            {
                PlaceOptions[ID] = new() {this};
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
        /// Check if this can take a part with an ID.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <param name="id">The ID to check for.</param>
        /// <returns>True if it can take a part with the ID, false otherwise.</returns>
        public bool PlaceAvailable(WarehouseAgent agent, int id) => (_interacting == null || IsInteracting(agent)) && Empty && ID == id;

        /// <summary>
        /// Claim an ID for an agent.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <param name="id"></param>
        /// <returns>True if it can be claimed, false otherwise.</returns>
        public bool PlaceClaim(WarehouseAgent agent, int id)
        {
            if (!PlaceAvailable(agent, id))
            {
                return false;
            }

            _interacting = agent;
            return true;
        }

        /// <summary>
        /// Release the claim on this.
        /// </summary>
        /// <param name="agent">The agent making the request.</param>
        public void ReleaseClaim(WarehouseAgent agent)
        {
            if (!IsInteracting(agent))
            {
                return;
            }

            _interacting = null;
            _interactingTime = 0;
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
        /// Check if this has a part with an ID that is available.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <param name="id">The ID to check for.</param>
        /// <returns>True if it has a part with the ID, false otherwise.</returns>
        public bool PickAvailable(WarehouseAgent agent, int id)
        {
            if (Empty || _part.ID != id)
            {
                return false;
            }

            return _interacting == null || IsInteracting(agent);
        }
        
        /// <summary>
        /// Claim an ID for an agent.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <param name="id"></param>
        /// <returns>True if it can be claimed, false otherwise.</returns>
        public bool PickClaim(WarehouseAgent agent, int id)
        {
            if (!PickAvailable(agent, id))
            {
                return false;
            }

            _interacting = agent;
            return true;
        }
        
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
            // If the agent does not have a part or this cannot hold this part do nothing.
            if (!agent.HasPart || !PlaceAvailable(agent, agent.Id))
            {
                return false;
            }

            // If not done the process of storing, up the time.
            if (!InteractionComplete)
            {
                _interactingTime += Time.deltaTime;
                return false;
            }

            // Otherwise, placement is complete to get the agent's part.
            Part part = agent.Remove();
            if (part == null)
            {
                ReleaseClaim(agent);
                return false;
            }

            // Add score to the agent for storing the part.
            agent.AddStoreScore();
            
            Set(part);
            
            // The agent is no longer using this so release the claim.
            ReleaseClaim(agent);
            
            return true;
        }

        /// <summary>
        /// Set the part.
        /// </summary>
        /// <param name="part">The part to set</param>
        public void Set(Part part)
        {
            // Add the part to the storage.
            _part = part;
            _part.transform.parent = transform;
            _part.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);

            // Remove from placement options.
            if (PlaceOptions.TryGetValue(ID, out HashSet<Storage> placeOption))
            {
                placeOption.Remove(this);
            }
            
            // Add to pick options.
            if (PickOptions.TryGetValue(_part.ID, out HashSet<Storage> pickOption))
            {
                pickOption.Add(this);
            }
            else
            {
                PickOptions[_part.ID] = new() { this };
            }
        }

        /// <summary>
        /// Pick a part to an agent.
        /// </summary>
        /// <param name="agent">The agent picking the part.</param>
        /// <returns>True if it was picked up, false otherwise.</returns>
        public bool Pick(WarehouseAgent agent)
        {
            // If there is no part at this storage or the agent has a part or this is not the part they want they cannot pick.
            if (Empty || agent.HasPart || (agent.Id >= 0 && _part.ID != agent.Id))
            {
                return false;
            }
            
            // If nothing is interacting with this, attach the agent to it.
            if (_interacting == null)
            {
                _interacting = agent;
                _interactingTime = 0;
            }
            // Otherwise if a different agent is already interacting, cannot place.
            else if (_interacting != agent)
            {
                return false;
            }
            
            // If not done the process of picking, up the time.
            if (!InteractionComplete)
            {
                _interactingTime += Time.deltaTime;
                return false;
            }

            // Remove from pick options.
            if (PickOptions.TryGetValue(_part.ID, out HashSet<Storage> option))
            {
                option.Remove(this);
                if (option.Count < 1)
                {
                    PickOptions.Remove(_part.ID);
                }
            }
            
            // Add the part into storage.
            _part.transform.parent = agent.HoldLocation;
            _part.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            _part = null;
            
            // The agent is no longer using this so release the claim.
            ReleaseClaim(agent);

            // Set that this can be placed at.
            UpdatePlaceable();
            
            return true;
        }

        /// <summary>
        /// This function is called when the object becomes enabled and active.
        /// </summary>
        private void OnEnable()
        {
            Instances.Add(this);

            SetId(ID);
        }

        /// <summary>
        /// This function is called when the behaviour becomes disabled.
        /// </summary>
        private void OnDisable()
        {
            Instances.Remove(this);

            SetId(ID);
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
            
            UpdatePlaceable();

            _interacting = null;
            _interactingTime = 0;
        }
    }
}