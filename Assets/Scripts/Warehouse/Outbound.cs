using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Warehouse
{
    [DisallowMultipleComponent]
    public class Outbound : MonoBehaviour, IPlace
    {
        public static readonly HashSet<Outbound> Instances = new();
        
        [Tooltip("The options this can request for an outbound order.")]
        [SerializeField]
        private int[] options = { };

        [Tooltip("The maximum options that can be required for this order.")]
        [Min(1)]
        [SerializeField]
        private int max = 1;

        [Tooltip("The amount of time before a new order comes in.")]
        [Min(0)]
        [SerializeField]
        private float delay;
        
        private readonly Dictionary<int, int> _requirements = new();

        private float _elapsedTime;

        public bool Active => _requirements.Count > 0;

        public int[] Requirements() => _requirements.Select(x => x.Key).ToArray();

        public bool Requires(int id) => _requirements.ContainsKey(id);

        public bool Place(WarehouseAgent agent)
        {
            if (!agent.HasPart || !_requirements.ContainsKey(agent.Id))
            {
                return false;
            }

            Part part = agent.Remove();
            if (part == null)
            {
                return false;
            }

            _requirements[part.ID]--;
            if (_requirements[part.ID] < 1)
            {
                _requirements.Remove(part.ID);
            }
            
            Destroy(part.gameObject);
            WarehouseAgent.TargetModified(this);
            return true;
        }

        private void OnEnable()
        {
            Instances.Add(this);
        }

        private void OnDisable()
        {
            Instances.Remove(this);
        }

        private void Start()
        {
            CreateOrder();
        }

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
            
            WarehouseAgent.TargetModified(this);
        }
    }
}