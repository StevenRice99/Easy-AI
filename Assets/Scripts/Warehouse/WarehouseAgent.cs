using EasyAI;
using UnityEngine;

namespace Warehouse
{
    public class WarehouseAgent : EasyTransformAgent
    {
        [field: Tooltip("How far the agent can pick or place a part.")]
        [field: Min(float.Epsilon)]
        [field: SerializeField]
        public float InteractDistance { get; private set; } = 2;
        
        [field: Tooltip("Where to hold an item.")]
        [field: SerializeField]
        public Transform HoldLocation { get; private set; }

        public bool HasPart
        {
            get
            {
                if (_part != null)
                {
                    return true;
                }

                if (HoldLocation.childCount < 1)
                {
                    return false;
                }

                _part = HoldLocation.GetChild(0).GetComponent<Part>();
                return _part != null;
            }
        }

        public MonoBehaviour Target { get; private set; }

        public bool HasTarget => Target != null;
        
        public bool CanInteract => HasTarget && Vector2.Distance(new(transform.position.x, transform.position.z), new(Target.transform.position.x, Target.transform.position.z)) <= InteractDistance;

        public int Id { get; private set; } = -1;

        private Part _part;

        public static void WarehouseUpdated()
        {
            foreach (EasyAgent agent in EasyManager.CurrentAgents)
            {
                if (agent is not WarehouseAgent w)
                {
                    continue;
                }

                w.SetTarget();
            }
        }

        public void SetTarget(MonoBehaviour target = null)
        {
            if (target == null)
            {
                Log("No target, stopping moving.");
                Target = null;
                StopMoving();
                return;
            }

            Vector3 pos;
            
            if (HasPart)
            {
                if (target is not IPlace)
                {
                    Log("Cannot go to a picking only position with a part.");
                    Target = null;
                    StopMoving();
                    return;
                }

                Target = target;
                pos = Target.transform.position;
                Move(new Vector2(pos.x, pos.z));
                Log($"Moving to {Target.name}");
                return;
            }

            if (target is not IPick)
            {
                Log("Cannot go to a placing only position without a part.");
                Target = null;
                StopMoving();
                return;
            }

            Target = target;
            pos = Target.transform.position;
            Move(new Vector2(pos.x, pos.z));
            Log($"Moving to {Target.name}");
        }

        public void SetId(int id)
        {
            if (!HasPart)
            {
                Id = id;
            }
        }

        public bool Attach(Part part)
        {
            SetTarget();
            
            if (HasPart)
            {
                return false;
            }

            _part = part;
            Transform t = _part.transform;
            t.parent = HoldLocation;
            t.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            Id = part.ID;
            return true;
        }

        public Part Remove()
        {
            SetTarget();
            
            if (!HasPart)
            {
                return null;
            }

            Part part = _part;
            part.transform.parent = null;
            _part = null;
            Id = -1;
            return part;
        }

        public int Destroy()
        {
            SetTarget();
            
            if (!HasPart)
            {
                return -1;
            }
            
            int id = Id;
            Destroy(_part.gameObject);
            _part = null;
            Id = -1;
            return id;
        }
    }
}