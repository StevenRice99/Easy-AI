using System.Collections.Generic;
using System.Linq;
using EasyAI;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Warehouse
{
    /// <summary>
    /// Inbound source to the warehouse.
    /// </summary>
    [DisallowMultipleComponent]
    public class Inbound : MonoBehaviour, IPick, IReset
    {
        /// <summary>
        /// All inbound instances.
        /// </summary>
        public static readonly HashSet<Inbound> Instances = new();

        /// <summary>
        /// The spaces to spawn parts at.
        /// </summary>
        [Tooltip("The spaces to spawn parts at.")]
        [SerializeField]
        private Transform[] locations;

        /// <summary>
        /// All parts this current has.
        /// </summary>
        private readonly List<Part> _parts = new();

        /// <summary>
        /// All part IDs, even those which have been claimed.
        /// </summary>
        private readonly Dictionary<int, int> _all = new();
        
        /// <summary>
        /// All available parts that have not been claimed by any workers.
        /// </summary>
        private readonly Dictionary<int, int> _available = new();

        /// <summary>
        /// The amount of time since the last inbound shipment was fully collected.
        /// </summary>
        public float ElapsedTime { get; private set; }

        /// <summary>
        /// If this is empty or not.
        /// </summary>
        public bool Empty => _all.Count < 1;

        /// <summary>
        /// Get the ID of the next available item by demand.
        /// </summary>
        /// <returns>The highest demand item that is available.</returns>
        public int GetNext()
        {
            // Parts in the manager are ordered by demand, so get the first one we have a key of.
            int count = WarehouseManager.Parts.Length;
            for (int i = 0; i < count; i++)
            {
                if (_available.ContainsKey(i))
                {
                    return i;
                }
            }

            // -1 means this is empty.
            return -1;
        }

        /// <summary>
        /// Check if this has a part with an ID that is available.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <param name="id">The ID to check for.</param>
        /// <returns>True if it has a part with the ID, false otherwise.</returns>
        public bool PickAvailable(WarehouseAgent agent, int id) => _available.Keys.Contains(id);

        /// <summary>
        /// Claim an ID for an agent.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <param name="id"></param>
        /// <returns>True if it can be claimed, false otherwise.</returns>
        public bool PickClaim(WarehouseAgent agent, int id)
        {
            // Ensure this is available for the agent.
            if (!PickAvailable(agent, id))
            {
                return false;
            }

            // Remove the part from the list so other agents don't try to get it.
            _available[id]--;
            if (_available[id] < 1)
            {
                _available.Remove(id);
            }

            return true;
        }

        /// <summary>
        /// Pick a part to an agent.
        /// </summary>
        /// <param name="agent">The agent picking the part.</param>
        /// <returns>True if it was picked up, false otherwise.</returns>
        public bool Pick(WarehouseAgent agent)
        {
            // If there is no part at this inbound or the agent has a part they cannot pick.
            if (_all.Count < 1 || agent.HasPart)
            {
                return false;
            }

            // Get the part for the agent to pick up.
            Part part = agent.Id < 0 ? _parts[0] : _parts.FirstOrDefault(x => x.ID == agent.Id);
            if (part == null || !agent.Attach(part))
            {
                return false;
            }
            
            // Detach the part from this.
            _parts.Remove(part);
            if (_all.ContainsKey(part.ID))
            {
                _all[part.ID]--;
                if (_all[part.ID] < 1)
                {
                    _all.Remove(part.ID);
                }
            }

            // If all items have been unloaded, up the score.
            if (_all.Count < 1)
            {
                WarehouseManager.ShipmentsUnloaded();
            }
            
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
        /// Frame-rate independent MonoBehaviour. FixedUpdate message for physics calculations.
        /// </summary>
        private void FixedUpdate()
        {
            // Still have items so do nothing.
            if (_all.Count > 0)
            {
                return;
            }

            if (WarehouseManager.SupplyChain)
            {
                SpawnParts();
                return;
            }

            // No items so count until a new shipment arrives.
            ElapsedTime += Time.deltaTime;
            if (ElapsedTime >= WarehouseManager.InboundDelay)
            {
                SpawnParts();
            }
        }

        /// <summary>
        /// Spawn more parts for the warehouse.
        /// </summary>
        private void SpawnParts()
        {
            // Cleanup any previous parts.
            for (int i = 0; i < _parts.Count; i++)
            {
                Destroy(_parts[i].gameObject);
            }
            
            _parts.Clear();
            _all.Clear();
            _available.Clear();

            // Track if at least one item is placed.
            bool placed = false;
            
            // The number of part types there are.
            int count = WarehouseManager.Parts.Length;
            
            // Store how many options we can spawn.
            int[] options = new int[count];
            for (int i = 0; i < count; i++)
            {
                options[i] = 0;
            }

            // Every open storage space means we can spawn an item.
            foreach (Storage storage in Storage.Instances.Where(x => x.Empty))
            {
                options[storage.ID]++;
            }

            // But, if this item exists at an inbound, it has priority for storage so this takes away from our limit.
            foreach (KeyValuePair<int, int> ids in Instances.SelectMany(inbound => inbound._all))
            {
                options[ids.Key] -= ids.Value;
            }

            // Additionally, remove items that are being carried from the count.
            foreach (WarehouseAgent agent in WarehouseAgent.Instances.Where(agent => agent.HasPart))
            {
                options[agent.Id]--;
            }
            
            // Spawn at most how many parts we can fit.
            for (int i = 0; i < locations.Length; i++)
            {
                int id;
                if (WarehouseManager.SupplyChain)
                {
                    id = 0;
                    for (int j = 1; j < options.Length; j++)
                    {
                        if (options[j] > options[id])
                        {
                            id = j;
                        }
                    }

                    if (options[id] == 0)
                    {
                        break;
                    }

                    int attempts = 0;

                    Dictionary<int, int> order = WarehouseManager.CurrentOrder;

                    bool canAdd = false;
                    
                    while (attempts <= count)
                    {
                        if (order.ContainsKey(id) && order[id] > 0)
                        {
                            canAdd = true;
                            break;
                        }

                        attempts++;
                
                        // Check the next ID, cycling back to zero if needed.
                        id++;
                        if (id >= count)
                        {
                            id = 0;
                        }
                    }

                    if (!canAdd)
                    {
                        break;
                    }

                    order[id]--;
                    if (order[id] < 1)
                    {
                        order.Remove(id);
                    }
                }
                else
                {
                    // Get a random ID for the part.
                    id = Random.Range(0, count);
            
                    // We can only attempt to change the ID as many times as there are ID options.
                    int attempts = 0;

                    // If the part can be added or not.
                    bool canAdd = true;
                    
                    // Ensure there is space in the warehouse to take this item.
                    while (options[id] <= 0)
                    {
                        // Increment the number of attempts and exit if all have been exhausted and thus no part can be added.
                        attempts++;
                        if (attempts >= count)
                        {
                            canAdd = false;
                            break;
                        }
                
                        // Check the next ID, cycling back to zero if needed.
                        id++;
                        if (id >= count)
                        {
                            id = 0;
                        }
                    }

                    if (!canAdd)
                    {
                        break;
                    }
                }
            
                // Spawn and configure the part prefab as the warehouse can store it.
                Part part = Instantiate(WarehouseManager.PartPrefab, locations[i], true);
                part.SetId(id);
                _parts.Add(part);
                part.name = $"Part {id}";
                part.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            
                // Update the information for this.
                if (!_available.TryAdd(id, 1))
                {
                    _available[id]++;
                    _all[id]++;
                }
                else
                {
                    _all.Add(id, 1);
                }

                // Claim a spot in the warehouse for this part.
                options[id]--;
            
                // At least one has been placed.
                placed = true;
            }

            // If something has been placed, restart the timer.
            if (placed)
            {
                ElapsedTime = 0;
            }
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
            Vector3 storage = transform.position;
            float pickup = EasyManager.PathLength(EasyManager.LookupPath(position, storage), position) / speed;
            return pickup + EasyManager.PathLength(EasyManager.LookupPath(storage, place), storage) / speed;
        }

        /// <summary>
        /// Reset this object.
        /// </summary>
        public void ResetObject()
        {
            _all.Clear();
            _available.Clear();
            
            while (!Empty)
            {
                Destroy(_parts[0].gameObject);
                _parts.RemoveAt(0);
            }

            ElapsedTime = WarehouseManager.InboundDelay;
        }
    }
}