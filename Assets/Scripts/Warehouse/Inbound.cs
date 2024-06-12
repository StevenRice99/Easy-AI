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
        /// The potential parts to spawn.
        /// </summary>
        [Tooltip("The potential parts to spawn.")]
        [SerializeField]
        private Part[] prefabs;

        /// <summary>
        /// The spaces to spawn parts at.
        /// </summary>
        [Tooltip("The spaces to spawn parts at.")]
        [SerializeField]
        private Transform[] locations;

        /// <summary>
        /// The amount of time before a new inbound shipment comes in.
        /// </summary>
        [Tooltip("The amount of time before a new inbound shipment comes in.")]
        [Min(0)]
        [SerializeField]
        private float delay;

        /// <summary>
        /// All parts this current has.
        /// </summary>
        private readonly List<Part> _parts = new();
        
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
        public bool Empty => _parts.Count < 1;

        /// <summary>
        /// Get a random item which can be claimed.
        /// </summary>
        /// <returns></returns>
        public int GetRandom()
        {
            if (_available.Count < 1)
            {
                return -1;
            }

            return _available.Keys.ToArray()[Random.Range(0, _available.Keys.Count)];
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
            if (_parts.Count < 1 || agent.HasPart)
            {
                return false;
            }

            Part part = agent.Id < 0 ? _parts[0] : _parts.FirstOrDefault(x => x.ID == agent.Id);
            if (part == null || !agent.Attach(part))
            {
                return false;
            }
            
            _parts.Remove(part);

            if (_parts.Count < 1)
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
        /// Start is called on the frame when a script is enabled just before any of the Update methods are called the first time.
        /// </summary>
        private void Start()
        {
            SpawnParts();
        }

        /// <summary>
        /// Frame-rate independent MonoBehaviour. FixedUpdate message for physics calculations.
        /// </summary>
        private void FixedUpdate()
        {
            if (_parts.Count > 0)
            {
                return;
            }

            ElapsedTime += Time.deltaTime;
            if (ElapsedTime >= delay)
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
            _available.Clear();
            
            for (int i = 0; i < locations.Length; i++)
            {
                Part part = Instantiate(prefabs[Random.Range(0, prefabs.Length)], locations[i], true);
                _parts.Add(part);
                part.name = $"Part {_parts[i].ID}";
                part.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
                if (!_available.TryAdd(part.ID, 1))
                {
                    _available[part.ID]++;
                }
            }

            ElapsedTime = 0;
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
            _available.Clear();
            
            while (!Empty)
            {
                Destroy(_parts[0].gameObject);
                _parts.RemoveAt(0);
            }

            ElapsedTime = delay;
        }
    }
}