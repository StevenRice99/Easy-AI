using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using EasyAI.Actuators;
using EasyAI.Agents;
using EasyAI.Cameras;
using EasyAI.Navigation;
using EasyAI.Navigation.Nodes;
using EasyAI.Thinking;
using EasyAI.Utility;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using Sensor = EasyAI.Sensors.Sensor;

namespace EasyAI.Managers
{
    /// <summary>
    /// Singleton to handle agents and GUI rendering. Must be exactly one of this or an extension of this present in every scene.
    /// </summary>
    [DisallowMultipleComponent]
    public class Manager : MonoBehaviour
    {
        /// <summary>
        /// Class to hold data for each node during A* pathfinding.
        /// </summary>
        private class AStarNode
        {
            /// <summary>
            /// The position of the node.
            /// </summary>
            public readonly Vector3 Position;

            /// <summary>
            /// The heuristic cost of this node to the goal.
            /// </summary>
            public float CostH { get; }

            /// <summary>
            /// The final cost of this node.
            /// </summary>
            public float CostF => CostG + CostH;

            /// <summary>
            /// The previous node which was moved to prior to this node.
            /// </summary>
            public AStarNode Previous { get; private set; }
        
            /// <summary>
            /// If this node is currently open or closed.
            /// </summary>
            public bool IsOpen { get; private set; }

            /// <summary>
            /// The cost to reach this node from previous nodes.
            /// </summary>
            private float CostG { get; set; }

            /// <summary>
            /// Store node data during A* pathfinding.
            /// </summary>
            /// <param name="pos">The position of the node.</param>
            /// <param name="goal">The goal to find a path to.</param>
            /// <param name="previous">The previous node in the A* pathfinding.</param>
            public AStarNode(Vector3 pos, Vector3 goal, AStarNode previous = null)
            {
                Open();
                Position = pos;
                CostH = Vector3.Distance(Position, goal);
                UpdatePrevious(previous);
            }

            /// <summary>
            /// Update the node to have a new previous node and then update its G cost.
            /// </summary>
            /// <param name="previous">The previous node in the A* pathfinding.</param>
            public void UpdatePrevious(AStarNode previous)
            {
                Previous = previous;
                if (Previous == null)
                {
                    CostG = 0;
                    return;
                }

                CostG = previous.CostG + Vector3.Distance(Position, Previous.Position);
            }

            /// <summary>
            /// Open the node.
            /// </summary>
            public void Open()
            {
                IsOpen = true;
            }

            /// <summary>
            /// Close the node.
            /// </summary>
            public void Close()
            {
                IsOpen = false;
            }
        }
    
        /// <summary>
        /// Hold a connection between two nodes.
        /// </summary>
        public struct Connection
        {
            /// <summary>
            /// A node in the connection.
            /// </summary>
            public readonly Vector3 A;
        
            /// <summary>
            /// A node in the connection.
            /// </summary>
            public readonly Vector3 B;

            /// <summary>
            /// Add a connection for two nodes.
            /// </summary>
            /// <param name="a">The first node.</param>
            /// <param name="b">The second node.</param>
            public Connection(Vector3 a, Vector3 b)
            {
                A = a;
                B = b;
            }
        }
    
        /// <summary>
        /// Hold data for the navigation lookup table.
        /// </summary>
        private struct NavigationLookup
        {
            /// <summary>
            /// The current or starting node.
            /// </summary>
            public readonly Vector3 Current;
        
            /// <summary>
            /// Where the end goal of the navigation is.
            /// </summary>
            public readonly Vector3 Goal;
        
            /// <summary>
            /// The node to move to from the current node in order to navigate towards the goal.
            /// </summary>
            public readonly Vector3 Next;

            /// <summary>
            /// Create a data entry for a navigation lookup table.
            /// </summary>
            /// <param name="current">The current or starting node.</param>
            /// <param name="goal">Where the end goal of the navigation is.</param>
            /// <param name="next">The node to move to from the current node in order to navigate towards the goal.</param>
            public NavigationLookup(Vector3 current, Vector3 goal, Vector3 next)
            {
                Current = current;
                Goal = goal;
                Next = next;
            }
        }
    
        /// <summary>
        /// Determine what mode messages are stored in.
        /// All - All messages are captured.
        /// Compact - All messages are captured, but, duplicate messages that appear immediately after each other will be merged into only a single instance of the message.
        /// Unique - No messages will be duplicated with the prior instance of the message being removed from its list when an identical message is added again.
        /// </summary>
        public enum MessagingMode : byte
        {
            All,
            Compact,
            Unique
        }

        /// <summary>
        /// Determine what navigation lines are drawn.
        /// Off - No lines are drawn.
        /// All - Every line for every connection is drawn.
        /// Active - Only lines for connections being used by agents are drawn.
        /// Selected - Only lines for the connections being used by the selected agent are drawn.
        /// </summary>
        public enum PathState : byte
        {
            Off,
            All,
            Active,
            Selected
        }
    
        /// <summary>
        /// What GUI State to display.
        /// Main - Displays a list of all agents and global messages. Never in this state if there is only one agent in the scene.
        /// Agent - Displays the selected agent. Displayed in place of "Main" if there is only one agent in the scene.
        /// Components - Displays lists of the sensors, actuators, percepts, and actions of the selected agent.
        /// Component - Displays details of a selected sensor or actuator.
        /// </summary>
        private enum GuiState : byte
        {
            Main,
            Agent,
            Components,
            Component
        }

        /// <summary>
        /// Determine what gizmos lines are drawn.
        /// Off - No lines are drawn.
        /// All - Every line from every agent, sensor, and actuator is drawn.
        /// Selected - If an agent is selected, only it and its sensors and actuators are drawn. If an individual sensor or actuator is selected, only it is drawn.
        /// </summary>
        private enum GizmosState : byte
        {
            Off,
            All,
            Selected
        }

        /// <summary>
        /// The folder to output navigation lookup tables into.
        /// </summary>
        private const string Folder = "Navigation";
        
        /// <summary>
        /// The width of the GUI buttons to open their respective menus when they are closed.
        /// </summary>
        private const float ClosedSize = 70;

        /// <summary>
        /// How much to visually offset navigation by so it does not clip into the ground.
        /// </summary>
        public const float NavigationVisualOffset = 0.1f;

        /// <summary>
        /// The mind or global state agents are in
        /// </summary>
        public static State Mind => Singleton.mind;

        /// <summary>
        /// How close an agent can be to a location its seeking or pursuing to declare it as reached.
        /// </summary>
        public static float SeekAcceptableDistance => Singleton.seekAcceptableDistance;

        /// <summary>
        /// How far an agent can be to a location its fleeing or evading from to declare it as reached.
        /// </summary>
        public static float FleeAcceptableDistance => Singleton.fleeAcceptableDistance;

        /// <summary>
        /// If an agent is not moving, ensure it comes to a complete stop when its velocity is less than this.
        /// </summary>
        public static float RestVelocity => Singleton.restVelocity;

        /// <summary>
        /// The height to draw agent looking visuals looking from.
        /// </summary>
        public static float SightHeight => Singleton.sightHeight;

        /// <summary>
        /// Which layers can nodes be placed on.
        /// </summary>
        public static LayerMask GroundLayers => Singleton.groundLayers;

        /// <summary>
        /// Which layers are obstacles that nodes cannot be placed on.
        /// </summary>
        public static LayerMask ObstacleLayers => Singleton.obstacleLayers;

        /// <summary>
        /// How far nodes can connect between with zero meaning no limit.
        /// </summary>
        public static float NodeDistance
        {
            get => Singleton.nodeDistance;
            set => Singleton.nodeDistance = value;
        }

        /// <summary>
        /// How wide is the agent radius for connecting nodes to ensure enough space for movement.
        /// </summary>
        public static float NavigationRadius => Singleton.navigationRadius;

        /// <summary>
        /// The maximum number of messages any component can hold.
        /// </summary>
        public static int MaxMessages => Singleton.maxMessages;

        /// <summary>
        /// The currently selected agent.
        /// </summary>
        public static Agent CurrentlySelectedAgent => Singleton.SelectedAgent;

        /// <summary>
        /// Determine what mode messages are stored in.
        /// </summary>
        public static MessagingMode MessageMode => Singleton._messageMode;

        /// <summary>
        /// The currently selected camera.
        /// </summary>
        public static Camera SelectedCamera => Singleton.selectedCamera;

        /// <summary>
        /// All agents in the scene.
        /// </summary>
        public static List<Agent> CurrentAgents => Singleton.Agents;

        /// <summary>
        /// List of all navigation nodes.
        /// </summary>
        public static List<Vector3> Nodes => Singleton._nodes;

        /// <summary>
        /// List of all navigation connections.
        /// </summary>
        public static List<Connection> Connections => Singleton._connections;

        /// <summary>
        /// All agents in the scene.
        /// </summary>
        public List<Agent> Agents { get; private set; } = new();

        /// <summary>
        /// The singleton agent manager.
        /// </summary>
        protected static Manager Singleton;
    
        /// <summary>
        /// All registered states.
        /// </summary>
        private static readonly Dictionary<Type, State> RegisteredStates = new();

