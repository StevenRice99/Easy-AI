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
            int count = WarehouseManager.Parts.Length;
            for (int i = 0; i < count; i++)
            {
                if (_available.ContainsKey(i))
                {
                    return i;
                }
            }

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
            if (!PickAvailable(agent, id))
            {
                return false;
            }

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
            if (_all.Count < 1 || agent.HasPart)
            {
                return false;
            }

            Part part = agent.Id < 0 ? _parts[0] : _parts.FirstOrDefault(x => x.ID == agent.Id);
            if (part == null || !agent.Attach(part))
            {
                return false;
            }
            
            _parts.Remove(part);
            if (_all.ContainsKey(part.ID))
            {
                _all[part.ID]--;
                if (_all[part.ID] < 1)
                {
                    _all.Remove(part.ID);
                }
            }

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
            if (_all.Count > 0)
            {
                return;
            }

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
            for (int i = 0; i < _parts.Count; i++)
            {
                Destroy(_parts[i].gameObject);
            }
            
            _parts.Clear();
            _all.Clear();
            _available.Clear();

            bool placed = false;
            
            int count = WarehouseManager.Parts.Length;
            int[] options = new int[count];
            for (int i = 0; i < count; i++)
            {
                options[i] = 0;
            }

            foreach (Storage storage in Storage.Instances.Where(x => !x.Claimed && x.Empty))
            {
                options[storage.ID]++;
            }

            foreach (KeyValuePair<int, int> ids in Instances.SelectMany(inbound => inbound._all))
            {
                options[ids.Key] -= ids.Value;
            }
            
            for (int i = 0; i < locations.Length; i++)
            {
                Part part = Instantiate(WarehouseManager.PartPrefab, locations[i], true);
                int id = Random.Range(0, count);
                int attempts = 0;
                while (options[id] <= 0)
                {
                    attempts++;
                    if (attempts >= count)
                    {
                        return;
                    }
                    
                    id++;
                    if (id >= count)
                    {
                        id = 0;
                    }
                }
                
                part.SetId(id);
                _parts.Add(part);
                part.name = $"Part {id}";
                part.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                if (!_available.TryAdd(id, 1))
                {
                    _available[id]++;
                    _all[id]++;
                }
                else
                {
                    _all.Add(id, 1);
                }

                options[id]--;
                placed = true;
            }

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