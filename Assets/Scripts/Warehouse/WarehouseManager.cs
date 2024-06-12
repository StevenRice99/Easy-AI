using System;
using System.Linq;
using EasyAI;
using Unity.Mathematics;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Warehouse
{
    /// <summary>
    /// Control the warehouse simulation.
    /// </summary>
    public class WarehouseManager : EasyManager
    {
        /// <summary>
        /// The part options for the warehouse.
        /// </summary>
        [Serializable]
        public struct PartInfo
        {
            [Tooltip("The color to display for this part.")]
            public Color color;
            
            [Tooltip("The demand for ordering this part.")]
            [Min(float.Epsilon)]
            public float demand;
        }
        
        /// <summary>
        /// How to manage the layout of the warehouse.
        /// </summary>
        public enum StorageLayout
        {
            Rows,
            NearInbound,
            NearOutbound
        }
        
        /// <summary>
        /// The prefab for the warehouse agent.
        /// </summary>
        [Header("Warehouse Layout")]
        [Tooltip("The prefab for the warehouse agent.")]
        [SerializeField]
        private WarehouseAgent warehouseAgentPrefab;

        /// <summary>
        /// The prefab to use for parts.
        /// </summary>
        [Tooltip("The prefab to use for parts.")]
        [SerializeField]
        private Part partPrefab;

        /// <summary>
        /// The location to spawn inbound workers at.
        /// </summary>
        [Tooltip("The locations to spawn inbound workers at.")]
        [SerializeField]
        private Transform[] inboundSpawns;

        /// <summary>
        /// The location to spawn outbound workers at.
        /// </summary>
        [Tooltip("The locations to spawn outbound workers at.")]
        [SerializeField]
        private Transform[] outboundSpawns;

        /// <summary>
        /// The number of workers for the warehouse.
        /// </summary>
        [Header("Parameters")]
        [Tooltip("The number of workers for the warehouse.")]
        [Min(1)]
        [SerializeField]
        private int workers = 12;

        /// <summary>
        /// Whether roles should be used for worker tasks or not.
        /// </summary>
        [Tooltip("Whether roles should be used for worker tasks or not.")]
        [SerializeField]
        private bool roles;

        /// <summary>
        /// Whether agents can always get information about the warehouse wireless, or terminals have to be used.
        /// </summary>
        [Tooltip("Whether agents can always get information about the warehouse wireless, or terminals have to be used.")]
        [SerializeField]
        private bool wireless;

        /// <summary>
        /// How to manage the layout of the warehouse.
        /// </summary>
        [Tooltip("How to manage the layout of the warehouse.")]
        [SerializeField]
        private StorageLayout layout;

        /// <summary>
        /// The part options for the warehouse.
        /// </summary>
        [Tooltip("The part options for the warehouse.")]
        [SerializeField]
        private PartInfo[] parts;
        
        /// <summary>
        /// The minimum and maximum order size that can be required for an order.
        /// </summary>
        [Tooltip("The minimum and maximum order size that can be required for an order.")]
        [SerializeField]
        private int2 orderSize = new(3, 3);

        /// <summary>
        /// The amount of time before a new inbound shipment comes in.
        /// </summary>
        [Tooltip("The amount of time before a new inbound shipment comes in.")]
        [Min(0)]
        [SerializeField]
        private float inboundDelay = 3;

        /// <summary>
        /// The amount of time before a new order comes in.
        /// </summary>
        [Tooltip("The amount of time before a new order comes in.")]
        [Min(0)]
        [SerializeField]
        private float outboundDelay = 10;

        /// <summary>
        /// How much does interacting take scaled with the Y position of this storage.
        /// </summary>
        [Tooltip("How much does interacting take scaled with the Y position of this storage.")]
        [Min(0)]
        [SerializeField]
        private float interactTimeScale = 1;
#if UNITY_EDITOR
        /// <summary>
        /// Whether to run tests.
        /// </summary>
        [Header("Tests")]
        [Tooltip("Whether to run tests.")]
        [SerializeField]
        private bool run;

        /// <summary>
        /// The time to run tests in seconds.
        /// </summary>
        [Tooltip("The time to run tests in seconds.")]
        [Min(1)]
        [SerializeField]
        private int testTime = 300;

        /// <summary>
        /// The numbers of workers to test with.
        /// </summary>
        [Tooltip("The numbers of workers to test with.")]
        [SerializeField]
        private int[] workerCases = { 2, 4, 6, 8, 10 };
#endif
        /// <summary>
        /// The prefab to use for parts.
        /// </summary>
        public static Part PartPrefab => ((WarehouseManager)Singleton).partPrefab;

        /// <summary>
        /// The part options for the warehouse.
        /// </summary>
        public static PartInfo[] Parts => ((WarehouseManager)Singleton).parts;
        
        /// <summary>
        /// The minimum and maximum order size that can be required for an order.
        /// </summary>
        public static int2 OrderSize => ((WarehouseManager)Singleton).orderSize;

        /// <summary>
        /// Whether roles should be used for worker tasks or not.
        /// </summary>
        public static bool Roles => ((WarehouseManager)Singleton).roles;

        /// <summary>
        /// Whether agents can always get information about the warehouse wireless, or terminals have to be used.
        /// </summary>
        public static bool Wireless => ((WarehouseManager)Singleton).wireless;
        
        /// <summary>
        /// The amount of time before a new inbound shipment comes in.
        /// </summary>
        public static float InboundDelay => ((WarehouseManager)Singleton).inboundDelay;
        
        /// <summary>
        /// The amount of time before a new order comes in.
        /// </summary>
        public static float OutboundDelay => ((WarehouseManager)Singleton).outboundDelay;
        
        /// <summary>
        /// How much does interacting take scaled with the Y position of this storage.
        /// </summary>
        /// <returns></returns>
        public static float InteractTimeScale => ((WarehouseManager)Singleton).interactTimeScale;
        
        /// <summary>
        /// Keep track of the number of orders completed.
        /// </summary>
        private int _ordersCompleted;

        /// <summary>
        /// Keep track of the number of shipments unloaded.
        /// </summary>
        private int _shipmentsUnloaded;

        /// <summary>
        /// The time which has passed since the simulation began.
        /// </summary>
        private double _startTime;
#if UNITY_EDITOR
        /// <summary>
        /// The number of test runs which have been complete.
        /// </summary>
        private int _runsComplete;

        /// <summary>
        /// The case index for the current workers.
        /// </summary>
        private int _currentWorkersCase;

        /// <summary>
        /// The total time in seconds which all tests will take.
        /// </summary>
        private int _totalTime;

        /// <summary>
        /// The number of orders that have been complete at every second of this test.
        /// </summary>
        private int[] _testOrdersComplete;

        /// <summary>
        /// The number of shipments that have been unloaded at every second of this test.
        /// </summary>
        private int[] _testShipmentsUnloaded;

        /// <summary>
        /// The number of storages that are being used at this second of this test.
        /// </summary>
        private int[] _testStoragesUsage;
#endif
        /// <summary>
        /// Indicate that an order has been completed.
        /// </summary>
        public static void OrderCompleted()
        {
            ((WarehouseManager)Singleton)._ordersCompleted++;
        }

        /// <summary>
        /// Indicate that a shipment has been unloaded.
        /// </summary>
        public static void ShipmentsUnloaded()
        {
            ((WarehouseManager)Singleton)._shipmentsUnloaded++;
        }

        /// <summary>
        /// Get the layout as a string.
        /// </summary>
        private static string LayoutString(StorageLayout l)
        {
            switch (l)
            {
                case StorageLayout.NearInbound:
                    return "Near Inbound";
                case StorageLayout.NearOutbound:
                    return "Near Outbound";
                case StorageLayout.Rows:
                default:
                    return "Rows";
            }
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
            int size = 7;
            if (workers > 1)
            {
                size++;
            }
#if UNITY_EDITOR
            size++;
#endif
            GuiBox(x, y, w, h, p, size);

            double seconds = Time.timeAsDouble - _startTime;
            int intSeconds = (int) seconds;
#if UNITY_EDITOR
            GuiLabel(x, y, w, h, p, "Running Tests");
            y = NextItem(y, h, p);

            string remaining;
            if (run)
            {
                int difference = _totalTime - _runsComplete * testTime - intSeconds;
                int minutesDifference = difference / 60;
                if (minutesDifference >= 60)
                {
                    remaining = $" | Remaining: {minutesDifference / 60}:{minutesDifference % 60:D2}:{difference % 60:D2}";
                }
                else
                {
                    remaining = $" | Remaining: {difference / 60}:{difference % 60:D2}";
                }
            }
            else
            {
                remaining = string.Empty;
            }
            
            GuiLabel(x, y, w, h, p, $"Time: {intSeconds / 60}:{intSeconds % 60:D2}{remaining}");
#else
            GuiLabel(x, y, w, h, p, $"Time: {intSeconds / 60}:{intSeconds % 60:D2}");
#endif      
            y = NextItem(y, h, p);
            GuiLabel(x, y, w, h, p, $"Workers: {workers}");

            if (workers > 1)
            {
                y = NextItem(y, h, p);
                GuiLabel(x, y, w, h, p, roles ? "Roles" : "No Roles");
            }
                
            y = NextItem(y, h, p);
            GuiLabel(x, y, w, h, p, wireless ? "Wireless Information" : "Terminals Information");

            
            y = NextItem(y, h, p);
            GuiLabel(x, y, w, h, p, $"Storage Layout: {LayoutString(layout)}");

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
                orderRate = _ordersCompleted / minutes;
                shipmentRate = _shipmentsUnloaded / minutes;
            }
            
            y = NextItem(y, h, p);
            GuiLabel(x, y, w, h, p, $"Orders Completed: {_ordersCompleted} | {orderRate:0.00} / minute");
            
            y = NextItem(y, h, p);
            GuiLabel(x, y, w, h, p, $"Shipments Unloaded: {_shipmentsUnloaded} | {shipmentRate:0.00} / minute");

            int storagesUsed = Storage.Instances.Count(i => !i.Empty);
            
            y = NextItem(y, h, p);
            GuiLabel(x, y, w, h, p, $"Storage Utilization: {storagesUsed} / {Storage.Instances.Count} | {(float) storagesUsed / Storage.Instances.Count * 100:0.00}%");
            
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
#if UNITY_EDITOR
            if (run)
            {
                return y;
            }
#endif
            if (GuiButton(x, y, w, h, "Reset"))
            {
                ResetLevel();
            }
            
            y = NextItem(y, h, p);
            
            if (GuiButton(x, y, w, h, "Add Worker"))
            {
                workers++;
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
                
                if (GuiButton(x, y, w, h, roles ? "Disable Roles" : "Enable Roles"))
                {
                    roles = !roles;
                    ResetLevel();
                }
                
                y = NextItem(y, h, p);
            }
            else
            {
                roles = false;
            }
                
            if (GuiButton(x, y, w, h, wireless ? "Disable Wireless" : "Enable Wireless"))
            {
                wireless = !wireless;
                ResetLevel();
            }
                
            y = NextItem(y, h, p);

            StorageLayout option = layout + 1;
            if (option > StorageLayout.NearOutbound)
            {
                option = StorageLayout.Rows;
            }
                
            if (GuiButton(x, y, w, h, $"Use {LayoutString(option)} Layout"))
            {
                layout++;
                if (layout > StorageLayout.NearOutbound)
                {
                    layout = StorageLayout.Rows;
                }
                
                ResetLevel();
            }
                
            y = NextItem(y, h, p);

            return y;
        }

        /// <summary>
        /// Start is called on the frame when a script is enabled just before any of the Update methods are called the first time.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            
#if UNITY_EDITOR
            workerCases = workerCases.Distinct().Where(x => x > 0).OrderBy(x => x).ToArray();
            if (workerCases.Length == 0)
            {
                workerCases = new[] {1};
            }
            _totalTime = 12 * workerCases.Length * testTime;
            if (workerCases.Length > 0 && workerCases[0] < 2)
            {
                _totalTime -= 6 * testTime;
            }

            workers = workerCases[0];
            _currentWorkersCase = 0;
            roles = false;
            wireless = false;
            layout = StorageLayout.Rows;
#endif
            ResetLevel();

            _startTime = Time.timeAsDouble;
        }

        /// <summary>
        /// Reset the level.
        /// </summary>
        private void ResetLevel()
        {
            if (workers < 2)
            {
                roles = false;
            }
            
            WarehouseAgent[] agents = WarehouseAgent.Instances.ToArray();
            for (int i = 0; i < agents.Length; i++)
            {
                Destroy(agents[i].gameObject);
            }

            _ordersCompleted = 0;
            _shipmentsUnloaded = 0;
            _startTime = Time.timeAsDouble;

            switch (layout)
            {
                case StorageLayout.NearInbound:
                    ConfigureStorageNearest(Inbound.Instances.Select(x => x.transform).ToArray());
                    break;
                case StorageLayout.NearOutbound:
                    ConfigureStorageNearest(Outbound.Instances.Select(x => x.transform).ToArray());
                    break;
                case StorageLayout.Rows:
                default:
                    Rack[] ordered = Rack.Instances.OrderBy(x => x.transform.position.magnitude).ThenBy(x => x.transform.position.z).ToArray();
                    int id = 0;
                    foreach (Rack rack in ordered)
                    {
                        rack.SetId(id++);
                        if (id >= parts.Length)
                        {
                            id = 0;
                        }
                    }
                    break;
            }
            
            Storage.PickOptions.Clear();
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

            foreach (InfoStation infoStation in InfoStation.Instances)
            {
                infoStation.ResetObject();
            }

            int inboundLocation = 0;
            int outboundLocation = 0;

            for (int i = 0; i < workers; i++)
            {
                bool inbound = !roles || i % 2 == 0;
                
                Vector3 spawnPosition;
                
                if (inbound)
                {
                    spawnPosition = inboundSpawns[inboundLocation++].position;
                    if (inboundLocation >= inboundSpawns.Length)
                    {
                        inboundLocation = 0;
                    }
                }
                else
                {
                    spawnPosition = outboundSpawns[outboundLocation++].position;
                    if (outboundLocation >= outboundSpawns.Length)
                    {
                        outboundLocation = 0;
                    }
                }
                
                WarehouseAgent agent = Instantiate(warehouseAgentPrefab, spawnPosition, quaternion.identity);
                string role = roles ? inbound ? " - Inbound" : " - Outbound" : string.Empty;
                agent.name = $"Worker {i + 1:D2}{role}";
                agent.Inbound = inbound;
            }
#if UNITY_EDITOR
            if (!run)
            {
                return;
            }

            int size = testTime + 1;
            _testOrdersComplete = new int[size];
            _testShipmentsUnloaded = new int[size];
            _testStoragesUsage = new int[size];
            for (int i = 0; i < size; i++)
            {
                _testOrdersComplete[i] = 0;
                _testShipmentsUnloaded[i] = 0;
                _testStoragesUsage[i] = 0;
            }
            
            Debug.Log($"{_runsComplete} | {workers} | {roles} | {wireless} | {layout}");
#endif
        }

        /// <summary>
        /// Configure the storage layout to be relative to either the inbounds or outbounds.
        /// </summary>
        /// <param name="close">The transforms to be close to.</param>
        private void ConfigureStorageNearest(Transform[] close)
        {
            Vector3 p = close.Aggregate(Vector3.zero, (current, t) => current + t.position) / close.Length;

            Storage[] storages = Storage.Instances.OrderBy(x => math.abs(x.transform.position.x - p.x)).ThenBy(x => math.abs(x.transform.position.y - p.y)).ThenBy(x => math.abs(x.transform.position.z - p.z)).ToArray();

            int step = storages.Length / parts.Length;
            int count = 0;
            int id = 0;

            foreach (Storage storage in storages)
            {
                storage.SetId(id);
                count++;
                if (count < step)
                {
                    continue;
                }

                count = 0;
                id++;
            }
        }

        /// <summary>
        /// Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
        /// </summary>
        private void OnValidate()
        {
            if (parts is { Length: > 0 })
            {
                parts = parts.OrderByDescending(x => x.demand).ToArray();
            }
            
            if (orderSize.x < 1)
            {
                orderSize.x = 1;
            }

            if (orderSize.y < 1)
            {
                orderSize.y = 1;
            }

            if (orderSize.x > orderSize.y)
            {
                (orderSize.x, orderSize.y) = (orderSize.y, orderSize.x);
            }
        }