        /// <summary>
        /// Cached shader value for use with line rendering.
        /// </summary>
        private static readonly int SrcBlend = Shader.PropertyToID("_SrcBlend");

        /// <summary>
        /// Cached shader value for use with line rendering.
        /// </summary>
        private static readonly int DstBlend = Shader.PropertyToID("_DstBlend");

        /// <summary>
        /// Cached shader value for use with line rendering.
        /// </summary>
        private static readonly int Cull = Shader.PropertyToID("_Cull");

        /// <summary>
        /// Cached shader value for use with line rendering.
        /// </summary>
        private static readonly int ZWrite = Shader.PropertyToID("_ZWrite");

        /// <summary>
        /// If the scene is currently playing or not.
        /// </summary>
        private static bool Playing => !Singleton._stepping && Time.timeScale > 0;

        /// <summary>
        /// The auto-generated material for displaying lines.
        /// </summary>
        private static Material _lineMaterial;

        /// <summary>
        /// List of all navigation connections.
        /// </summary>
        private readonly List<Connection> _connections = new();
    
        /// <summary>
        /// List of all navigation nodes.
        /// </summary>
        private readonly List<Vector3> _nodes = new();

        /// <summary>
        /// The currently selected agent.
        /// </summary>
        protected Agent SelectedAgent;

        [Header("Agents")]
        [Tooltip("The mind or global state agents are in. Initialize it with the global state to start it. If left empty the agent will have manual right-click-to-move controls.")]
        [SerializeField]
        private State mind;
        
        [Tooltip("How close an agent can be to a location its seeking or pursuing to declare it as reached. Set negative for none.")]
        [SerializeField]
        private float seekAcceptableDistance = 0.1f;

        [Tooltip("How far an agent can be to a location its fleeing or evading from to declare it as reached. Set negative for none.")]
        [SerializeField]
        private float fleeAcceptableDistance = 10f;

        [Tooltip("If an agent is not moving, ensure it comes to a complete stop when its velocity is less than this.")]
        [Min(0)]
        [SerializeField]
        private float restVelocity = 0.1f;

        [Tooltip("The radius of agents. This is for connecting navigation nodes to ensure enough space for movement.")]
        [Min(0)]
        [SerializeField]
        private float navigationRadius = 0.5f;
        
        [Tooltip("The height to draw agent looking visuals looking from.")]
        [Min(0)]
        [SerializeField]
        private float sightHeight;
        
        [Header("Navigation")]
        [Tooltip("Which layers can nodes be placed on.")]
        [SerializeField]
        private LayerMask groundLayers;

        [Tooltip("Which layers are obstacles that nodes cannot be placed on.")]
        [SerializeField]
        private LayerMask obstacleLayers;

        [Tooltip("How far nodes can connect between with zero meaning no limit.")]
        [Min(0)]
        [SerializeField]
        private float nodeDistance;

        [Tooltip(
            "How much height difference can there be between string pulls, set to zero for no limit.\n" +
            "Increase this value if generated paths are being generated between too high of slopes/stairs."
        )]
        [Min(0)]
        [SerializeField]
        private float pullMaxDifference;

        [Tooltip("Read and use a pre-generated navigation lookup table instead of generating it at start.")]
        [SerializeField]
        private bool lookupTable;

        [Header("Performance")]
        [Tooltip("The maximum number of agents which can be updated in a single frame. Set to zero to be unlimited.")]
        [Min(0)]
        [SerializeField]
        private int maxAgentsPerUpdate;

        [Tooltip("The maximum number of messages any component can hold.")]
        [Min(0)]
        [SerializeField]
        private int maxMessages = 100;
        
        [Header("UI")]
        [Tooltip("How wide the details list is. Set to zero to disable details list rendering.")]
        [Min(0)]
        [SerializeField]
        private float detailsWidth = 500;
        
        [Tooltip("How wide the controls list is. Set to zero to disable controls list rendering.")]
        [Min(0)]
        [SerializeField]
        private float controlsWidth = 120;

        [Tooltip(
            "Determine what mode messages are stored in.\n" +
            "All - All messages are captured.\n" +
            "Compact - All messages are captured, but, duplicate messages that appear immediately after each other will be merged into only a single instance of the message.\n" +
            "Unique - No messages will be duplicated with the prior instance of the message being removed from its list when an identical message is added again."
        )]
        private MessagingMode _messageMode = MessagingMode.Compact;
    
        [Header("Visualization")]
        [Tooltip("The currently selected camera. Set this to start with that camera active. Leaving empty will default to the first camera by alphabetic order.")]
        [SerializeField]
        private Camera selectedCamera;
        
        [SerializeField]
        [Tooltip(
            "Determine what gizmos lines are drawn.\n" +
            "Off - No lines are drawn.\n" +
            "All - Every line from every agent, sensor, and actuator is drawn.\n" +
            "Selected - If an agent is selected, only it and its sensors and actuators are drawn. If an individual sensor or actuator is selected, only it is drawn."
        )]
        private GizmosState gizmos = GizmosState.Selected;
    
        [SerializeField]
        [Tooltip(
            "Determine what path lines are drawn.\n" +
            "Off - No lines are drawn.\n" +
            "All - Every line for every connection is drawn.\n" +
            "Active - Only lines for connections being used by agents are drawn.\n"+
            "Selected - Only lines for the connections being used by the selected agent are drawn."
        )]
        private PathState paths = PathState.Selected;

        /// <summary>
        /// All cameras in the scene.
        /// </summary>
        private Camera[] _cameras = Array.Empty<Camera>();
        
        /// <summary>
        /// The global messages.
        /// </summary>
        private List<string> _globalMessages = new();

        /// <summary>
        /// All agents which move during an update tick.
        /// </summary>
        private readonly List<Agent> _updateAgents = new();

        /// <summary>
        /// All agents which move during a fixed update tick.
        /// </summary>
        private readonly List<Agent> _fixedUpdateAgents = new();
    
        /// <summary>
        /// State of the GUI system.
        /// </summary>
        private GuiState _state;

        /// <summary>
        /// The agent which is currently thinking.
        /// </summary>
        private int _currentAgentIndex;

        /// <summary>
        /// True if the scene is taking a single time step.
        /// </summary>
        private bool _stepping;

        /// <summary>
        /// If the details menu is currently open.
        /// </summary>
        private bool _detailsOpen;

        /// <summary>
        /// If the controls menu is currently open.
        /// </summary>
        private bool _controlsOpen;

        /// <summary>
        /// The currently selected component.
        /// </summary>
        private IntelligenceComponent _selectedComponent;

        /// <summary>
        /// The navigation lookup table.
        /// </summary>
        private NavigationLookup[] _navigationTable;

        /// <summary>
        /// Lock tracking cameras to the best agent.
        /// </summary>
        private bool _followBest = true;

        /// <summary>
        /// Create a transform agent.
        /// </summary>
        public static GameObject CreateTransformAgent()
        {
            GameObject agent = CreateAgent("Transform Agent");
            agent.AddComponent<TransformAgent>();
            return agent;
        }

        /// <summary>
        /// Create a character controller agent.
        /// </summary>
        public static GameObject CreateCharacterAgent()
        {
            GameObject agent = CreateAgent("Character Agent");
            CharacterController c = agent.AddComponent<CharacterController>();
            c.center = new(0, 1, 0);
            c.minMoveDistance = 0;
            agent.AddComponent<CharacterAgent>();
            return agent;
        }

        /// <summary>
        /// Create a rigidbody agent.
        /// </summary>
        public static GameObject CreateRigidbodyAgent()
        {
            GameObject agent = CreateAgent("Rigidbody Agent");
            CapsuleCollider c = agent.AddComponent<CapsuleCollider>();
            c.center = new(0, 1, 0);
            c.height = 2;
            Rigidbody rb = agent.AddComponent<Rigidbody>();
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.freezeRotation = true;
            agent.AddComponent<RigidbodyAgent>();
            return agent;
        }

        /// <summary>
        /// Create all types of cameras which only adds in those that are not yet present in the scene.
        /// </summary>
        public static void CreateAllCameras()
        {
            if (FindObjectOfType<FollowAgentCamera>() == null)
            {
                CreateFollowAgentCamera();
            }
            else
            {
                Debug.Log("Already have a follow agent camera in the scene - skipping creating one.");
            }
        
            if (FindObjectOfType<LookAtAgentCamera>() == null)
            {
                CreateLookAtAgentCamera();
            }
            else
            {
                Debug.Log("Already have a look at agent camera in the scene - skipping creating one.");
            }
        
            if (FindObjectOfType<TrackAgentCamera>() == null)
            {
                CreateTrackAgentCamera();
            }
            else
            {
                Debug.Log("Already have a track agent camera in the scene - skipping creating one.");
            }
        }

        /// <summary>
        /// Create a follow agent camera.
        /// </summary>
        public static GameObject CreateFollowAgentCamera()
        {
            GameObject camera = CreateCamera("Follow Camera");
            camera.AddComponent<FollowAgentCamera>();
            return camera;
        }

        /// <summary>
        /// Create a look at agent camera.
        /// </summary>
        public static GameObject CreateLookAtAgentCamera()
        {
            GameObject camera = CreateCamera("Look At Camera");
            camera.AddComponent<LookAtAgentCamera>();
            return camera;
        }

