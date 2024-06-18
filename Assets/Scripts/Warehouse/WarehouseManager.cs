using System;
using System.Collections.Generic;
using System.Linq;
using EasyAI;
using Unity.Mathematics;
using UnityEngine;
#if UNITY_EDITOR
using System.IO;
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
            /// <summary>
            /// The color to display for this part.
            /// </summary>
            [Tooltip("The color to display for this part.")]
            public Color color;
            
            /// <summary>
            /// The demand for ordering this part.
            /// </summary>
            [Tooltip("The demand for ordering this part.")]
            [Min(float.Epsilon)]
            public float demand;
        }
        
        /// <summary>
        /// How to manage the layout of the warehouse.
        /// </summary>
        private enum StorageLayout
        {
            Rows,
            NearInbound,
            NearOutbound
        }

        /// <summary>
        /// How to predict the next shipment order.
        /// </summary>
        private enum Forecast
        {
            Linear,
            Quadratic
        }
        
        /// <summary>
        /// Root of where to save data.
        /// </summary>
        private const string Root = "Warehouse";

        /// <summary>
        /// The folder to save trials to.
        /// </summary>
        private const string Trials = "Trials";

        /// <summary>
        /// The folder to save final results composed of the trials.
        /// </summary>
        private const string Results = "Results";

        /// <summary>
        /// The folder to save the statistics of the results.
        /// </summary>
        private const string Statistics = "Statistics";
        
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

        /// <summary>
        /// What percentage of storage units should start full.
        /// </summary>
        [Tooltip("What percentage of storage units should start full.")]
        [Range(0, 1)]
        [SerializeField]
        private float startStorage;

        /// <summary>
        /// Whether to use supply chain simulation.
        /// </summary>
        [Header("Scheduling")]
        [Tooltip("Whether to use supply chain simulation.")]
        [SerializeField]
        private bool supplyChain;
        
        /// <summary>
        /// The lead time for placed orders.
        /// </summary>
        [Tooltip("The lead time for placed orders.")]
        [Min(0)]
        [SerializeField]
        private int leadTime;

        /// <summary>
        /// How to forecast orders.
        /// </summary>
        [Tooltip("How to forecast orders.")]
        [SerializeField]
        private Forecast forecast;

        /// <summary>
        /// The order curves the warehouse can follow.
        /// </summary>
        [Tooltip("The order curves the warehouse can follow.")]
        [SerializeField]
        private OrderCurve[] orderCurves =
        {
            // Constant low.
            new() {orders = new Order[]
            {
                new() {requirements = new [] {10, 10, 10, 10, 10}},
                new() {requirements = new [] {10, 10, 10, 10, 10}},
                new() {requirements = new [] {10, 10, 10, 10, 10}},
                new() {requirements = new [] {10, 10, 10, 10, 10}},
                new() {requirements = new [] {10, 10, 10, 10, 10}},
                new() {requirements = new [] {10, 10, 10, 10, 10}},
                new() {requirements = new [] {10, 10, 10, 10, 10}},
                new() {requirements = new [] {10, 10, 10, 10, 10}},
                new() {requirements = new [] {10, 10, 10, 10, 10}},
                new() {requirements = new [] {10, 10, 10, 10, 10}}
            }},
            
            // Large peak.
            new() {orders = new Order[]
            {
                new() {requirements = new [] {20, 15, 20, 15, 10}},
                new() {requirements = new [] {15, 20, 20, 15, 15}},
                new() {requirements = new [] {25, 20, 20, 20, 25}},
                new() {requirements = new [] {35, 30, 30, 30, 35}},
                new() {requirements = new [] {55, 60, 50, 40, 45}},
                new() {requirements = new [] {70, 65, 60, 50, 55}},
                new() {requirements = new [] {75, 80, 70, 60, 65}},
                new() {requirements = new [] {65, 50, 60, 50, 55}},
                new() {requirements = new [] {50, 45, 50, 40, 50}},
                new() {requirements = new [] {40, 40, 40, 30, 45}}
            }},
            
            // Wave pattern.
            new() {orders = new Order[]
            {
                new() {requirements = new [] {20, 15, 10, 10, 5}},
                new() {requirements = new [] {25, 20, 10, 15, 15}},
                new() {requirements = new [] {30, 25, 20, 20, 25}},
                new() {requirements = new [] {35, 30, 25, 30, 25}},
                new() {requirements = new [] {30, 25, 20, 20, 25}},
                new() {requirements = new [] {25, 20, 10, 15, 15}},
                new() {requirements = new [] {20, 15, 10, 10, 5}},
                new() {requirements = new [] {25, 20, 10, 15, 15}},
                new() {requirements = new [] {30, 25, 20, 20, 25}},
                new() {requirements = new [] {35, 30, 25, 30, 25}}
            }},
        };

        /// <summary>
        /// The current curve to get demands off of.
        /// </summary>
        [Tooltip("The current curve to get demands off of.")]
        [Min(0)]
        [SerializeField]
        private int currentCurve;
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
        private int testTime = 600;

        /// <summary>
        /// How quickly to run the simulation for tests.
        /// </summary>
        [Tooltip("How quickly to run the simulation for tests.")]
        [Min(1)]
        [SerializeField]
        private float testSpeed = 2;

        /// <summary>
        /// The numbers of workers to test with.
        /// </summary>
        [Tooltip("The numbers of workers to test with.")]
        [SerializeField]
        private int[] workerCases = { 2, 4, 6, 8, 10, 12 };