#if UNITY_EDITOR
        private void FixedUpdate()
        {
            if (!run)
            {
                return;
            }
            
            double seconds = Time.timeAsDouble - _startTime;
            int intSeconds = (int) seconds;

            _testOrdersComplete[intSeconds] = _ordersCompleted;
            _testShipmentsUnloaded[intSeconds] = _shipmentsUnloaded;
            
            int storagesUsed = Storage.Instances.Count(i => !i.Empty);
            if (storagesUsed > _testStoragesUsage[intSeconds])
            {
                _testStoragesUsage[intSeconds] = storagesUsed;
            }

            if (intSeconds < testTime)
            {
                return;
            }
            
            // TODO - Save data.
                
            _runsComplete++;
                
            if (layout < StorageLayout.NearOutbound)
            {
                layout++;
                ResetLevel();
                return;
            }

            layout = StorageLayout.Rows;
                
            if (workers > 1)
            {
                if (!wireless)
                {
                    wireless = true;
                    ResetLevel();
                    return;
                }

                wireless = false;

                if (!roles)
                {
                    roles = true;
                    ResetLevel();
                    return;
                }
            }

            _currentWorkersCase++;

            if (_currentWorkersCase >= workerCases.Length)
            {
                EditorApplication.isPlaying = false;
                return;
            }

            workers = workerCases[_currentWorkersCase];
            ResetLevel();
        }
#endif
    }
}