        /// <summary>
        /// Create a track agent camera.
        /// </summary>
        public static GameObject CreateTrackAgentCamera()
        {
            GameObject camera = CreateCamera("Track Camera");
            camera.AddComponent<TrackAgentCamera>();
            camera.transform.localRotation = Quaternion.Euler(90, 0, 0);
            return camera;
        }

        /// <summary>
        /// Base method for setting up the core visuals of an agent.
        /// </summary>
        /// <param name="name">The name to give the agent.</param>
        /// <returns>Game object with the visuals setup for a basic agent.</returns>
        private static GameObject CreateAgent(string name)
        {
            GameObject agent = new(name);

            GameObject visuals = new("Visuals");
            visuals.transform.SetParent(agent.transform);
            visuals.transform.localPosition = Vector3.zero;
            visuals.transform.localRotation = Quaternion.identity;
            
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.name = "Body";
            body.transform.SetParent(visuals.transform);
            body.transform.localPosition = new(0, 1, 0);
            body.transform.localRotation = Quaternion.identity;
            DestroyImmediate(body.GetComponent<CapsuleCollider>());
            
            GameObject eyes = GameObject.CreatePrimitive(PrimitiveType.Cube);
            eyes.name = "Eyes";
            eyes.transform.SetParent(body.transform);
            eyes.transform.localPosition = new(0, 0.4f, 0.25f);
            eyes.transform.localRotation = Quaternion.identity;
            eyes.transform.localScale = new(1, 0.2f, 0.5f);
            DestroyImmediate(eyes.GetComponent<BoxCollider>());

            return agent;
        }

        /// <summary>
        /// Base method for setting up a camera.
        /// </summary>
        /// <param name="name">The name to give the camera.</param>
        /// <returns>Game object with a camera.</returns>
        private static GameObject CreateCamera(string name)
        {
            GameObject camera = new(name);
            camera.AddComponent<Camera>();
            return camera;
        }

        /// <summary>
        /// Lookup a path to take from a starting position to an end goal.
        /// </summary>
        /// <param name="position">The starting position.</param>
        /// <param name="goal">The end goal position.</param>
        /// <returns>A list of the points to move to to reach the goal destination.</returns>
        public static List<Vector3> LookupPath(Vector3 position, Vector3 goal)
        {
            // If there are no nodes in the lookup table simply return the end goal position.
            if (Singleton._nodes.Count == 0)
            {
                return new() { goal };
            }
            
            // Check if there is a direct line of sight so we can skip pathing and just move directly towards the goal.
            if (Singleton.navigationRadius <= 0)
            {
                if (!Physics.Linecast(position, goal, Singleton.obstacleLayers))
                {
                    return new() { goal };
                }
            }
            else
            {
                Vector3 p1 = position;
                p1.y += Singleton.navigationRadius;
                Vector3 p2 = goal;
                p2.y += Singleton.navigationRadius;
                if (!Physics.SphereCast(p1, Singleton.navigationRadius, (p2 - p1).normalized, out _, Vector3.Distance(p1, p2), Singleton.obstacleLayers))
                {
                    return new() { goal };
                }
            }
        
            // Get the starting node and end nodes closest to their positions.
            Vector3 nodePosition = Nearest(position);
            Vector3 nodeGoal = Nearest(goal);

            // Add the starting position to the path.
            List<Vector3> path = new() { position };
        
            // If the first node is not the same as the starting position, add it as well.
            if (nodePosition != position)
            {
                path.Add(nodePosition);
            }

            // Loop until the path is finished or the end goal cannot be reached.
            while (true)
            {
                try
                {
                    // Get the next node to move to.
                    NavigationLookup lookup = Singleton._navigationTable.First(l => l.Current == nodePosition && l.Goal == nodeGoal);
                
                    // If the node is the goal destination, all nodes in the path have been finished so stop the loop.
                    if (lookup.Next == nodeGoal)
                    {
                        break;
                    }
                
                    // Move to the next node and add it to the path.
                    nodePosition = lookup.Next;
                    path.Add(nodePosition);
                }
                catch
                {
                    break;
                }
            }
        
            // Add the goal node to the path.
            path.Add(nodeGoal);
        
            // If the goal node and the goal itself are not the same, add the goal itself to the path as well.
            if (goal != nodeGoal)
            {
                path.Add(goal);
            }
        
            // Create a copy of the path in reverse from the end to the start.
            List<Vector3> backwards = new();
            backwards.AddRange(path);
            backwards.Reverse();

            // Pull the strings of both the forwards and backwards path.
            StringPull(path);
            StringPull(backwards);

            // Return the path which is the shortest after string pulling.
            if (PathLength(path) <= PathLength(backwards))
            {
                return path;
            }

            // The backwards path needs to be reversed once again to switch it back to its original order.
            backwards.Reverse();
            return backwards;
        }

        /// <summary>
        /// Find the nearest node to a position.
        /// </summary>
        /// <param name="position">The position to find the nearest node to.</param>
        /// <returns></returns>
        private static Vector3 Nearest(Vector3 position)
        {
            // Order all nodes by distance to the position.
            List<Vector3> potential = Singleton._nodes.OrderBy(n => Vector3.Distance(n, position)).ToList();
            foreach (Vector3 node in potential)
            {
                // If the node is directly at the position, return it.
                if (node == position)
                {
                    return node;
                }
            
                // Otherwise if there is a line of sight to the node, return it.
                if (Singleton.navigationRadius <= 0)
                {
                    if (!Physics.Linecast(position, node, Singleton.obstacleLayers))
                    {
                        return node;
                    }
                    
                    continue;
                }

                Vector3 p1 = position;
                p1.y += Singleton.navigationRadius;
                Vector3 p2 = node;
                p2.y += Singleton.navigationRadius;
                if (!Physics.SphereCast(p1, Singleton.navigationRadius, (p2 - p1).normalized, out _, Vector3.Distance(p1, p2), Singleton.obstacleLayers))
                {
                    return node;
                }
            }

            // If no nodes are in line of sight, return the nearest node even though it is not in line of sight.
            return potential.First();
        }

        /// <summary>
        /// Shorten a path.
        /// </summary>
        /// <param name="path">The path to shorten.</param>
        private static void StringPull(IList<Vector3> path)
        {
            // Loop through every point in the path less two as there must be at least two points in a path.
            for (int i = 0; i < path.Count - 2; i++)
            {
                // Inner loop from two points ahead of the outer loop to check if a node can be skipped.
                for (int j = i + 2; j < path.Count; j++)
                {
                    // Do not string pull for multi-level paths as these could skip over objects that require stairs.
                    if (Math.Abs(path[i].y - path[j].y) > Singleton.pullMaxDifference)
                    {
                        continue;
                    }
                
                    // If a node can be skipped as there is line of sight without it, remove it.
                    if (Singleton.navigationRadius <= 0)
                    {
                        if (!Physics.Linecast(path[i], path[j], Singleton.obstacleLayers))
                        {
                            path.RemoveAt(j-- - 1);
                        }
                        
                        continue;
                    }

                    Vector3 p1 = path[i];
                    p1.y += Singleton.navigationRadius;
                    Vector3 p2 = path[j];
                    p2.y += Singleton.navigationRadius;
                    if (!Physics.SphereCast(p1, Singleton.navigationRadius, (p2 - p1).normalized, out _, Vector3.Distance(p1, p2), Singleton.obstacleLayers))
                    {
                        path.RemoveAt(j-- - 1);
                    }
                }
            }
        }

        /// <summary>
        /// Calculate how long a path is to determine which path is the most optimal to take.
        /// </summary>
        /// <param name="path">The path to get the length of.</param>
        /// <returns>The length of the path.</returns>
        private static float PathLength(IReadOnlyList<Vector3> path)
        {
            float length = 0;
            for (int i = 0; i < path.Count - 1; i++)
            {
                length += Vector3.Distance(path[i], path[i + 1]);
            }

            return length;
        }

        /// <summary>
        /// Set the currently selected agent.
        /// </summary>
        /// <param name="agent">The agent to select.</param>
        public static void SetSelectedAgent(Agent agent)
        {
            Singleton._selectedComponent = null;
            Singleton.SelectedAgent = agent;
            Singleton._state = GuiState.Agent;
        }

        /// <summary>
        /// Lookup a state type from the dictionary.
        /// </summary>
        /// <typeparam name="T">The type of state to register</typeparam>
        /// <returns>The state of the requested type.</returns>
        public static State GetState<T>() where T : State
        {
            return RegisteredStates.ContainsKey(typeof(T)) ? RegisteredStates[typeof(T)] : CreateState<T>();
        }

        public static bool IsInState(Agent agent, Type stateType)
        {
            return agent.State.GetType() == stateType;
        }

        /// <summary>
        /// Setup all agents again.
        /// </summary>
        public static void RefreshAgents()
        {
            foreach (Agent agent in Singleton.Agents)
            {
                agent.Setup();
            }
        }

