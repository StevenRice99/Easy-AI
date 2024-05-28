using EasyAI;
using UnityEngine;

namespace Warehouse
{
    /// <summary>
    /// Agents to work in a warehouse.
    /// </summary>
    [DisallowMultipleComponent]
    public class WarehouseAgent : EasyTransformAgent
    {
        /// <summary>
        /// The score to add an item to an order.
        /// </summary>
        private const int ScoreOrder = 2;

        /// <summary>
        /// The score to add an item to storage.
        /// </summary>
        private const int ScoreStore = 1;
        
        /// <summary>
        /// How far the agent can pick or place a part.
        /// </summary>
        [field: Tooltip("How far the agent can pick or place a part.")]
        [field: Min(float.Epsilon)]
        [field: SerializeField]
        public float InteractDistance { get; private set; } = 1.5f;
        
        /// <summary>
        /// Where to hold an item.
        /// </summary>
        [field: Tooltip("Where to hold an item.")]
        [field: SerializeField]
        public Transform HoldLocation { get; private set; }
        
        /// <summary>
        /// The score of this agent.
        /// </summary>
        public int Score { get; private set; }

        /// <summary>
        /// If the agent currently has a part.
        /// </summary>
        public bool HasPart
        {
            get
            {
                if (_part != null)
                {
                    return true;
                }

                // If part is null and there are no children, it should be false.
                if (HoldLocation.childCount < 1)
                {
                    return false;
                }

                // Otherwise, a child should have a part.
                _part = HoldLocation.GetChild(0).GetComponent<Part>();
                return _part != null;
            }
        }

        /// <summary>
        /// The target to pick or place to or from.
        /// </summary>
        public MonoBehaviour Target { get; private set; }

        /// <summary>
        /// If the agent has a target.
        /// </summary>
        public bool HasTarget => Target != null;
        
        /// <summary>
        /// If the agent can interact with its target.
        /// </summary>
        public bool CanInteract => HasTarget && Vector2.Distance(new(transform.position.x, transform.position.z), new(Target.transform.position.x, Target.transform.position.z)) <= InteractDistance;

        /// <summary>
        /// The ID the agent wants or is carrying.
        /// </summary>
        public int Id { get; private set; } = -1;

        /// <summary>
        /// The agent's part.
        /// </summary>
        private Part _part;

        /// <summary>
        /// The warehouse has been updated.
        /// </summary>
        /// <param name="target">The target that has changed.</param>
        public static void WarehouseUpdated(MonoBehaviour target)
        {
            foreach (EasyAgent agent in EasyManager.CurrentAgents)
            {
                if (agent is not WarehouseAgent w)
                {
                    continue;
                }

                // If this agent was in relation to this target or has no specific goal, remove target to force it to find a new one.
                if (w.Id < 0 || w.Target == target)
                {
                    w.SetTarget();
                }
            }
        }

        /// <summary>
        /// Set the target for the agent.
        /// </summary>
        /// <param name="target">The target to pick up from or place down at.</param>
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

        /// <summary>
        /// Set the ID the agent wants to pick up.
        /// </summary>
        /// <param name="id">The ID to pick up.</param>
        public void SetId(int id)
        {
            // If we have a part, the ID is locked to that.
            if (!HasPart)
            {
                Id = id;
            }
        }

        /// <summary>
        /// Attach a part to this agent.
        /// </summary>
        /// <param name="part">The part to attach.</param>
        /// <returns>True if it was attached successfully, false otherwise.</returns>
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

        /// <summary>
        /// Remove the part from the agent.
        /// </summary>
        /// <returns>The part that was removed or null if there was none.</returns>
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

        /// <summary>
        /// Destroy the part the agent was carrying.
        /// </summary>
        /// <returns>The ID of the part the agent was carrying or -1 if it had no part.</returns>
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

        /// <summary>
        /// Add the score for adding a part to an order.
        /// </summary>
        public void AddOrderScore()
        {
            Score += ScoreOrder;
        }

        /// <summary>
        /// Add the score for placing a part in storage.
        /// </summary>
        public void AddStoreScore()
        {
            Score += ScoreStore;
        }
    }
}