#endif
        /// <summary>
        /// The prefab to use for parts.
        /// </summary>
        public static Part PartPrefab => WarehouseSingleton.partPrefab;

        /// <summary>
        /// The part options for the warehouse.
        /// </summary>
        public static PartInfo[] Parts => WarehouseSingleton.parts;
        
        /// <summary>
        /// The minimum and maximum order size that can be required for an order.
        /// </summary>
        public static int2 OrderSize => WarehouseSingleton.orderSize;

        /// <summary>
        /// Whether roles should be used for worker tasks or not.
        /// </summary>
        public static bool Roles => WarehouseSingleton.roles;

        /// <summary>
        /// Whether agents can always get information about the warehouse wireless, or terminals have to be used.
        /// </summary>
        public static bool Wireless => WarehouseSingleton.wireless;
        
        /// <summary>
        /// The amount of time before a new inbound shipment comes in.
        /// </summary>
        public static float InboundDelay => WarehouseSingleton.inboundDelay;
        
        /// <summary>
        /// The amount of time before a new order comes in.
        /// </summary>
        public static float OutboundDelay => WarehouseSingleton.outboundDelay;
        
        /// <summary>
        /// How much does interacting take scaled with the Y position of this storage.
        /// </summary>
        /// <returns></returns>
        public static float InteractTimeScale => WarehouseSingleton.interactTimeScale;

        /// <summary>
        /// Whether to use supply chain simulation.
        /// </summary>
        public static bool SupplyChain => WarehouseSingleton.supplyChain;
        
        /// <summary>
        /// The current shipment being completed by the warehouse.
        /// </summary>
        public static Dictionary<int, int> CurrentShipment => WarehouseSingleton._currentShipment;

        /// <summary>
        /// The current order leaving the warehouse.
        /// </summary>
        public static Dictionary<int, int> CurrentOrder => WarehouseSingleton._currentOrder;
        
        /// <summary>
        /// The singleton cast to a warehouse manager.
        /// </summary>
        private static WarehouseManager WarehouseSingleton => (WarehouseManager) Singleton;
        
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

        /// <summary>
        /// Shipments requested next for the warehouse.
        /// </summary>
        private readonly Queue<Dictionary<int, int>> _shipments = new();

        /// <summary>
        /// The current shipment being brought to the warehouse.
        /// </summary>
        private Dictionary<int, int> _currentShipment = new();

        /// <summary>
        /// The current order leaving the warehouse.
        /// </summary>
        private Dictionary<int, int> _currentOrder = new();

        /// <summary>
        /// The previous orders the warehouse has completed.
        /// </summary>
        private readonly List<Dictionary<int, int>> _previousOrders = new();

        /// <summary>
        /// How many time periods have passed to start the initial ordering.
        /// </summary>
        private int _periodsElapsed;

        /// <summary>
        /// Flag the next period just started so the simulation has a chance.
        /// </summary>
        private bool _nextPeriod;
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
        private int[] _testStoragesUsed;