        /// <summary>
        /// Render a GUI button.
        /// </summary>
        /// <param name="x">X rendering position. In most cases this should remain unchanged.</param>
        /// <param name="y">Y rendering position. In most cases this should remain unchanged.</param>
        /// <param name="w">Width of components. In most cases this should remain unchanged.</param>
        /// <param name="h">Height of components. In most cases this should remain unchanged.</param>
        /// <param name="message">The message to display in the button.</param>
        /// <returns>True if the button was clicked, false if it was not or there was no space for it.</returns>
        public static bool GuiButton(float x, float y, float w, float h, string message)
        {
            return !(y + h > Screen.height) && GUI.Button(new(x, y, w, h), message);
        }

        /// <summary>
        /// Render a GUI label.
        /// </summary>
        /// <param name="x">X rendering position. In most cases this should remain unchanged.</param>
        /// <param name="y">Y rendering position. In most cases this should remain unchanged.</param>
        /// <param name="w">Width of components. In most cases this should remain unchanged.</param>
        /// <param name="h">Height of components. In most cases this should remain unchanged.</param>
        /// <param name="p">Padding of components. In most cases this should remain unchanged.</param>
        /// <param name="message">The message to display.</param>
        public static void GuiLabel(float x, float y, float w, float h, float p, string message)
        {
            if (y + h > Screen.height)
            {
                return;
            }
            
            GUI.Label(new(x + p, y, w - p, h), message);
        }

        /// <summary>
        /// Render a GUI box.
        /// </summary>
        /// <param name="x">X rendering position. In most cases this should remain unchanged.</param>
        /// <param name="y">Y rendering position. In most cases this should remain unchanged.</param>
        /// <param name="w">Width of components. In most cases this should remain unchanged.</param>
        /// <param name="h">Height of components. In most cases this should remain unchanged.</param>
        /// <param name="p">Padding of components. In most cases this should remain unchanged.</param>
        /// <param name="number">How many labels the box should be able to hold.</param>
        public static void GuiBox(float x, float y, float w, float h, float p, int number)
        {
            while (y + (h + p) * number - p > Screen.height)
            {
                number--;
                if (number <= 0)
                {
                    return;
                }
            }
        
            GUI.Box(new(x,y,w,(h + p) * number - p), string.Empty);
        }

        /// <summary>
        /// Determine the updated Y value for the next GUI to be placed with.
        /// </summary>
        /// <param name="y">Y rendering position. In most cases this should remain unchanged.</param>
        /// <param name="h">Height of components. In most cases this should remain unchanged.</param>
        /// <param name="p">Padding of components. In most cases this should remain unchanged.</param>
        /// <returns></returns>
        public static float NextItem(float y, float h, float p)
        {
            return y + h + p;
        }

        /// <summary>
        /// Add a message to the global message list.
        /// </summary>
        /// <param name="message">The message to add.</param>
        public static void AddGlobalMessage(string message)
        {
            switch (Singleton._messageMode)
            {
                case MessagingMode.Compact when Singleton._globalMessages.Count > 0 && Singleton._globalMessages[0] == message:
                    return;
                case MessagingMode.Unique:
                    Singleton._globalMessages = Singleton._globalMessages.Where(m => m != message).ToList();
                    break;
            }

            Singleton._globalMessages.Insert(0, message);
            if (Singleton._globalMessages.Count > MaxMessages)
            {
                Singleton._globalMessages.RemoveAt(Singleton._globalMessages.Count - 1);
            }
        }

        /// <summary>
        /// Register an agent with the agent manager.
        /// </summary>
        /// <param name="agent">The agent to add.</param>
        public static void AddAgent(Agent agent)
        {
            // Ensure the agent is only added once.
            if (Singleton.Agents.Contains(agent))
            {
                return;
            }
            
            // Add to their movement handling list.
            Singleton.Agents.Add(agent);
            switch (agent)
            {
                case TransformAgent updateAgent:
                    Singleton._updateAgents.Add(updateAgent);
                    break;
                case RigidbodyAgent fixedUpdateAgent:
                    Singleton._fixedUpdateAgents.Add(fixedUpdateAgent);
                    break;
            }
            
            // If the agent had any cameras attached to it we need to add them.
            FindCameras();
        }

        /// <summary>
        /// Remove an agent from the agent manager.
        /// </summary>
        /// <param name="agent">The agent to remove.</param>
        public static void RemoveAgent(Agent agent)
        {
            // This should always be true as agents are added at their creation but check just in case.
            if (!Singleton.Agents.Contains(agent))
            {
                return;
            }

            // Remove the agent and update the current index accordingly so no agents are skipped in Update.
            int index = Singleton.Agents.IndexOf(agent);
            Singleton.Agents.Remove(agent);
            if (Singleton._currentAgentIndex > index)
            {
                Singleton._currentAgentIndex--;
            }
            if (Singleton._currentAgentIndex < 0 || Singleton._currentAgentIndex >= Singleton.Agents.Count)
            {
                Singleton._currentAgentIndex = 0;
            }

            // Remove from their movement handling list.
            switch (agent)
            {
                case TransformAgent updateAgent:
                {
                    if (Singleton._updateAgents.Contains(updateAgent))
                    {
                        Singleton._updateAgents.Remove(updateAgent);
                    }

                    break;
                }
                case RigidbodyAgent fixedUpdateAgent:
                {
                    if (Singleton._fixedUpdateAgents.Contains(fixedUpdateAgent))
                    {
                        Singleton._fixedUpdateAgents.Remove(fixedUpdateAgent);
                    }

                    break;
                }
            }

            // If the agent had any cameras attached to it we need to remove them.
            FindCameras();
        }

        /// <summary>
        /// Change the messaging mode.
        /// </summary>
        /// <param name="mode">The mode to change to.</param>
        public static void ChangeMessageMode(MessagingMode mode)
        {
            Singleton._messageMode = mode;
        }

        /// <summary>
        /// Change to the next gizmos state.
        /// </summary>
        private static void ChangeGizmosState()
        {
            if (Singleton.gizmos == GizmosState.Selected)
            {
                Singleton.gizmos = GizmosState.Off;
                return;
            }

            Singleton.gizmos++;
        }

        /// <summary>
        /// Sort all agents by name.
        /// </summary>
        protected static void SortAgents()
        {
            Singleton.Agents = Singleton.Agents.OrderBy(a => a.name).ToList();
        }

        /// <summary>
        /// Find all cameras in the scene so buttons can be setup for them.
        /// </summary>
        private static void FindCameras()
        {
            Singleton._cameras = FindObjectsOfType<Camera>().OrderBy(c => c.name).ToArray();
        }

        /// <summary>
        /// Change to the next messaging mode.
        /// </summary>
        private static void ChangeMessageMode()
        {
            if (Singleton._messageMode == MessagingMode.Unique)
            {
                Singleton._messageMode = MessagingMode.All;
            }
            else
            {
                Singleton._messageMode++;
            }

            if (Singleton._messageMode == MessagingMode.Unique)
            {
                ClearMessages();
            }
        }
        /// <summary>
        /// Register a state type into the dictionary for future reference.
        /// </summary>
        /// <param name="stateToAdd">The state itself.</param>
        /// <typeparam name="T">The type of state to register</typeparam>
        private static void RegisterState<T>(State stateToAdd) where T : State
        {
            RegisteredStates[typeof(T)] = stateToAdd;
        }

        /// <summary>
        /// Create a state if there was not one within the dictionary.
        /// </summary>
        /// <typeparam name="T">The type of state to register</typeparam>
        /// <returns>The state instance that was created</returns>
        private static State CreateState<T>() where T : State
        {
            RegisterState<T>(ScriptableObject.CreateInstance(typeof(T)) as State);
            return RegisteredStates[typeof(T)];
        }

        /// <summary>
        /// Handle moving of agents.
        /// </summary>
        /// <param name="agents">The agents to move.</param>
        private static void MoveAgents(List<Agent> agents)
        {
            foreach (Agent agent in agents)
            {
                agent.MovementCalculations();
            }
        }

        /// <summary>
        /// Setup the material for line rendering.
        /// </summary>
        private static void LineMaterial()
        {
            if (_lineMaterial)
            {
                return;
            }

            // Unity has a built-in shader that is useful for drawing simple colored things.
            _lineMaterial = new(Shader.Find("Hidden/Internal-Colored"))
            {
                hideFlags = HideFlags.HideAndDontSave
            };
            
            // Turn on alpha blending.
            _lineMaterial.SetInt(SrcBlend, (int)BlendMode.SrcAlpha);
            _lineMaterial.SetInt(DstBlend, (int)BlendMode.OneMinusSrcAlpha);
            
            // Turn backface culling off.
            _lineMaterial.SetInt(Cull, (int)CullMode.Off);
            
            // Turn off depth writes.
            _lineMaterial.SetInt(ZWrite, 0);
        }

        /// <summary>
        /// Display gizmos for an agent.
        /// </summary>
        /// <param name="agent">The agent to display gizmos for.</param>
        private static void AgentGizmos(Agent agent)
        {
            agent.DisplayGizmos();

            if (agent.Actuators != null)
            {
                foreach (Actuator actuator in agent.Actuators)
                {
                    actuator.DisplayGizmos();
                }
            }

            if (agent.Sensors != null)
            {
                foreach (Sensor sensor in agent.Sensors)
                {
                    sensor.DisplayGizmos();
                }
            }
        }

