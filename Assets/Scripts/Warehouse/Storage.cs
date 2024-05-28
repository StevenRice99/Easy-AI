using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Warehouse
{
    [DisallowMultipleComponent]
    public class Storage : MonoBehaviour, IPick, IPlace
    {
        public static readonly HashSet<Storage> Instances = new();
        
        [Tooltip("The types of parts that can be stored here.")]
        public int[] ids = { };
        
        private Part _part;

        public bool Empty => _part == null;

        public bool Has(int id) => !Empty && _part.ID == id;

        public bool CanTake(int id) => Empty && ids.Contains(id);

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

            _part = part;
            _part.transform.parent = transform;
            _part.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            WarehouseAgent.WarehouseUpdated();
            return true;
        }

        public bool Pick(WarehouseAgent agent)
        {
            if (Empty || agent.HasPart || (agent.Id >= 0 && _part.ID != agent.Id))
            {
                return false;
            }

            _part.transform.parent = agent.HoldLocation;
            _part.transform.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            _part = null;
            WarehouseAgent.WarehouseUpdated();
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
    }
}