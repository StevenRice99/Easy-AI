using EasyAI;
using Unity.Mathematics;
using UnityEngine;

namespace Warehouse
{
    /// <summary>
    /// Control the warehouse simulation.
    /// </summary>
    public class WarehouseManager : EasyManager
    {
        /// <summary>
        /// The prefab for the warehouse agent.
        /// </summary>
        [Tooltip("The prefab for the warehouse agent.")]
        [SerializeField]
        private WarehouseAgent warehouseAgentPrefab;

        /// <summary>
        /// Locations to spawn agents at.
        /// </summary>
        [Tooltip("Locations to spawn agents at.")]
        [SerializeField]
        private Transform[] spawnPoints;

        /// <summary>
        /// The number of workers for the warehouse.
        /// </summary>
        [Tooltip("The number of workers for the warehouse.")]
        [Range(1, 12)]
        [SerializeField]
        private int workers = 12;
        
        /// <summary>
        /// Keep track of the number of orders completed.
        /// </summary>
        private int _orderedCompleted;
        
        /// <summary>
        /// Indicate that an order has been completed.
        /// </summary>
        public static void OrderCompleted()
        {
            ((WarehouseManager)Singleton)._orderedCompleted++;
        }
        
        /// <summary>
        /// Display details about the warehouse.
        /// </summary>
        /// <param name="x">X rendering position. In most cases this should remain unchanged.</param>
        /// <param name="y">Y rendering position. Update this with every component added and return it.</param>
        /// <param name="w">Width of components. In most cases this should remain unchanged.</param>
        /// <param name="h">Height of components. In most cases this should remain unchanged.</param>
        /// <param name="p">Padding of components. In most cases this should remain unchanged.</param>
        /// <returns>The updated Y position after all custom rendering has been done.</returns>
        protected override float DisplayDetails(float x, float y, float w, float h, float p)
        {
            y = NextItem(y, h, p);
            GuiBox(x, y, w, h, p, 1);
            GuiLabel(x, y, w, h, p, $"Orders Completed: {_orderedCompleted}");
            return y;
        }
        
        /// <summary>
        /// Render buttons to change level details or reset the level.
        /// </summary>
        /// <param name="x">X rendering position. In most cases this should remain unchanged.</param>
        /// <param name="y">Y rendering position. Update this with every component added and return it.</param>
        /// <param name="w">Width of components. In most cases this should remain unchanged.</param>
        /// <param name="h">Height of components. In most cases this should remain unchanged.</param>
        /// <param name="p">Padding of components. In most cases this should remain unchanged.</param>
        /// <returns>The updated Y position after all custom rendering has been done.</returns>
        protected override float CustomRendering(float x, float y, float w, float h, float p)
        {
            if (GuiButton(x, y, w, h, "Reset"))
            {
                ResetLevel();
            }
            
            y = NextItem(y, h, p);

            if (workers > 1)
            {
                if (GuiButton(x, y, w, h, "Remove Worker"))
                {
                    workers--;
                    ResetLevel();
                }
                
                y = NextItem(y, h, p);
            }
            
            if (workers < 12)
            {
                if (GuiButton(x, y, w, h, "Add Worker"))
                {
                    workers++;
                    ResetLevel();
                }
                
                y = NextItem(y, h, p);
            }

            return y;
        }

        protected override void Start()
        {
            base.Start();

            ResetLevel();
        }

        /// <summary>
        /// Reset the level.
        /// </summary>
        private void ResetLevel()
        {
            WarehouseAgent[] current = FindObjectsByType<WarehouseAgent>(FindObjectsSortMode.None);
            for (int i = 0; i < current.Length; i++)
            {
                Destroy(current[i].gameObject);
            }
            
            foreach (Storage storage in Storage.Instances)
            {
                storage.ResetObject();
            }
            
            foreach (Inbound inbound in Inbound.Instances)
            {
                inbound.ResetObject();
            }

            foreach (Outbound outbound in Outbound.Instances)
            {
                outbound.ResetObject();
            }

            _orderedCompleted = 0;

            for (int i = 0; i < workers; i++)
            {
                WarehouseAgent agent = Instantiate(warehouseAgentPrefab, spawnPoints[i].position, quaternion.identity);
                agent.name = $"Worker {i + 1:D2}";
            }
        }
    }
}