        /// <summary>
        /// Go to the next scene.
        /// </summary>
        private static void NextScene()
        {
            int scenes = SceneManager.sceneCountInBuildSettings;
            if (scenes <= 1)
            {
                return;
            }

            int next = SceneManager.GetActiveScene().buildIndex + 1;
            if (next >= scenes)
            {
                next = 0;
            }

            SceneManager.LoadScene(next);
        }

        /// <summary>
        /// Go to the previous scene.
        /// </summary>
        private static void LastScene()
        {
            int scenes = SceneManager.sceneCountInBuildSettings;
            if (scenes <= 1)
            {
                return;
            }

            int next = SceneManager.GetActiveScene().buildIndex - 1;
            if (next <= 0)
            {
                next = scenes - 1;
            }

            SceneManager.LoadScene(next);
        }

        private void OnGUI()
        {
            Render(10, 10, 20, 5);
        }
        
        private void OnRenderObject()
        {
            LineMaterial();
            _lineMaterial.SetPass(0);

            GL.PushMatrix();
            GL.MultMatrix(transform.localToWorldMatrix);
            GL.Begin(GL.LINES);

            // Render navigation nodes if either all or only active nodes should be shown.
            if (paths is not PathState.Off)
            {
                // Render all nodes as white if they should be.
                if (paths == PathState.All)
                {
                    GL.Color(Color.white);
                    foreach (Connection connection in _connections)
                    {
                        Vector3 a = connection.A;
                        a.y += NavigationVisualOffset;
                        Vector3 b = connection.B;
                        b.y += NavigationVisualOffset;
                        GL.Vertex(a);
                        GL.Vertex(b);
                    }
                }

                // Render active nodes in green for either all agents or only the selected agent.
                if (paths is not PathState.Selected)
                {
                    GL.Color(Color.green);
                    foreach (Agent agent in Agents.Where(agent => agent.Path != null && agent.Path.Count != 0))
                    {
                        GL.Vertex(agent.transform.position);
                        GL.Vertex(agent.Path[0]);
                        for (int i = 0; i < agent.Path.Count - 1; i++)
                        {
                            GL.Vertex(agent.Path[i]);
                            GL.Vertex(agent.Path[i + 1]);
                        }
                    }
                }
                else if (SelectedAgent != null && SelectedAgent.Path != null && SelectedAgent.Path.Count != 0)
                {
                    GL.Vertex(SelectedAgent.transform.position);
                    GL.Vertex(SelectedAgent.Path[0]);
                    for (int i = 0; i < SelectedAgent.Path.Count - 1; i++)
                    {
                        GL.Vertex(SelectedAgent.Path[i]);
                        GL.Vertex(SelectedAgent.Path[i + 1]);
                    }
                }
            }
        
            // Nothing to do if gizmos are turned off.
            if (gizmos != GizmosState.Off)
            {
                // Render either all or the selected agent/component.
                if (gizmos == GizmosState.All)
                {
                    foreach (Agent agent in Agents)
                    {
                        AgentGizmos(agent);
                    }
                }
                else
                {
                    if (Agents.Count == 1)
                    {
                        SelectedAgent = Agents[0];
                    }
                
                    if (_selectedComponent != null)
                    {
                        _selectedComponent.DisplayGizmos();
                    }
                    else if (SelectedAgent != null)
                    {
                        AgentGizmos(SelectedAgent);
                    }
                }
            }

            GL.End();
            GL.PopMatrix();
        }

        /// <summary>
        /// Render the automatic GUI.
        /// </summary>
        /// <param name="x">X rendering position. In most cases this should remain unchanged.</param>
        /// <param name="y">Y rendering position. Update this with every component added and return it.</param>
        /// <param name="h">Height of components. In most cases this should remain unchanged.</param>
        /// <param name="p">Padding of components. In most cases this should remain unchanged.</param>
        private static void Render(float x, float y, float h, float p)
        {
            if (Singleton.detailsWidth > 0)
            {
                RenderDetails(x, y, Singleton.detailsWidth, h, p);
            }

            if (Singleton.controlsWidth > 0)
            {
                RenderControls(x, y, Singleton.controlsWidth, h, p);
            }
        }

        /// <summary>
        /// Render the GUI section for displaying message options.
        /// </summary>
        /// <param name="x">X rendering position. In most cases this should remain unchanged.</param>
        /// <param name="y">Y rendering position. Update this with every component added and return it.</param>
        /// <param name="w">Width of components. In most cases this should remain unchanged.</param>
        /// <param name="h">Height of components. In most cases this should remain unchanged.</param>
        /// <param name="p">Padding of components. In most cases this should remain unchanged.</param>
        /// <returns>The updated Y position after all custom rendering has been done.</returns>
        private static float RenderMessageOptions(float x, float y, float w, float h, float p)
        {
            // Button to change messaging mode.
            y = NextItem(y, h, p);
            if (GuiButton(x, y, w / 2 - p, h, Singleton._messageMode switch
                {
                    MessagingMode.Compact => "Message Mode: Compact",
                    MessagingMode.All => "Message Mode: All",
                    _ => "Message Mode: Unique"
                }))
            {
                ChangeMessageMode();
            }

            // Button to clear messages.
            if (GuiButton(x + w / 2 + p, y, w / 2 - p, h, "Clear Messages"))
            {
                ClearMessages();
            }

            return y;
        }

        /// <summary>
        /// Render the automatic details GUI.
        /// </summary>
        /// <param name="x">X rendering position. In most cases this should remain unchanged.</param>
        /// <param name="y">Y rendering position. Update this with every component added and return it.</param>
        /// <param name="w">Width of components. In most cases this should remain unchanged.</param>
        /// <param name="h">Height of components. In most cases this should remain unchanged.</param>
        /// <param name="p">Padding of components. In most cases this should remain unchanged.</param>
        private static void RenderDetails(float x, float y, float w, float h, float p)
        {
            if (Singleton.Agents.Count < 1)
            {
                return;
            }

            if (!Singleton._detailsOpen)
            {
                w = ClosedSize;
            }

            if (w + 4 * p > Screen.width)
            {
                w = Screen.width - 4 * p;
            }
            
            // Button open/close details.
            if (GuiButton(x, y, w, h, Singleton._detailsOpen ? "Close" : "Details"))
            {
                Singleton._detailsOpen = !Singleton._detailsOpen;
            }
            
            if (!Singleton._detailsOpen)
            {
                return;
            }

            if (Singleton.SelectedAgent == null && Singleton._state == GuiState.Agent || Singleton._selectedComponent == null && Singleton._state == GuiState.Component)
            {
                Singleton._state = GuiState.Main;
            }

            if (Singleton._state == GuiState.Main && Singleton.Agents.Count == 1)
            {
                Singleton.SelectedAgent = Singleton.Agents[0];
                Singleton._state = GuiState.Agent;
            }

            // Handle agent view rendering.
            if (Singleton._state == GuiState.Agent)
            {
                // Can only go to the main view if there is more than one agent.
                if (Singleton.Agents.Count > 1)
                {
                    // Button to go back to the main view.
                    y = NextItem(y, h, p);
                    if (GuiButton(x, y, w, h, "Back to Overview" + (Singleton._followBest ? " - Stop Following Best" : string.Empty)))
                    {
                        Singleton._followBest = false;
                        Singleton._state = GuiState.Main;
                    }
                }
                
                RenderAgent(x, y, w, h, p);

                return;
            }

            // Handle components view rendering.
            if (Singleton._state == GuiState.Components)
            {
                // Button to go back to the agents view.
                y = NextItem(y, h, p);
                if (GuiButton(x, y, w, h, $"Back to {Singleton.SelectedAgent.name}"))
                {
                    Singleton._state = GuiState.Agent;
                }
                else
                {
                    RenderComponents(x, y, w, h, p);
                }

                return;
            }

            // Handle the component view.
            if (Singleton._state == GuiState.Component)
            {
                // Button to go back to the components view.
                y = NextItem(y, h, p);
                if (GuiButton(x, y, w, h, $"Back to {Singleton.SelectedAgent.name} Sensors and Actuators"))
                {
                    Singleton._selectedComponent = null;
                    Singleton._state = GuiState.Components;
                }
                
                RenderComponent(x, y, w, h, p);
                return;
            }

            // Display all agents.
            y = NextItem(y, h, p);
            GuiBox(x, y, w, h, p, 1);
            GuiLabel(x, y, w, h, p, $"{Singleton.Agents.Count} Agents");

            foreach (Agent agent in Singleton.Agents)
            {
                // Button to select an agent.
                y = NextItem(y, h, p);
                if (!GuiButton(x, y, w, h, $"{agent.name} - {agent}"))
                {
                    continue;
                }

                Singleton.SelectedAgent = agent;
                Singleton._state = GuiState.Agent;
            }
            
            // Display global messages.
            if (Singleton._globalMessages.Count == 0)
            {
                return;
            }
            
            y = RenderMessageOptions(x, y, w, h, p);
            
            y = NextItem(y, h, p);
            GuiBox(x, y, w, h, p, Singleton._globalMessages.Count);
            
            foreach (string message in Singleton._globalMessages)
            {
                GuiLabel(x, y, w, h, p, message);
                y = NextItem(y, h, p);
            }
        }

