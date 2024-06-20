using System.Collections.Generic;
using System.Linq;
using EasyAI;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Warehouse
{
    /// <summary>
    /// Outbound order from the warehouse.
    /// </summary>
    [DisallowMultipleComponent]
    public class Outbound : MonoBehaviour, IPlace, IReset
    {
        /// <summary>
        /// All outbound instances.
        /// </summary>
        public static readonly HashSet<Outbound> Instances = new();
        
        /// <summary>
        /// All requirements for the current order.
        /// </summary>
        private readonly Dictionary<int, int> _requirements = new();

        /// <summary>
        /// The requirements which have yet to be claimed by a worker to complete.
        /// </summary>
        private readonly Dictionary<int, int> _available = new();

        /// <summary>
        /// How much time has passed since the previous order was completed.
        /// </summary>
        private float _elapsedTime;

        /// <summary>
        /// If this order is currently active.
        /// </summary>
        public bool Active => _available.Count > 0;

        /// <summary>
        /// Get all requirement types.
        /// </summary>
        /// <returns>The required types.</returns>
        public int[] AllAvailable() => _available.Select(x => x.Key).ToArray();

        /// <summary>
        /// Check if this can take a part with an ID.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <param name="id">The ID to check for.</param>
        /// <returns>True if it can take a part with the ID, false otherwise.</returns>
        public bool PlaceAvailable(WarehouseAgent agent, int id) => _available.ContainsKey(id);

        /// <summary>
        /// Claim an ID for an agent.
        /// </summary>
        /// <param name="agent">The agent.</param>
        /// <param name="id"></param>
        /// <returns>True if it can be claimed, false otherwise.</returns>
        public bool PlaceClaim(WarehouseAgent agent, int id)
        {
            // Ensure this is available for the agent.
            if (!PlaceAvailable(agent, id))
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
        /// Place a part at this location.
        /// </summary>
        /// <param name="agent">The agent placing the part.</param>
        /// <returns>True if the part was added, false otherwise.</returns>
        public bool Place(WarehouseAgent agent)
        {
            // If the agent does not have a part or this does not require this part do nothing.
            if (!agent.HasPart || !_requirements.ContainsKey(agent.Id))
            {
                return false;
            }
            
            // Destroy the part the agent was carrying to indicate it was used for this order.
            int id = agent.Destroy();
            
            // Remove this from the requirements.
            if (id >= 0)
            {
                _requirements[id]--;
                if (_requirements[id] < 1)
                {
                    _requirements.Remove(id);
                }
            }

            // If the order has been completed, add the score for it.
            if (_requirements.Count < 1)
            {
                WarehouseManager.OrderCompleted();
            }
            
            // Agent's have individual score which is incremented.
            agent.AddOrderScore();
            return true;
        }

        /// <summary>
        /// Get the time it would take to place a part at this location.
        /// </summary>
        /// <param name="position">The position the placer is currently at.</param>
        /// <param name="speed">How fast the placer can move.</param>
        /// <returns>The time it would take to place a part at this location.</returns>
        public float PlaceTime(Vector3 position, float speed)
        {
            return EasyManager.PathLength(EasyManager.LookupPath(position, transform.position), position) / speed;
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
            // Still need items so do nothing.
            if (_requirements.Count > 0)
            {
                return;
            }

            // Order completed so wait before requesting a new one.
            _elapsedTime += Time.deltaTime;
            if (_elapsedTime >= WarehouseManager.OutboundDelay)
            {
                CreateOrder();
            }
        }

        /// <summary>
        /// Create a new order.
        /// </summary>
        private void CreateOrder()
        {
            // Cleanup the previous order.
            _requirements.Clear();
            _available.Clear();

            // Get all parts the warehouse can spawn.
            WarehouseManager.PartInfo[] options = WarehouseManager.Parts;
            
            // Sum the total weight of all parts for random chance calculations.
            float sum = options.Sum(option => option.demand);
            
            // Determine the size of this order.
            int2 orderSize = WarehouseManager.OrderSize;
            int number = Random.Range(orderSize.x, orderSize.y + 1);
            
            // Loop for the size of the order.
            for (int i = 0; i < number; i++)
            {
                // Get the random value to determine what part is needed.
                float rand = Random.Range(0, sum);
                
                // Start with the first part.
                int option = 0;
                float current = options[0].demand;
                
                // Loop until the part which meets the weight threshold is met.
                while (current < rand)
                {
                    current += options[++option].demand;
                }
                
                // Store the new requirement.
                if (!_requirements.TryAdd(option, 1))
                {
                    _requirements[option]++;
                    _available[option]++;
                }
                else
                {
                    _available.Add(option, 1);
                }
            }

            // Reset the timer.
            _elapsedTime = 0;
        }

        /// <summary>
        /// Reset this object.
        /// </summary>
        public void ResetObject()
        {
            _requirements.Clear();
            _available.Clear();
            _elapsedTime = WarehouseManager.OutboundDelay;
        }
    }
}