using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Warehouse
{
    /// <summary>
    /// Inbound source to the warehouse.
    /// </summary>
    [DisallowMultipleComponent]
    public class Inbound : MonoBehaviour, IPick
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
        /// The amount of time since the last inbound shipment was fully collected.
        /// </summary>
        private float _elapsedTime;

        /// <summary>
        /// If this is empty or not.
        /// </summary>
        public bool Empty => _parts.Count < 1;

        /// <summary>
        /// Check if this has a part with an ID.
        /// </summary>
        /// <param name="id">The ID to check for.</param>
        /// <returns>True if it has a part with the ID, false otherwise.</returns>
        public bool Has(int id) => _parts.Any(x => x.ID == id);

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

            _elapsedTime += Time.deltaTime;
            if (_elapsedTime >= delay)
            {
                SpawnParts();
            }
        }

        /// <summary>
        /// Spawn more parts for the warehouse.
        /// </summary>
        private void SpawnParts()
        {
            _parts.Clear();
            for (int i = 0; i < locations.Length; i++)
            {
                Part part = Instantiate(prefabs[Random.Range(0, prefabs.Length)], locations[i], true);
                _parts.Add(part);
                part.name = $"Part {_parts[i].ID}";
                part.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            }

            _elapsedTime = 0;
            WarehouseAgent.WarehouseUpdated(this);
        }
    }
}