        /// <summary>
        /// Render the automatic agent GUI.
        /// </summary>
        /// <param name="x">X rendering position. In most cases this should remain unchanged.</param>
        /// <param name="y">Y rendering position. Update this with every component added and return it.</param>
        /// <param name="w">Width of components. In most cases this should remain unchanged.</param>
        /// <param name="h">Height of components. In most cases this should remain unchanged.</param>
        /// <param name="p">Padding of components. In most cases this should remain unchanged.</param>
        private static void RenderAgent(float x, float y, float w, float h, float p)
        {
            if (Singleton.SelectedAgent == null)
            {
                Singleton._state = GuiState.Main;
                return;
            }
            
            y = NextItem(y, h, p);
            int length = 2;
            if (Singleton.Agents.Count > 1)
            {
                length++;
            }

            if (Singleton.SelectedAgent.State != null)
            {
                length++;
            }

            if (Singleton.SelectedAgent.PerformanceMeasure != null)
            {
                length++;
            }

            if (Singleton.SelectedAgent.Destination != null)
            {
                length++;
            }
            else
            {
                if (Singleton.SelectedAgent.Moves.Count > 0)
                {
                    length += Singleton.SelectedAgent.Moves.Count;
                }
                else
                {
                    length++;
                }
            }

            // Display all agent details.
            GuiBox(x, y, w, h, p, length);
            if (Singleton.Agents.Count > 1)
            {
                GuiLabel(x, y, w, h, p, Singleton.SelectedAgent.name);
                y = NextItem(y, h, p);
            }
        
            if (Singleton.SelectedAgent.State != null)
            {
                GuiLabel(x, y, w, h, p, $"State: {Singleton.SelectedAgent.State}");
                y = NextItem(y, h, p);
            }
        
            if (Singleton.SelectedAgent.PerformanceMeasure != null)
            {
                GuiLabel(x, y, w, h, p, $"Performance: {Singleton.SelectedAgent.Performance}");
                y = NextItem(y, h, p);
            }
        
            GuiLabel(x, y, w, h, p, $"Position: {Singleton.SelectedAgent.transform.position} | Velocity: {Singleton.SelectedAgent.MoveVelocity.magnitude}");
        
            y = NextItem(y, h, p);
            GuiLabel(x, y, w, h, p, $"Rotation: {Singleton.SelectedAgent.Visuals.rotation.eulerAngles.y} Degrees" + (Singleton.SelectedAgent.LookingToTarget ? $" | Looking to {Singleton.SelectedAgent.LookTarget} at {Singleton.SelectedAgent.LookSpeed} degrees/second." : string.Empty));

            if (Singleton.SelectedAgent.Destination != null)
            {
                Vector3 destination = Singleton.SelectedAgent.Destination.Value;
                y = NextItem(y, h, p);
                GuiLabel(x, y, w, h, p, $"Navigating to ({destination.x}, {destination.z})");
            }
            else
            {
                if (Singleton.SelectedAgent.Moves.Count > 0)
                {
                    foreach (Agent.Movement movement in Singleton.SelectedAgent.Moves)
                    {
                        string toFrom = Steering.IsApproachingBehaviour(movement.Behaviour) ? " towards" : " from";
                        Vector3 pos3 = movement.Transform != null ? movement.Transform.position : Vector3.zero;
                        string pos = movement.Transform != null ? $" ({pos3.x}, {pos3.z})" : $" ({movement.Position.x}, {movement.Position.y})";
                        y = NextItem(y, h, p);
                        GuiLabel(x, y, w, h, p, $"{movement.Behaviour}{toFrom}{pos}");
                    }
                }
                else
                {
                    y = NextItem(y, h, p);
                    GuiLabel(x, y, w, h, p, $"Not moving");
                }
            }

            // Display any custom details implemented for the agent.
            y = Singleton.SelectedAgent.DisplayDetails(x, y, w, h, p);

            // Display all sensors for the agent.
            if (Singleton.SelectedAgent.Sensors.Length > 0 && Singleton.SelectedAgent.Actuators.Length > 0)
            {
                y = NextItem(y, h, p);
                if (GuiButton(x, y, w, h, "Sensors, Actuators, Percepts, and Actions"))
                {
                    Singleton._state = GuiState.Components;
                }
            }

            if (!Singleton.SelectedAgent.HasMessages)
            {
                return;
            }

            // Display all messages for the agent.
            y = RenderMessageOptions(x, y, w, h, p);
            
            y = NextItem(y, h, p);
            GuiBox(x, y, w, h, p, Singleton.SelectedAgent.MessageCount);
            
            foreach (string message in Singleton.SelectedAgent.Messages)
            {
                GuiLabel(x, y, w, h, p, message);
                y = NextItem(y, h, p);
            }
        }

        /// <summary>
        /// Render the automatic components GUI.
        /// </summary>
        /// <param name="x">X rendering position. In most cases this should remain unchanged.</param>
        /// <param name="y">Y rendering position. Update this with every component added and return it.</param>
        /// <param name="w">Width of components. In most cases this should remain unchanged.</param>
        /// <param name="h">Height of components. In most cases this should remain unchanged.</param>
        /// <param name="p">Padding of components. In most cases this should remain unchanged.</param>
        private static void RenderComponents(float x, float y, float w, float h, float p)
        {
            if (Singleton.SelectedAgent == null)
            {
                Singleton._state = GuiState.Main;
                return;
            }
            
            // List all sensors.
            y = NextItem(y, h, p);
            GuiBox(x, y, w, h, p, 1);
            GuiLabel(x, y, w, h, p, Singleton.SelectedAgent.Sensors.Length switch
            {
                0 => "No Sensors",
                1 => "1 Sensor",
                _ => $"{Singleton.SelectedAgent.Sensors.Length} Sensors"
            });

            foreach (Sensor sensor in Singleton.SelectedAgent.Sensors)
            {
                // Button to select a sensor.
                y = NextItem(y, h, p);
                if (!GuiButton(x, y, w, h, sensor.ToString()))
                {
                    continue;
                }

                Singleton._selectedComponent = sensor;
                Singleton._state = GuiState.Component;
            }
            
            // Display all actuators.
            y = NextItem(y, h, p);
            GuiBox(x, y, w, h, p, 1);
            GuiLabel(x, y, w, h, p, Singleton.SelectedAgent.Actuators.Length switch
            {
                0 => "No Actuators",
                1 => "1 Actuator",
                _ => $"{Singleton.SelectedAgent.Actuators.Length} Actuators"
            });
            
            foreach (Actuator actuator in Singleton.SelectedAgent.Actuators)
            {
                // Button to select an actuator.
                y = NextItem(y, h, p);
                if (!GuiButton(x, y, w, h, actuator.ToString()))
                {
                    continue;
                }

                Singleton._selectedComponent = actuator;
                Singleton._state = GuiState.Component;
            }
        }
        
        /// <summary>
        /// Render the automatic component GUI.
        /// </summary>
        /// <param name="x">X rendering position. In most cases this should remain unchanged.</param>
        /// <param name="y">Y rendering position. Update this with every component added and return it.</param>
        /// <param name="w">Width of components. In most cases this should remain unchanged.</param>
        /// <param name="h">Height of components. In most cases this should remain unchanged.</param>
        /// <param name="p">Padding of components. In most cases this should remain unchanged.</param>
        private static void RenderComponent(float x, float y, float w, float h, float p)
        {
            if (Singleton._selectedComponent == null)
            {
                Singleton._state = GuiState.Components;
                return;
            }
            
            // Display component details.
            y = NextItem(y, h, p);
            GuiBox(x, y, w, h, p, 1);
            GuiLabel(x, y, w, h, p, $"{Singleton.SelectedAgent.name} | {Singleton._selectedComponent}");
            
            // Display any custom details implemented for the component.
            y = Singleton._selectedComponent.DisplayDetails(x, y, w, h, p);
            
            // Display component messages.
            if (!Singleton._selectedComponent.HasMessages)
            {
                return;
            }
            
            y = RenderMessageOptions(x, y, w, h, p);
            
            y = NextItem(y, h, p);
            GuiBox(x, y, w, h, p, Singleton._selectedComponent.MessageCount);

            foreach (string message in Singleton._selectedComponent.Messages)
            {
                GuiLabel(x, y, w, h, p, message);
                y = NextItem(y, h, p);
            }
        }