#endif
        /// <summary>
        /// Indicate that an order has been completed.
        /// </summary>
        public static void OrderCompleted()
        {
            WarehouseSingleton._ordersCompleted++;
        }

        /// <summary>
        /// Get the next time period to process.
        /// </summary>
        public void GetNextTimePeriod()
        {
            _nextPeriod = true;

            foreach (Inbound inbound in Inbound.Instances)
            {
                inbound.ResetObject();
            }

            foreach (Outbound outbound in Outbound.Instances)
            {
                outbound.ResetObject();
            }
            
            if (_periodsElapsed < leadTime || _shipments.Count < 1)
            {
                _currentShipment = new();
            }
            else
            {
                _currentShipment = _shipments.Dequeue();
            }

            _currentOrder = new();
            Dictionary<int, int> copyForPrevious = new();
            
            if (currentCurve >= orderCurves.Length || orderCurves[currentCurve].orders.Length <= _periodsElapsed)
            {
                return;
            }

            int totalItems = 0;
            int[] requirements = orderCurves[currentCurve].orders[_periodsElapsed++].requirements;
            for (int i = 0; i < requirements.Length && i < parts.Length; i++)
            {
                _currentOrder.Add(i, requirements[i]);
                copyForPrevious.Add(i, requirements[i]);
                totalItems += requirements[i];
            }

            Outbound[] outbounds = Outbound.Instances.ToArray();
            int capacity = totalItems / outbounds.Length;
            if (totalItems % outbounds.Length != 0)
            {
                capacity++;
            }

            orderSize = new(capacity, capacity);
            _previousOrders.Add(copyForPrevious);

            Dictionary<int, int> requested = new();
            for (int i = 0; i < parts.Length; i++)
            {
                int[] past = new int[_previousOrders.Count + leadTime];
                for (int j = 0; j < past.Length; j++)
                {
                    if (j < _previousOrders.Count)
                    {
                        if (_previousOrders[j].ContainsKey(i))
                        {
                            past[j] = _previousOrders[j][i];
                        }
                        else
                        {
                            past[j] = 0;
                        }
                        continue;
                    }

                    int[] sub = new int[j];
                    for (int k = 0; k < sub.Length; k++)
                    {
                        sub[k] = past[k];
                    }
                    
                    switch (forecast)
                    {
                        case Forecast.Quadratic:
                            past[j] = PredictQuadratic(sub);
                            break;
                        case Forecast.Linear:
                        default:
                            past[j] = PredictQuadratic(sub);
                            break;
                    }
                }

                switch (forecast)
                {
                    case Forecast.Quadratic:
                        requested.Add(i, PredictQuadratic(past));
                        break;
                    case Forecast.Linear:
                    default:
                        requested.Add(i, PredictLinear(past));
                        break;
                }
            }
            
            _shipments.Enqueue(requested);
        }

        private static int PredictLinear(int[] trend)
        {
            double slope = CalculateSlope(trend);
            double intercept = CalculateIntercept(trend, slope);

            int nextIndex = trend.Length;
            int nextNumber = (int) (slope * nextIndex + intercept);
            return nextNumber < 0 ? 0 : nextNumber;
        }

        private static double CalculateSlope(int[] numbers)
        {
            int n = numbers.Length;
            double sumX = 0;
            double sumY = numbers.Sum();
            double sumXY = 0;
            double sumX2 = 0;

            for (int i = 0; i < n; i++)
            {
                sumX += i;
                sumXY += i * numbers[i];
                sumX2 += i * i;
            }

            return (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
        }

        private static double CalculateIntercept(int[] numbers, double slope)
        {
            int n = numbers.Length;
            double sumX = 0;
            double sumY = numbers.Sum();

            for (int i = 0; i < n; i++)
            {
                sumX += i;
            }

            return (sumY - slope * sumX) / n;
        }

        private static int PredictQuadratic(int[] trend)
        {
            (double a, double b, double c) = CalculateQuadraticCoefficients(trend);

            int nextIndex = trend.Length;
            int nextNumber = (int) (a * nextIndex * nextIndex + b * nextIndex + c);
            return nextNumber < 0 ? 0 : nextNumber;
        }
        
        private static (double a, double b, double c) CalculateQuadraticCoefficients(int[] numbers)
        {
            int n = numbers.Length;

            double sumX = 0, sumY = numbers.Sum();
            double sumX2 = 0, sumX3 = 0, sumX4 = 0;
            double sumXY = 0, sumX2Y = 0;

            for (int i = 0; i < n; i++)
            {
                double x = i;
                double y = numbers[i];

                sumX += x;
                sumX2 += x * x;
                sumX3 += x * x * x;
                sumX4 += x * x * x * x;
                sumXY += x * y;
                sumX2Y += x * x * y;
            }

            // Form the augmented matrix
            double[,] matrix = {
                { n, sumX, sumX2, sumY },
                { sumX, sumX2, sumX3, sumXY },
                { sumX2, sumX3, sumX4, sumX2Y }
            };

            // Perform Gaussian elimination
            for (int i = 0; i < 3; i++)
            {
                for (int k = i + 1; k < 3; k++)
                {
                    double t = matrix[k, i] / matrix[i, i];
                    for (int j = 0; j <= 3; j++)
                    {
                        matrix[k, j] -= t * matrix[i, j];
                    }
                }
            }

            // Back substitution
            double a = matrix[2, 3] / matrix[2, 2];
            double b = (matrix[1, 3] - matrix[1, 2] * a) / matrix[1, 1];
            double c = (matrix[0, 3] - matrix[0, 2] * a - matrix[0, 1] * b) / matrix[0, 0];

            return (a, b, c);
        }

        /// <summary>
        /// Indicate that a shipment has been unloaded.
        /// </summary>
        public static void ShipmentsUnloaded()
        {
            WarehouseSingleton._shipmentsUnloaded++;
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
            
            // One more item to display if more than one agent, as there cannot be roles with one agent.
            int size = 7;
            if (workers > 1)
            {
                size++;
            }
#if UNITY_EDITOR
            // Need an extra line in the editor to indicate if automatic tests are being run.
            if (run)
            {
                size++;
            }
#endif
            GuiBox(x, y, w, h, p, size);

            // Get how much time has elapsed.
            double seconds = Time.timeAsDouble - _startTime;
            int intSeconds = (int) seconds;
#if UNITY_EDITOR
            // If running tests or not.
            string remaining;
            if (run)
            {
                GuiLabel(x, y, w, h, p, "Running Tests");
                y = NextItem(y, h, p);
                
                // Get how much time is left accounting for time scaling.
                double difference = (_totalTime - _runsComplete * testTime - seconds) / testSpeed;
                int minutesDifference = (int) difference / 60;
                remaining = minutesDifference >= 60 ? $" | Remaining: {minutesDifference / 60}:{minutesDifference % 60:D2}:{(int) difference % 60:D2}" : $" | Remaining: {(int) difference / 60}:{(int) difference % 60:D2}";
            }
            else
            {
                remaining = string.Empty;
            }
            
            GuiLabel(x, y, w, h, p, $"Time: {intSeconds / 60}:{intSeconds % 60:D2}{remaining}");
#else
            GuiLabel(x, y, w, h, p, $"Time: {intSeconds / 60}:{intSeconds % 60:D2}");
#endif      
            // Display the number of workers.
            y = NextItem(y, h, p);
            GuiLabel(x, y, w, h, p, $"Workers: {workers}");

            // If more than one worker, display if using roles or not.
            if (workers > 1)
            {
                y = NextItem(y, h, p);
                GuiLabel(x, y, w, h, p, roles ? "Roles" : "No Roles");
            }
            
            // Display if accessing information wireless or via terminals.
            y = NextItem(y, h, p);
            GuiLabel(x, y, w, h, p, wireless ? "Wireless Information" : "Terminals Information");

            // Display the layout mode.
            y = NextItem(y, h, p);
            GuiLabel(x, y, w, h, p, $"Storage Layout: {LayoutString(layout)}");

            // Calculate rates for order and shipment handling.
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
            
            // Display the orders complete.
            y = NextItem(y, h, p);
            GuiLabel(x, y, w, h, p, $"Orders Completed: {_ordersCompleted} | {orderRate:0.00} / minute");
            
            // Display the shipments unloaded.
            y = NextItem(y, h, p);
            GuiLabel(x, y, w, h, p, $"Shipments Unloaded: {_shipmentsUnloaded} | {shipmentRate:0.00} / minute");

            // Get how many storages are being used.
            int storagesUsed = Storage.Instances.Count(i => !i.Empty);
            
            // Display the storage utilization.
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
            // Don't display controls if running tests.
            if (run)
            {
                return y;
            }
#endif
            // A button to reset the level.
            if (GuiButton(x, y, w, h, "Reset"))
            {
                ResetLevel();
            }
            
            y = NextItem(y, h, p);
            
            // A button to add a worker.
            if (GuiButton(x, y, w, h, "Add Worker"))
            {
                workers++;
                ResetLevel();
            }
                
            y = NextItem(y, h, p);

            // Can only remove workers or change roles if there are more than one worker.
            if (workers > 1)
            {
                // A button to remove a worker.
                if (GuiButton(x, y, w, h, "Remove Worker"))
                {
                    workers--;
                    ResetLevel();
                }
                
                y = NextItem(y, h, p);
                
                // A button to toggle roles.
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
            
            // A button to toggle using wireless.
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
            
            // A button to toggle the layout.
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
            GetNextTimePeriod();
#if UNITY_EDITOR
            // Configure the trials if running them.
            if (run)
            {
                // Run at the desired rate.
                Time.timeScale = testSpeed;
                Time.fixedDeltaTime /= testSpeed;
                
                // Ensure worker cases are distinct.
                workerCases = workerCases.Distinct().Where(x => x > 0).OrderBy(x => x).ToArray();
                if (workerCases.Length == 0)
                {
                    workerCases = new[] {1};
                }
                
                // Determine how much time all cases will take.
                _totalTime = 12 * workerCases.Length * testTime;
                
                // If the first case has only one worker, certain cases will not be run so account for that.
                if (workerCases.Length > 0 && workerCases[0] < 2)
                {
                    _totalTime -= 6 * testTime;
                }

                // Set initial values.
                _currentWorkersCase = 0;
                workers = workerCases[_currentWorkersCase];
                roles = false;
                wireless = false;
                layout = StorageLayout.Rows;

                // If all trials are already done, there is nothing to do.
                if (TrialExists(workers, layout, wireless, roles) && !NextTrial())
                {
                    return;
                }
            }
#endif
            ResetLevel();
        }

        /// <summary>
        /// Reset the level.
        /// </summary>
        private void ResetLevel()
        {
            // Reset supply chain values.
            _periodsElapsed = 0;
            _previousOrders.Clear();
            _shipments.Clear();
            _nextPeriod = false;
            
            // If only one worker, there cannot be roles.
            if (workers < 2)
            {
                roles = false;
            }
            
            // Destroy all current agents.
            WarehouseAgent[] agents = WarehouseAgent.Instances.ToArray();
            for (int i = 0; i < agents.Length; i++)
            {
                Destroy(agents[i].gameObject);
            }

            // Reset details.
            _ordersCompleted = 0;
            _shipmentsUnloaded = 0;
            _startTime = Time.timeAsDouble;

            // Set the layout.
            switch (layout)
            {
                case StorageLayout.NearInbound:
                    // Most in-demand parts are towards the inbounds.
                    ConfigureStorageNearest(Inbound.Instances.Select(x => x.transform).ToArray());
                    break;
                case StorageLayout.NearOutbound:
                    // Most in-demand parts are towards the outbounds.
                    ConfigureStorageNearest(Outbound.Instances.Select(x => x.transform).ToArray());
                    break;
                case StorageLayout.Rows:
                default:
                    // Assign racks by demand, with most in-demand parts being towards the center.
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
            
            // Reset all storages options.
            Storage.PickOptions.Clear();
            Storage.PlaceOptions.Clear();
            foreach (Storage storage in Storage.Instances)
            {
                storage.ResetObject();
            }
            
            // Reset all inbounds.
            foreach (Inbound inbound in Inbound.Instances)
            {
                inbound.ResetObject();
            }

            // Reset all outbounds.
            foreach (Outbound outbound in Outbound.Instances)
            {
                outbound.ResetObject();
            }

            // Reset all info stations.
            foreach (InfoStation infoStation in InfoStation.Instances)
            {
                infoStation.ResetObject();
            }

            // Create initial storage inventory if needed.
            if (startStorage > 0)
            {
                for (int i = 0; i < parts.Length; i++)
                {
                    Storage[] storages = Storage.Instances.Where(x => x.ID == i).OrderBy(x => x.transform.position.y).ThenBy(x => x.transform.position.x).ThenBy(x => x.transform.position.z).ToArray();
                    int fill = (int) (storages.Length * startStorage);
                    for (int j = 0; j < fill; j++)
                    {
                        Part part = Instantiate(partPrefab, storages[j].transform.position, Quaternion.identity);
                        part.SetId(i);
                        storages[j].Set(part);
                    }
                }
            }

            // Track what location to spawn agents at.
            int inboundLocation = 0;
            int outboundLocation = 0;

            // Spawn the required number of workers.
            for (int i = 0; i < workers; i++)
            {
                // If not using roles or every other agent, spawn them at an inbound.
                bool inbound = !roles || i % 2 == 0;
                
                // Get the spawn position needed.
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
                
                // Spawn the agent.
                WarehouseAgent agent = Instantiate(warehouseAgentPrefab, spawnPosition, quaternion.identity);
                string role = roles ? inbound ? " - Inbound" : " - Outbound" : string.Empty;
                agent.name = $"Worker {i + 1:D2}{role}";
                agent.Inbound = inbound;
            }
#if UNITY_EDITOR
            // If doing automatic runs, set up the variables to hold the data.
            if (!run)
            {
                return;
            }

            int size = testTime + 1;
            _testOrdersComplete = new int[size];
            _testShipmentsUnloaded = new int[size];
            _testStoragesUsed = new int[size];
            for (int i = 0; i < size; i++)
            {
                _testOrdersComplete[i] = 0;
                _testShipmentsUnloaded[i] = 0;
                _testStoragesUsed[i] = 0;
            }
#endif
        }

        /// <summary>
        /// Configure the storage layout to be relative to either the inbounds or outbounds.
        /// </summary>
        /// <param name="close">The transforms to be close to.</param>
        private void ConfigureStorageNearest(Transform[] close)
        {
            // Get the point to order storages by.
            Vector3 p = close.Aggregate(Vector3.zero, (current, t) => current + t.position) / close.Length;

            // Order by distance, then height, then offset.
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
            // Ensure parts are in order.
            if (parts is { Length: > 0 })
            {
                parts = parts.OrderByDescending(x => x.demand).ToArray();
            }
            
            // Ensure order sizes are valid.
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

        /// <summary>
        /// Frame-rate independent MonoBehaviour. FixedUpdate message for physics calculations.
        /// </summary>
        private void FixedUpdate()
        {
            if (supplyChain)
            {
                if (_nextPeriod)
                {
                    _nextPeriod = false;
                }
                else
                {
                    bool reset = WarehouseAgent.Instances.All(agent => !agent.NeedsInfo && !agent.Moving && !agent.HasTarget);
                    if (reset)
                    {
                        // TODO - Save what orders failed and shipments were lost
                        GetNextTimePeriod();
                    }
                }
            }
            
#if UNITY_EDITOR
            // Nothing to do if not running trials.
            if (!run)
            {
                return;
            }

            // If there are no agents, this means the trials are done so exit.
            if (WarehouseAgent.Instances.Count < 1)
            {
                EditorApplication.ExitPlaymode();
                return;
            }
            
            // Get the elapsed time.
            double seconds = Time.timeAsDouble - _startTime;
            int intSeconds = (int) seconds;

            // Store the results at the current time step.
            _testOrdersComplete[intSeconds] = _ordersCompleted;
            _testShipmentsUnloaded[intSeconds] = _shipmentsUnloaded;
            int storagesUsed = Storage.Instances.Count(i => !i.Empty);
            if (storagesUsed > _testStoragesUsed[intSeconds])
            {
                _testStoragesUsed[intSeconds] = storagesUsed;
            }

            // If this test still has time, return.
            if (intSeconds < testTime)
            {
                return;
            }

            // Otherwise, save data by ensuring the folder exists.
            if (!Directory.Exists(Root))
            {
                Directory.CreateDirectory(Root);
            }

            if (Directory.Exists(Root))
            {
                string trials = $"{Root}/{Trials}";
                if (!Directory.Exists(trials))
                {
                    Directory.CreateDirectory(trials);
                }

                if (Directory.Exists(trials))
                {
                    string title = DetermineFileTrial(workers, layout, wireless, roles);
                    string data = "Seconds,Orders Completed,Shipments Unloaded,Storages Used";
                    for (int i = 0; i < _testOrdersComplete.Length; i++)
                    {
                        data += $"\n{i},{_testOrdersComplete[i]},{_testShipmentsUnloaded[i]},{_testStoragesUsed[i]}";
                    }
                    StreamWriter writer = new($"{trials}/{title}.csv", false);
                    writer.Write(data);
                    writer.Close();
                }
            }

            // Go to the next trial.
            NextTrial();
#endif
        }
#if UNITY_EDITOR
        /// <summary>
        /// Go to the next trial.
        /// </summary>
        /// <returns>True if the next trial was configured, false if all trials are done.</returns>
        private bool NextTrial()
        {
            // Up the number of runs that have been completed.
            _runsComplete++;

            // Test the layout options.
            for (StorageLayout next = layout + 1; next <= StorageLayout.NearOutbound; next++)
            {
                if (TrialExists(workers, next, wireless, roles))
                {
                    continue;
                }

                layout = next;
                ResetLevel();
                return true;
            }

            layout = StorageLayout.Rows;
            
            // If not wireless communication, try switching to wireless.
            if (!wireless && !TrialExists(workers, layout, true, roles))
            {
                wireless = true;
                ResetLevel();
                return true;
            }

            wireless = false;
            
            // If more than one worker...
            if (workers > 1)
            {
                // If not using roles, try switching to roles.
                if (!roles && !TrialExists(workers, layout, wireless, true))
                {
                    roles = true;
                    ResetLevel();
                    return true;
                }

                roles = false;
            }
            // Always no roles when only one agent.
            else
            {
                roles = false;
            }
            
            // Check all worker options.
            for (int i = _currentWorkersCase + 1; i < workerCases.Length; i++)
            {
                if (TrialExists(workerCases[i], layout, wireless, roles))
                {
                    continue;
                }

                _currentWorkersCase = i;
                workers = workerCases[_currentWorkersCase];
                ResetLevel();
                return true;
            }
            
            // All trials are done so aggregate the data.
            SaveData(StorageLayout.Rows, false, false, out double[] outRowsTerminalsNo, out double[] inRowsTerminalsNo, out double[] storeRowsTerminalsNo);
            SaveData(StorageLayout.Rows, false, true, out double[] outRowsTerminalsYes, out double[] inRowsTerminalsYes, out double[] storeRowsTerminalsYes);
            SaveData(StorageLayout.Rows, true, false, out double[] outRowsWirelessNo, out double[] inRowsWirelessNo, out double[] storeRowsWirelessNo);
            SaveData(StorageLayout.Rows, true, true, out double[] outRowsWirelessYes, out double[] inRowsWirelessYes, out double[] storeRowsWirelessYes);
            SaveData(StorageLayout.NearInbound, false, false, out double[] outInboundTerminalsNo, out double[] inInboundTerminalsNo, out double[] storeInboundTerminalsNo);
            SaveData(StorageLayout.NearInbound, false, true, out double[] outInboundTerminalsYes, out double[] inInboundTerminalsYes, out double[] storeInboundTerminalsYes);
            SaveData(StorageLayout.NearInbound, true, false, out double[] outInboundWirelessNo, out double[] inInboundWirelessNo, out double[] storeInboundWirelessNo);
            SaveData(StorageLayout.NearInbound, true, true, out double[] outInboundWirelessYes, out double[] inInboundWirelessYes, out double[] storeInboundWirelessYes);
            SaveData(StorageLayout.NearOutbound, false, false, out double[] outOutboundTerminalsNo, out double[] inOutboundTerminalsNo, out double[] storeOutboundTerminalsNo);
            SaveData(StorageLayout.NearOutbound, false, true, out double[] outOutboundTerminalsYes, out double[] inOutboundTerminalsYes, out double[] storeOutboundTerminalsYes);
            SaveData(StorageLayout.NearOutbound, true, false, out double[] outOutboundWirelessNo, out double[] inOutboundWirelessNo, out double[] storeOutboundWirelessNo);
            SaveData(StorageLayout.NearOutbound, true, true, out double[] outOutboundWirelessYes, out double[] inOutboundWirelessYes, out double[] storeOutboundWirelessYes);

            // Save the statistics for the data.
            string path = $"{Root}/{Statistics}";

            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            if (!Directory.Exists(path))
            {
                EditorApplication.ExitPlaymode();
                return false;
            }
            
            string header = workerCases.Aggregate("Layout,Wireless/Terminals,Roles/No Roles", (current, workerCase) => current + $",{workerCase}");

            string outbounds = header;
            string inbounds = header;
            string storages = header;

            const string rowsTerminalsNo = "\nRows,Terminals,No Roles";
            outbounds += rowsTerminalsNo;
            inbounds += rowsTerminalsNo;
            storages += rowsTerminalsNo;
            for (int i = 0; i < workerCases.Length; i++)
            {
                outbounds += $",{outRowsTerminalsNo[i]}";
                inbounds += $",{inRowsTerminalsNo[i]}";
                storages += $",{storeRowsTerminalsNo[i]}";
            }
            
            const string rowsTerminalsYes = "\nRows,Terminals,Roles";
            outbounds += rowsTerminalsYes;
            inbounds += rowsTerminalsYes;
            storages += rowsTerminalsYes;
            for (int i = 0; i < workerCases.Length; i++)
            {
                outbounds += $",{outRowsTerminalsYes[i]}";
                inbounds += $",{inRowsTerminalsYes[i]}";
                storages += $",{storeRowsTerminalsYes[i]}";
            }

            const string rowsWirelessNo = "\nRows,Wireless,No Roles";
            outbounds += rowsWirelessNo;
            inbounds += rowsWirelessNo;
            storages += rowsWirelessNo;
            for (int i = 0; i < workerCases.Length; i++)
            {
                outbounds += $",{outRowsWirelessNo[i]}";
                inbounds += $",{inRowsWirelessNo[i]}";
                storages += $",{storeRowsWirelessNo[i]}";
            }

            const string rowsWirelessYes = "\nRows,Wireless,Roles";
            outbounds += rowsWirelessYes;
            inbounds += rowsWirelessYes;
            storages += rowsWirelessYes;
            for (int i = 0; i < workerCases.Length; i++)
            {
                outbounds += $",{outRowsWirelessYes[i]}";
                inbounds += $",{inRowsWirelessYes[i]}";
                storages += $",{storeRowsWirelessYes[i]}";
            }

            const string inboundTerminalsNo = "\nNear Inbound,Terminals,No Roles";
            outbounds += inboundTerminalsNo;
            inbounds += inboundTerminalsNo;
            storages += inboundTerminalsNo;
            for (int i = 0; i < workerCases.Length; i++)
            {
                outbounds += $",{outInboundTerminalsNo[i]}";
                inbounds += $",{inInboundTerminalsNo[i]}";
                storages += $",{storeInboundTerminalsNo[i]}";
            }
            
            const string inboundTerminalsYes = "\nNear Inbound,Terminals,Roles";
            outbounds += inboundTerminalsYes;
            inbounds += inboundTerminalsYes;
            storages += inboundTerminalsYes;
            for (int i = 0; i < workerCases.Length; i++)
            {
                outbounds += $",{outInboundTerminalsYes[i]}";
                inbounds += $",{inInboundTerminalsYes[i]}";
                storages += $",{storeInboundTerminalsYes[i]}";
            }

            const string inboundWirelessNo = "\nNear Inbound,Wireless,No Roles";
            outbounds += inboundWirelessNo;
            inbounds += inboundWirelessNo;
            storages += inboundWirelessNo;
            for (int i = 0; i < workerCases.Length; i++)
            {
                outbounds += $",{outInboundWirelessNo[i]}";
                inbounds += $",{inInboundWirelessNo[i]}";
                storages += $",{storeInboundWirelessNo[i]}";
            }

            const string inboundWirelessYes = "\nNear Inbound,Wireless,Roles";
            outbounds += inboundWirelessYes;
            inbounds += inboundWirelessYes;
            storages += inboundWirelessYes;
            for (int i = 0; i < workerCases.Length; i++)
            {
                outbounds += $",{outInboundWirelessYes[i]}";
                inbounds += $",{inInboundWirelessYes[i]}";
                storages += $",{storeInboundWirelessYes[i]}";
            }

            const string outboundTerminalsNo = "\nNear Outbound,Terminals,No Roles";
            outbounds += outboundTerminalsNo;
            inbounds += outboundTerminalsNo;
            storages += outboundTerminalsNo;
            for (int i = 0; i < workerCases.Length; i++)
            {
                outbounds += $",{outOutboundTerminalsNo[i]}";
                inbounds += $",{inOutboundTerminalsNo[i]}";
                storages += $",{storeOutboundTerminalsNo[i]}";
            }
            
            const string outboundTerminalsYes = "\nNear Outbound,Terminals,Roles";
            outbounds += outboundTerminalsYes;
            inbounds += outboundTerminalsYes;
            storages += outboundTerminalsYes;
            for (int i = 0; i < workerCases.Length; i++)
            {
                outbounds += $",{outOutboundTerminalsYes[i]}";
                inbounds += $",{inOutboundTerminalsYes[i]}";
                storages += $",{storeOutboundTerminalsYes[i]}";
            }

            const string outboundWirelessNo = "\nNear Outbound,Wireless,No Roles";
            outbounds += outboundWirelessNo;
            inbounds += outboundWirelessNo;
            storages += outboundWirelessNo;
            for (int i = 0; i < workerCases.Length; i++)
            {
                outbounds += $",{outOutboundWirelessNo[i]}";
                inbounds += $",{inOutboundWirelessNo[i]}";
                storages += $",{storeOutboundWirelessNo[i]}";
            }

            const string outboundWirelessYes = "\nNear Outbound,Wireless,Roles";
            outbounds += outboundWirelessYes;
            inbounds += outboundWirelessYes;
            storages += outboundWirelessYes;
            for (int i = 0; i < workerCases.Length; i++)
            {
                outbounds += $",{outOutboundWirelessYes[i]}";
                inbounds += $",{inOutboundWirelessYes[i]}";
                storages += $",{storeOutboundWirelessYes[i]}";
            }
            
            StreamWriter writer = new($"{path}/Orders Completed.csv", false);
            writer.Write(outbounds);
            writer.Close();
            
            writer = new($"{path}/Shipments Unloaded.csv", false);
            writer.Write(inbounds);
            writer.Close();
            
            writer = new($"{path}/Storages Used.csv", false);
            writer.Write(storages);
            writer.Close();

            EditorApplication.ExitPlaymode();
            return false;
        }

        /// <summary>
        /// Check if a trial for a particular configuration is done.
        /// </summary>
        /// <param name="workerCount">The number of workers.</param>
        /// <param name="currentLayout">The layout.</param>
        /// <param name="usingWireless">If using wireless.</param>
        /// <param name="usingRoles">If using roles.</param>
        /// <returns>True if the file for this trial exists, false otherwise.</returns>
        private static bool TrialExists(int workerCount, StorageLayout currentLayout, bool usingWireless, bool usingRoles)
        {
            if (!Directory.Exists(Root))
            {
                return false;
            }

            string trials = $"{Root}/{Trials}";
            if (!Directory.Exists(trials))
            {
                return false;
            }
            
            string title = DetermineFileTrial(workerCount, currentLayout, usingWireless, usingRoles);
            return File.Exists($"{trials}/{title}.csv");
        }

        private void SaveData(StorageLayout currentLayout, bool usingWireless, bool usingRoles, out double[] orders, out double[] shipments, out double[] averageStorage)
        {
            // Store data for all worker numbers.
            orders = new double[workerCases.Length];
            shipments = new double[workerCases.Length];
            averageStorage = new double[workerCases.Length];
            for (int i = 0; i < workerCases.Length; i++)
            {
                orders[i] = 0;
                shipments[i] = 0;
                averageStorage[i] = 0;
            }
            
            // Nothing to do if there are no trials.
            if (!Directory.Exists(Root) || !Directory.Exists($"{Root}/{Trials}"))
            {
                return;
            }

            // Create the results' folder.
            string path = $"{Root}/{Results}";
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }

            if (!Directory.Exists(path))
            {
                return;
            }
            
            // Read trials data.
            List<string>[] outboundData = new List<string>[workerCases.Length];
            List<string>[] inboundData = new List<string>[workerCases.Length];
            List<string>[] storageData = new List<string>[workerCases.Length];
            
            // The length of the trials.
            int len = 0;
            
            // Try for every worker configuration.
            for (int i = 0; i < workerCases.Length; i++)
            {
                outboundData[i] = new();
                inboundData[i] = new();
                storageData[i] = new();
                
                // If the file does not exist then do nothing.
                if (!TrialExists(workerCases[i], currentLayout, usingWireless, usingRoles))
                {
                    continue;
                }
                
                int runLen = 0;

                // Load the file.
                using StreamReader sr = new($"{Root}/{Trials}/{DetermineFileTrial(workerCases[i], currentLayout, usingWireless, usingRoles)}.csv");
                bool isHeader = true;
                
                // Read the entire file.
                while (!sr.EndOfStream)
                {
                    // Read the line.
                    string line = sr.ReadLine();
                    
                    // Skip the header.
                    if (line == null || isHeader)
                    {
                        isHeader = false;
                        continue;
                    }

                    // Parse the CSV.
                    string[] values = line.Split(',');
                    
                    if (values.Length < 2)
                    {
                        continue;
                    }

                    runLen++;
                    
                    outboundData[i].Add(values[1]);
                    if (values.Length < 3)
                    {
                        continue;
                    }
                        
                    inboundData[i].Add(values[2]);
                    if (values.Length < 4)
                    {
                        continue;
                    }
                        
                    storageData[i].Add(values[3]);
                }

                if (runLen > len)
                {
                    len = runLen;
                }
            }

            // Construct the header for the CSV files.
            string header = workerCases.Aggregate("Seconds", (current, workerCase) => current + $",{workerCase}");

            // Set the header to start each file.
            string outbounds = header;
            string inbounds = header;
            string storages = header;
            
            // Store previous values in case of errors.
            int[] previousOutbound = new int[workerCases.Length];
            int[] previousInbound = new int[workerCases.Length];
            int[] previousStorage = new int[workerCases.Length];
            for (int i = 0; i < workerCases.Length; i++)
            {
                previousOutbound[i] = 0;
                previousInbound[i] = 0;
                previousStorage[i] = 0;
            }
            
            // Loop for the longest time.
            for (int i = 0; i < len; i++)
            {
                // Shift to new lines for each file and add in the second this is for.
                outbounds += $"\n{i}";
                inbounds += $"\n{i}";
                storages += $"\n{i}";
                
                // Loop for all worker numbers and parse in data..
                for (int j = 0; j < workerCases.Length; j++)
                {
                    if (i < outboundData[j].Count)
                    {
                        outbounds += $",{outboundData[j][i]}";
                        if (int.TryParse(outboundData[j][i], out int c))
                        {
                            previousOutbound[j] = c;
                            orders[j] = c;
                        }
                    }
                    else
                    {
                        outbounds += $",{previousOutbound[j]}";
                    }
                    
                    if (i < inboundData[j].Count)
                    {
                        inbounds += $",{inboundData[j][i]}";
                        if (int.TryParse(inboundData[j][i], out int c))
                        {
                            previousInbound[j] = c;
                            shipments[j] = c;
                        }
                    }
                    else
                    {
                        inbounds += $",{previousInbound[j]}";
                    }
                    
                    if (i < storageData[j].Count)
                    {
                        storages += $",{storageData[j][i]}";
                        if (int.TryParse(storageData[j][i], out int c))
                        {
                            previousStorage[j] = c;
                            averageStorage[j] += c;
                        }
                    }
                    else
                    {
                        storages += $",{previousStorage[j]}";
                        averageStorage[j] += previousStorage[j];
                    }
                }
            }

            // Convert storages to averages.
            for (int i = 0; i < workerCases.Length; i++)
            {
                averageStorage[i] /= len;
            }

            // Get the core of the title for each file and then save them.
            string title = DetermineFileCore(currentLayout, usingWireless, usingRoles);
            
            StreamWriter writer = new($"{path}/Orders Completed {title}.csv", false);
            writer.Write(outbounds);
            writer.Close();
            
            writer = new($"{path}/Shipments Unloaded {title}.csv", false);
            writer.Write(inbounds);
            writer.Close();
            
            writer = new($"{path}/Storages Used {title}.csv", false);
            writer.Write(storages);
            writer.Close();
        }

        /// <summary>
        /// Determine the file name for a trial.
        /// </summary>
        /// <param name="workerCount">The number of workers.</param>
        /// <param name="currentLayout">The layout.</param>
        /// <param name="usingWireless">If using wireless.</param>
        /// <param name="usingRoles">If using roles.</param>
        /// <returns>The name for this trial file.</returns>
        private static string DetermineFileTrial(int workerCount, StorageLayout currentLayout, bool usingWireless, bool usingRoles)
        {
            return $"{workerCount} {DetermineFileCore(currentLayout, usingWireless, usingRoles)}";
        }

        /// <summary>
        /// Determine the core of the naming for files without the worker number.
        /// </summary>
        /// <param name="currentLayout">The layout.</param>
        /// <param name="usingWireless">If using wireless.</param>
        /// <param name="usingRoles">If using roles.</param>
        /// <returns>The core of the name for this file.</returns>
        private static string DetermineFileCore(StorageLayout currentLayout, bool usingWireless, bool usingRoles)
        {
            string w = usingWireless ? "Wireless" : "Terminals";
            string r = usingRoles ? "Roles" : "No Roles";
            return $"{LayoutString(currentLayout)} {w} {r}";
        }
#endif
    }
}