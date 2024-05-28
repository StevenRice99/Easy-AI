using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Warehouse
{
    /// <summary>
    /// Outbound order from the warehouse.
    /// </summary>
    [DisallowMultipleComponent]
    public class Outbound : MonoBehaviour, IPlace
    {
        /// <summary>
        /// All outbound instances.
        /// </summary>
        public static readonly HashSet<Outbound> Instances = new();
        
        /// <summary>
        /// The options this can request for an outbound order.
        /// </summary>
        [Tooltip("The options this can request for an outbound order.")]
        [SerializeField]
        private int[] options = { };

        /// <summary>
        /// The maximum options that can be required for this order.
        /// </summary>
        [Tooltip("The maximum options that can be required for this order.")]
        [Min(1)]
        [SerializeField]
        private int max = 1;

        /// <summary>
        /// The amount of time before a new order comes in.
        /// </summary>
        [Tooltip("The amount of time before a new order comes in.")]
        [Min(0)]
        [SerializeField]
        private float delay;
        
        /// <summary>
        /// All requirements for the current order.
        /// </summary>
        private readonly Dictionary<int, int> _requirements = new();

        /// <summary>
        /// How much time has passed since the previous order was completed.
        /// </summary>
        private float _elapsedTime;

        /// <summary>
        /// If this order is currently active.
        /// </summary>
        public bool Active => _requirements.Count > 0;

        /// <summary>
        /// Get all requirement types.
        /// </summary>
        /// <returns>The required types.</returns>
        public int[] Requirements() => _requirements.Select(x => x.Key).ToArray();

        /// <summary>
        /// Check if this requires an ID.
        /// </summary>
        /// <param name="id">The ID to check.</param>
        /// <returns>True if this required the ID, false otherwise.</returns>
        public bool Requires(int id) => _requirements.ContainsKey(id);

        /// <summary>
        /// Place a part at this location.
        /// </summary>
        /// <param name="agent">The agent placing the part.</param>
        /// <returns>True if the part was added, false otherwise.</returns>
        public bool Place(WarehouseAgent agent)
        {
            if (!agent.HasPart || !_requirements.ContainsKey(agent.Id))
            {
                return false;
            }
            
            int id = agent.Destroy();
            if (id >= 0)
            {
                _requirements[id]--;
                if (_requirements[id] < 1)
                {
                    _requirements.Remove(id);
                }
            }
            
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
            CreateOrder();
        }

        /// <summary>
        /// Frame-rate independent MonoBehaviour. FixedUpdate message for physics calculations.
        /// </summary>
        private void FixedUpdate()
        {
            if (_requirements.Count > 0)
            {
                return;
            }

            _elapsedTime += Time.deltaTime;
            if (_elapsedTime >= delay)
            {
                CreateOrder();
            }
        }

        /// <summary>
        /// Create a new order.
        /// </summary>
        private void CreateOrder()
        {
            int number = Random.Range(1, max + 1);
            for (int i = 0; i < number; i++)
            {
                int option = options[Random.Range(0, options.Length)];
                if (!_requirements.TryAdd(option, 1))
                {
                    _requirements[option]++;
                }
            }

            delay = 0;
            WarehouseAgent.WarehouseUpdated(this);
        }
    }
}