        /// <summary>
        /// Render the automatic controls GUI.
        /// </summary>
        /// <param name="x">X rendering position. In most cases this should remain unchanged.</param>
        /// <param name="y">Y rendering position. Update this with every component added and return it.</param>
        /// <param name="w">Width of components. In most cases this should remain unchanged.</param>
        /// <param name="h">Height of components. In most cases this should remain unchanged.</param>
        /// <param name="p">Padding of components. In most cases this should remain unchanged.</param>
        private static void RenderControls(float x, float y, float w, float h, float p)
        {
            if (!Singleton._controlsOpen)
            {
                w = ClosedSize;
            }
        
            if (Singleton.Agents.Count == 0 && w + 4 * p > Screen.width)
            {
                w = Screen.width - 4 * p;
            }

            if (Singleton.Agents.Count > 0 && Screen.width < (Singleton._detailsOpen ? Singleton.detailsWidth : ClosedSize) + Singleton.controlsWidth + 5 * p)
            {
                return;
            }
            
            x = Screen.width - x - w;

            // Button open/close controls.
            if (GuiButton(x, y, w, h, Singleton._controlsOpen ? "Close" : "Controls"))
            {
                Singleton._controlsOpen = !Singleton._controlsOpen;
            }
            
            if (!Singleton._controlsOpen)
            {
                return;
            }

            y = NextItem(y, h, p);
            y = Singleton.CustomRendering(x, y, w, h, p);

            if (Singleton.Agents.Count > 1)
            {
                // Button to lock any tracking cameras to the best performing agent or not.
                if (GuiButton(x, y, w, h, Singleton._followBest ? "Stop Following" : "Follow Best"))
                {
                    Singleton._followBest = !Singleton._followBest;
                    if (Singleton._followBest && Singleton._state == GuiState.Main)
                    {
                        Singleton._state = GuiState.Agent;
                    }
                }
            
                y = NextItem(y, h, p);
            }
            else
            {
                Singleton._followBest = false;
            }

            // Button to pause or resume the scene.
            if (GuiButton(x, y, w, h, Playing ? "Pause" : "Resume"))
            {
                if (Playing)
                {
                    Pause();
                }
                else
                {
                    Resume();
                }
            }

            if (!Playing)
            {
                // Button to take a single time step.
                y = NextItem(y, h, p);
                if (GuiButton(x, y, w, h, "Step"))
                {
                    Step();
                }
            }
        
            // Button to change gizmos mode.
            y = NextItem(y, h, p);
            if (GuiButton(x, y, w, h, Singleton.gizmos switch
                {
                    GizmosState.Off => "Gizmos: Off",
                    GizmosState.Selected => "Gizmos: Selected",
                    _ => "Gizmos: All"
                }))
            {
                ChangeGizmosState();
            }

            if (Singleton._connections.Count > 0)
            {
                // Button to change navigation mode.
                y = NextItem(y, h, p);
                if (GuiButton(x, y, w, h, Singleton.paths switch
                    {
                        PathState.Off => "Nodes: Off",
                        PathState.Active => "Nodes: Active",
                        PathState.All => "Nodes: All",
                        _ => "Nodes: Selected"
                    }))
                {
                    ChangeNavigationState();
                }
            }

            if (Singleton._cameras.Length > 1)
            {
                // Buttons to switch cameras.
                foreach (Camera cam in Singleton._cameras)
                {
                    y = NextItem(y, h, p);
                    if (GuiButton(x, y, w, h, cam.name))
                    {
                        SwitchCamera(cam);
                    }
                }
            }

            if (SceneManager.sceneCountInBuildSettings > 1)
            {
                // Display button to go to the next scene.
                y = NextItem(y, h, p);
                if (GuiButton(x, y, w, h, "Next Scene"))
                {
                    NextScene();
                }

                if (SceneManager.sceneCountInBuildSettings > 2)
                {
                    // Display button to go to the previous scene.
                    y = NextItem(y, h, p);
                    if (GuiButton(x, y, w, h, "Last Scene"))
                    {
                        LastScene();
                    }
                }
            }

            // Button to quit.
            y = NextItem(y, h, p);
            if (GuiButton(x, y, w, h, "Quit"))
            {
#if UNITY_EDITOR
                EditorApplication.ExitPlaymode();
#else
                Application.Quit();
#endif
            }
        }

        /// <summary>
        /// Go to the next agent.
        /// </summary>
        private void NextAgent()
        {
            _currentAgentIndex++;
            if (_currentAgentIndex >= Agents.Count)
            {
                _currentAgentIndex = 0;
            }
        }

        /// <summary>
        /// Resume playing.
        /// </summary>
        private static void Resume()
        {
            Time.timeScale = 1;
        }

        /// <summary>
        /// Pause playing.
        /// </summary>
        private static void Pause()
        {
            Time.timeScale = 0;
        }

        /// <summary>
        /// Step for a single frame.
        /// </summary>
        private static void Step()
        {
            Singleton.StartCoroutine(StepOneFrame());
        }
        
        /// <summary>
        /// Change to the next navigation state.
        /// </summary>
        private static void ChangeNavigationState()
        {
            if (Singleton.paths == PathState.Selected)
            {
                Singleton.paths = PathState.Off;
                return;
            }

            Singleton.paths++;
        }

        /// <summary>
        /// Clear all messages.
        /// </summary>
        private static void ClearMessages()
        {
            Singleton._globalMessages.Clear();
            foreach (IntelligenceComponent component in FindObjectsOfType<IntelligenceComponent>())
            {
                component.ClearMessages();
            }
        }

        /// <summary>
        /// Switch to a camera.
        /// </summary>
        /// <param name="cam">The camera to switch to.</param>
        private static void SwitchCamera(Camera cam)
        {
            Singleton.selectedCamera = cam;
            cam.enabled = true;
            foreach (Camera cam2 in Singleton._cameras)
            {
                if (cam != cam2)
                {
                    cam2.enabled = false;
                }
            }
        }
        
        /// <summary>
        /// Coroutine lasts for exactly one frame to step though each time step.
        /// </summary>
        /// <returns>Nothing.</returns>
        private static IEnumerator StepOneFrame()
        {
            Singleton._stepping = true;
            Resume();
            yield return 0;
            Pause();
            Singleton._stepping = false;
        }

        /// <summary>
        /// Perform A* pathfinding.
        /// </summary>
        /// <param name="current">The starting position.</param>
        /// <param name="goal">The end goal position.</param>
        /// <returns>The path of nodes to take.</returns>
        private List<Vector3> AStar(Vector3 current, Vector3 goal)
        {
            AStarNode best = null;
        
            // Add the starting position to the list of nodes.
            List<AStarNode> aStarNodes = new() { new(current, goal) };

            // Loop until there are no options left meaning the path cannot be completed.
            while (aStarNodes.Any(n => n.IsOpen))
            {
                // Get the open node with the lowest F cost and then by the lowest H cost if there is a tie in F cost.
                AStarNode node = aStarNodes.Where(n => n.IsOpen).OrderBy(n => n.CostF).ThenBy(n => n.CostH).First();
            
                // Close the current node.
                node.Close();
            
                // Update this to the best node if it is.
                if (best == null || node.CostF < best.CostF)
                {
                    best = node;
                }
            
                // Loop through all nodes which connect to the current node.
                foreach (Connection connection in _connections.Where(c => c.A == node.Position || c.B == node.Position))
                {
                    // Get the other position in the connection so we do not work with the exact same node again and get stuck.
                    Vector3 position = connection.A == node.Position ? connection.B : connection.A;
                
                    // Create the A* node.
                    AStarNode successor = new(position, goal, node);

                    // If this node is the goal destination, A* is done so set it as the best and clear the node list so the loop ends.
                    if (position == goal)
                    {
                        best = successor;
                        aStarNodes.Clear();
                        break;
                    }

                    // If the node is not yet in the list, add it.
                    AStarNode existing = aStarNodes.FirstOrDefault(n => n.Position == position);
                    if (existing == null)
                    {
                        aStarNodes.Add(successor);
                        continue;
                    }

                    // If it did already exist in the list but this path takes longer do nothing. 
                    if (existing.CostF <= successor.CostF)
                    {
                        continue;
                    }

                    // If the new path is shorter, update its previous node and open it again.
                    existing.UpdatePrevious(node);
                    existing.Open();
                }
            }

            // If there was no best node which should never happen, simply return a line between the start and end positions.
            if (best == null)
            {
                return new() { current, goal };
            }

            // Go from the last to node to the first adding all positions to the path.
            List<Vector3> path = new();
            while (best != null)
            {
                path.Add(best.Position);
                best = best.Previous;
            }

            // Reverse the path so it is from start to goal and return it.
            path.Reverse();
        
            // Reduce the path if possible.
            StringPull(path);
        
            return path;
        }

        /// <summary>
        /// Write lookup table data to a file.
        /// </summary>
        private void WriteLookupData()
        {
            // Build the data string.
            string data = string.Empty;
            for (int i = 0; i < _navigationTable.Length; i++)
            {
                data += $"{_navigationTable[i].Current.x} {_navigationTable[i].Current.y} {_navigationTable[i].Current.z} {_navigationTable[i].Goal.x} {_navigationTable[i].Goal.y} {_navigationTable[i].Goal.z} {_navigationTable[i].Next.x} {_navigationTable[i].Next.y} {_navigationTable[i].Next.z}";
                if (i != _navigationTable.Length - 1)
                {
                    data += "\n";
                }
            }

            // Do not create files if there is no node data.
            if (string.IsNullOrWhiteSpace(data))
            {
                return;
            }
        
            // Create the directory.
            if (!Directory.Exists(Folder))
            {
                DirectoryInfo info = Directory.CreateDirectory(Folder);
                if (!info.Exists)
                {
                    return;
                }
            }

            // Write to the file.
            string fileName = $"{Folder}/{SceneManager.GetActiveScene().name}.txt";
            StreamWriter writer = new(fileName, false);
            writer.Write(data);
            writer.Close();
        }

