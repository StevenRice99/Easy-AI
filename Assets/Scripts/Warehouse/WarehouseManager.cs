using System.Linq;
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
        [Header("Warehouse Layout")]
        [Tooltip("The prefab for the warehouse agent.")]
        [SerializeField]
        private WarehouseAgent warehouseAgentPrefab;

        /// <summary>
        /// The location to spawn inbound workers at.
        /// </summary>
        [Tooltip("The location to spawn inbound workers at.")]
        [SerializeField]
        private Transform inboundSpawn;

        /// <summary>
        /// The location to spawn outbound workers at.
        /// </summary>
        [Tooltip("The location to spawn outbound workers at.")]
        [SerializeField]
        private Transform outboundSpawn;

        /// <summary>
        /// The number of workers for the warehouse.
        /// </summary>
        [Header("Parameters")]
        [Tooltip("The number of workers for the warehouse.")]
        [Range(1, 12)]
        [SerializeField]
        private int workers = 12;

        /// <summary>
        /// Whether roles should be used for worker tasks or not.
        /// </summary>
        [field: Tooltip("Whether roles should be used for worker tasks or not.")]
        [field: SerializeField]
        public bool Roles { get; private set; }
        
        /// <summary>
        /// Keep track of the number of orders completed.
        /// </summary>
        private int _orderedCompleted;

        /// <summary>
        /// Keep track of the number of shipments unloaded.
        /// </summary>
        private int _shipmentsUnloaded;

        /// <summary>
        /// The time which has passed since the simulation began.
        /// </summary>
        private double _startTime;
        
        /// <summary>
        /// Indicate that an order has been completed.
        /// </summary>
        public static void OrderCompleted()
        {
            ((WarehouseManager)Singleton)._orderedCompleted++;
        }

        /// <summary>
        /// Indicate that a shipment has been unloaded.
        /// </summary>
        public static void ShipmentsUnloaded()
        {
            ((WarehouseManager)Singleton)._shipmentsUnloaded++;
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
            int size = 5;
            if (workers > 1)
            {
                size++;
            }
            
            GuiBox(x, y, w, h, p, size);

            double seconds = Time.timeAsDouble - _startTime;
            int intSeconds = (int) seconds;
            GuiLabel(x, y, w, h, p, $"Time: {intSeconds / 60}:{intSeconds % 60:D2}");
            
            y = NextItem(y, h, p);
            GuiLabel(x, y, w, h, p, $"Workers: {workers}");

            if (workers > 1)
            {
                y = NextItem(y, h, p);
                GuiLabel(x, y, w, h, p, Roles ? "Roles" : "No Roles");
            }

            double minutes = seconds / 60;
            double orderRate;
            double shipmentRate;
            if (minutes == 0)
            {
                orderRate = 0;
                shipmentRate = 0;
            }
            else
            {
                orderRate = _orderedCompleted / minutes;
                shipmentRate = _shipmentsUnloaded / minutes;
            }
            
            y = NextItem(y, h, p);
            GuiLabel(x, y, w, h, p, $"Orders Completed: {_orderedCompleted} | {orderRate:0.00} / minute");
            
            y = NextItem(y, h, p);
            GuiLabel(x, y, w, h, p, $"Shipments Unloaded: {_shipmentsUnloaded} | {shipmentRate:0.00} / minute");

            int storagesUsed = Storage.Instances.Count(i => !i.Empty);
            
            y = NextItem(y, h, p);
            GuiLabel(x, y, w, h, p, $"Storage Utilization: {storagesUsed} / {Storage.Instances.Count} | {(float) storagesUsed / Storage.Instances.Count:0.00}%");
            
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
                    if (workers == 1)
                    {
                        Roles = false;
                    }
                    
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

            if (workers > 1)
            {
                if (GuiButton(x, y, w, h, Roles ? "Disable Roles" : "Enable Roles"))
                {
                    Roles = !Roles;
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

            _startTime = Time.timeAsDouble;
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
            _shipmentsUnloaded = 0;
            _startTime = Time.timeAsDouble;

            for (int i = 0; i < workers; i++)
            {
                Vector3 spawnPosition = !Roles || i % 2 == 0 ? inboundSpawn.position : outboundSpawn.position;
                WarehouseAgent agent = Instantiate(warehouseAgentPrefab, spawnPosition, quaternion.identity);
                agent.name = $"Worker {i + 1:D2}";
            }
        }
    }
}