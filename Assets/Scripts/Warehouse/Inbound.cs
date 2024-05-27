using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Warehouse
{
    [DisallowMultipleComponent]
    public class Inbound : MonoBehaviour, IPick
    {
        public static readonly HashSet<Inbound> Instances = new();
        
        [Tooltip("The potential parts to spawn.")]
        [SerializeField]
        private Part[] prefabs;

        [Tooltip("The spaces to spawn parts at.")]
        [SerializeField]
        private Transform[] locations;

        [Tooltip("The amount of time before a new inbound shipment comes in.")]
        [Min(0)]
        [SerializeField]
        private float delay;

        private readonly List<Part> _parts = new();

        private float _elapsedTime;

        public bool Has(int id) => _parts.Any(x => x.ID == id);

        public bool Pick(WarehouseAgent agent)
        {
            if (_parts.Count < 1)
            {
                return false;
            }

            Part part = agent.Id < 0 ? _parts[0] : _parts.FirstOrDefault(x => x.ID == agent.Id);
            if (part == null || !agent.Attach(part))
            {
                return false;
            }
            
            _parts.Remove(part);
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
            SpawnParts();
        }

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
            WarehouseAgent.TargetModified(this);
        }
    }
}