        /// <summary>
        /// Read lookup table data from a file.
        /// </summary>
        private void ReadLookupData()
        {
            // If there is no directory for data return.
            if (!Directory.Exists(Folder))
            {
                // Set that there was no pre-generated data so data can now be generated.
                lookupTable = false;
                return;
            }
        
            // If there is no file for data return.
            string fileName = $"{Folder}/{SceneManager.GetActiveScene().name}.txt";
            if (!File.Exists(fileName))
            {
                // Set that there was no pre-generated data so data can now be generated.
                lookupTable = false;
                return;
            }

            List<NavigationLookup> lookups = new();

            // Read all lines from the file.
            string[] lines = File.ReadAllLines(fileName);
        
            // Loop through all lines building each into a data entry for the lookup table.
            foreach (string line in lines)
            {
                string[] s = line.Split(' ');
                NavigationLookup lookup = new(
                    new(float.Parse(s[0]), float.Parse(s[1]), float.Parse(s[2])),
                    new(float.Parse(s[3]), float.Parse(s[4]), float.Parse(s[5])),
                    new(float.Parse(s[6]), float.Parse(s[7]), float.Parse(s[8]))
                );

                // Ensure all nodes are added.
                if (!_nodes.Contains(lookup.Current))
                {
                    _nodes.Add(lookup.Current);
                }
            
                if (!_nodes.Contains(lookup.Goal))
                {
                    _nodes.Add(lookup.Goal);
                }
            
                if (!_nodes.Contains(lookup.Next))
                {
                    _nodes.Add(lookup.Next);
                }

                // Ensure a connection between the current and next nodes exists.
                if (!_connections.Any(c => c.A == lookup.Current && c.B == lookup.Next || c.A == lookup.Next && c.B == lookup.Current))
                {
                    _connections.Add(new(lookup.Current, lookup.Next));
                }
            
                // Add to the lookup table.
                lookups.Add(lookup);
            }

            // Finalize the lookup table.
            _navigationTable = lookups.ToArray();
        }
        
                protected virtual void Awake()
        {
            if (Singleton == this)
            {
                return;
            }

            if (Singleton != null)
            {
                Destroy(gameObject);
                return;
            }

            Singleton = this;
        }

        protected virtual void Start()
        {
            // If we should use a pre-generated lookup table, use it if one exists.
            if (lookupTable)
            {
                ReadLookupData();
            }
        
            // If we should generate a lookup table or there was not one pre-generated to load, generate one.
            if (!lookupTable)
            {
                // Generate all node areas in the scene.
                foreach (NodeArea nodeArea in FindObjectsOfType<NodeArea>())
                {
                    nodeArea.Generate();
                }

                // Setup all freely-placed nodes.
                foreach (Node node in FindObjectsOfType<Node>())
                {
                    Vector3 p = node.transform.position;
                    node.Finish();
                
                    foreach (Vector3 v in _nodes)
                    {
                        // Ensure the nodes are in range to form a connection.
                        float d = Vector3.Distance(p, v);
                        if (nodeDistance > 0 && d > nodeDistance)
                        {
                            continue;
                        }
                    
                        // Ensure the nodes have line of sight on each other.
                        if (navigationRadius <= 0)
                        {
                            if (Physics.Linecast(p, v, obstacleLayers))
                            {
                                continue;
                            }
                        }
                        else
                        {
                            Vector3 p1 = p;
                            p1.y += navigationRadius;
                            Vector3 p2 = v;
                            p2.y += navigationRadius;
                            Vector3 direction = (p2 - p1).normalized;
                            if (Physics.SphereCast(p1, navigationRadius, direction, out _, d, obstacleLayers))
                            {
                                continue;
                            }
                        }
                    
                        // Ensure there is not already an entry for this connection in the list.
                        if (_connections.Any(c => c.A == p && c.B == v || c.A == v && c.B == p))
                        {
                            continue;
                        }
                
                        // Add the connection to the list.
                        _connections.Add(new(p, v));
                    }
                
                    _nodes.Add(p);
                }

                // If any nodes are not a part of any connections, remove them.
                for (int i = 0; i < _nodes.Count; i++)
                {
                    if (!_connections.Any(c => c.A == _nodes[i] || c.B == _nodes[i]))
                    {
                        _nodes.RemoveAt(i--);
                    }
                }

                // Store all new lookup tables.
                List<NavigationLookup> table = new();
        
                // Loop through all nodes.
                for (int i = 0; i < _nodes.Count; i++)
                {
                    // Loop through all nodes again so pathfinding can be done on each pair.
                    for (int j = 0; j < _nodes.Count; j++)
                    {
                        // Skip if each node is the same.
                        if (i == j)
                        {
                            continue;
                        }

                        // Get the A* path from one node to another.
                        List<Vector3> path = AStar(_nodes[i], _nodes[j]);
                    
                        // Skip if there was no path.
                        if (path.Count < 2)
                        {
                            continue;
                        }

                        // Loop through all nodes in the path and add them to the lookup table.
                        for (int k = 0; k < path.Count - 1; k++)
                        {
                            // Ensure there are no duplicates in the lookup table.
                            if (path[k] == _nodes[j] || table.Any(t => t.Current == path[k] && t.Goal == _nodes[j] && t.Next == path[k + 1]))
                            {
                                continue;
                            }

                            NavigationLookup lookup = new(path[k], _nodes[j], path[k + 1]);
                            table.Add(lookup);
                        }
                    }
                }

                // Finalize the lookup table.
                _navigationTable = table.ToArray();

                // Write the lookup table to a file for fast reading on future runs.
                WriteLookupData();
            }

            // Clean up all node related components in the scene as they are no longer needed after generation.
            foreach (NodeBase nodeBase in FindObjectsOfType<NodeBase>().OrderBy(n => n.transform.childCount))
            {
                nodeBase.Finish();
            }
        
            // Setup cameras.
            FindCameras();
            if (selectedCamera != null)
            {
                SwitchCamera(selectedCamera);
            }
            else if (_cameras.Length > 0)
            {
                SwitchCamera(_cameras[0]);
            }
            else
            {
                CreateFollowAgentCamera();
                CreateTrackAgentCamera();
                FindCameras();
                SwitchCamera(_cameras[0]);
            }
        }

        protected virtual void Update()
        {
            if (Agents.Count == 1)
            {
                SelectedAgent = Agents[0];
            }
        
            // Perform for all agents if there is no limit or only the next allowable number of agents if there is.
            if (maxAgentsPerUpdate <= 0)
            {
                for (int i = 0; i < Agents.Count; i++)
                {
                    try
                    {
                        Agents[i].Perform();
                    }
                    catch
                    {
                        // Ignored.
                    }
                }
            }
            else
            {
                for (int i = 0; i < maxAgentsPerUpdate && i < Agents.Count; i++)
                {
                    try
                    {
                        Agents[_currentAgentIndex].Perform();
                    }
                    catch
                    {
                        continue;
                    }
                
                    NextAgent();
                }
            }

            // Update the delta time for all agents and look towards their targets.
            foreach (Agent agent in Agents)
            {
                agent.IncreaseDeltaTime();
                agent.LookCalculations();
            }
        
            // Move agents that do not require physics.
            MoveAgents(_updateAgents);

            // Click to select an agent.
            if (Mouse.current.leftButton.wasPressedThisFrame && Physics.Raycast(selectedCamera.ScreenPointToRay(new(Mouse.current.position.x.ReadValue(), Mouse.current.position.y.ReadValue(), 0)), out RaycastHit hit, Mathf.Infinity))
            {
                // See if an agent was actually hit with the click and select it if so.
                Transform tr = hit.collider.transform;
                do
                {
                    Agent clicked = tr.GetComponent<Agent>();
                    if (clicked != null)
                    {
                        SelectedAgent = clicked;
                        _followBest = false;
                        break;
                    }
                    tr = tr.parent;
                } while (tr != null);
            }

            if (!_followBest)
            {
                return;
            }

            // If locked to following the best agent, select the best agent.
            float best = float.MinValue;
            SelectedAgent = null;
            foreach (Agent agent in Agents.Where(a => a.PerformanceMeasure != null))
            {
                float score = agent.PerformanceMeasure.GetPerformance();
                if (score <= best)
                {
                    continue;
                }

                best = score;
                SelectedAgent = agent;
            }

            if (SelectedAgent == null)
            {
                _followBest = false;
                return;
            }

            if (Singleton._state == GuiState.Main)
            {
                Singleton._state = GuiState.Agent;
            }
        }

        protected void FixedUpdate()
        {
            // Move agents that require physics.
            MoveAgents(_fixedUpdateAgents);
        }

        /// <summary>
        /// Override for custom detail rendering on the automatic GUI.
        /// </summary>
        /// <param name="x">X rendering position. In most cases this should remain unchanged.</param>
        /// <param name="y">Y rendering position. Update this with every component added and return it.</param>
        /// <param name="w">Width of components. In most cases this should remain unchanged.</param>
        /// <param name="h">Height of components. In most cases this should remain unchanged.</param>
        /// <param name="p">Padding of components. In most cases this should remain unchanged.</param>
        /// <returns>The updated Y position after all custom rendering has been done.</returns>
        protected virtual float CustomRendering(float x, float y, float w, float h, float p)
        {
            return y;
        }
    }
}