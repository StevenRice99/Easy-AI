using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using EasyAI.Navigation;
using EasyAI.Navigation.Utility;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using Debug = UnityEngine.Debug;
using System.IO;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace EasyAI
{
    /// <summary>
    /// Singleton to handle agents and GUI rendering. Must be exactly one of this or an extension of this present in every scene.
    /// </summary>
    [SelectionBase]
    [DisallowMultipleComponent]
    public class EasyManager : MonoBehaviour
    {
        /// <summary>
        /// Determine what path lines are drawn.
        /// Off - No lines are drawn.
        /// All - Lines for every move, navigation, and connection is drawn.
        /// Active - Lines for every move and navigation drawn.
        /// Selected - Only lines for the moves and navigation of the selected agent are drawn.
        /// </summary>
        private enum PathState : byte
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
        /// </summary>
        private enum GuiState : byte
        {
            Main,
            Agent,
            Components
        }
        
        /// <summary>
        /// The width of the GUI buttons to open their respective menus when they are closed.
        /// </summary>
        private const float ClosedSize = 70;

        /// <summary>
        /// How much to visually offset navigation by, so it does not clip into the ground.
        /// </summary>
        private const float NavigationVisualOffset = 0.1f;
        
        /// <summary>
        /// Open symbol.
        /// </summary>
        private const char Open = ' ';
    
        /// <summary>
        /// Closed symbol.
        /// </summary>
        private const char Closed = '#';

        /// <summary>
        /// Node symbol.
        /// </summary>
        private const char Node = '*';

        /// <summary>
        /// How close an agent can be to a location its seeking or pursuing to declare it as reached.
        /// </summary>
        public static float SeekDistance => Singleton.seekDistance;

        /// <summary>
        /// How far an agent can be to a location its fleeing or evading from to declare it as reached.
        /// </summary>
        public static float FleeDistance => Singleton.fleeDistance;

        /// <summary>
        /// If an agent is not moving, ensure it comes to a complete stop when its velocity is less than this.
        /// </summary>
        public static float RestVelocity => Singleton.restVelocity;

        /// <summary>
        /// Which layers can nodes be placed on.
        /// </summary>
        public static LayerMask GroundLayers => Singleton.groundLayers;

        /// <summary>
        /// Which layers are obstacles that nodes cannot be placed on.
        /// </summary>
        public static LayerMask ObstacleLayers => Singleton.obstacleLayers;

        /// <summary>
        /// The maximum number of messages any component can hold.
        /// </summary>
        public static int MaxMessages => Singleton.maxMessages;

        /// <summary>
        /// The currently selected agent.
        /// </summary>
        public static EasyAgent CurrentlySelectedAgent => Singleton.SelectedAgent;

        /// <summary>
        /// The currently selected camera.
        /// </summary>
        public static Camera SelectedCamera => Singleton.selectedCamera;

        /// <summary>
        /// All agents in the scene.
        /// </summary>
        public static List<EasyAgent> CurrentAgents => Singleton.Agents;

        /// <summary>
        /// All agents in the scene.
        /// </summary>
        public List<EasyAgent> Agents { get; private set; } = new();

        /// <summary>
        /// The singleton agent manager.
        /// </summary>
        protected static EasyManager Singleton;
    
        /// <summary>
        /// All registered states.
        /// </summary>
        private static readonly Dictionary<Type, EasyState> RegisteredStates = new();
        
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
        /// The currently selected agent.
        /// </summary>
        protected EasyAgent SelectedAgent;
        
        /// <summary>
        /// Which layers can nodes be placed on.
        /// </summary>
        [Header("Navigation")]
        [Tooltip("Which layers can nodes be placed on.")]
        [SerializeField]
        private LayerMask groundLayers;

        /// <summary>
        /// Which layers are obstacles that nodes cannot be placed on.
        /// </summary>
        [Tooltip("Which layers are obstacles that nodes cannot be placed on.")]
        [SerializeField]
        private LayerMask obstacleLayers;

        /// <summary>
        /// Lookup table to save and load navigation.
        /// </summary>
        [Tooltip("Lookup table to save and load navigation.")]
        [SerializeField]
        private EasyLookupTable lookupTable;
        
        /// <summary>
        /// One of the corner coordinates (X, Z) of the area to generate nodes in.
        /// </summary>
        [Tooltip("One of the corner coordinates (X, Z) of the area to generate nodes in.")]
        [SerializeField]
        private int2 corner1 = new(5, 5);
    
        /// <summary>
        /// One of the corner coordinates (X, Z) of the area to generate nodes in.
        /// </summary>
        [Tooltip("One of the corner coordinates (X, Z) of the area to generate nodes in.")]
        [SerializeField]
        private int2 corner2 = new(-5, -5);

        /// <summary>
        /// The floor and ceiling of the area to generate nodes in.
        /// </summary>
        [Tooltip("The floor and ceiling of the area to generate nodes in.")]
        [SerializeField]
        private float2 floorCeiling = new(-1, 10);

        /// <summary>
        /// How many nodes to place for every unit of world space. Example values:
        /// 1 - Node per every 1 unit.
        /// 2 - Node per every 0.5 units.
        /// 4 - Node per every 0.25 units.
        /// </summary>
        [Tooltip(
            "How many nodes to place for every unit of world space. Example values:\n" +
            "1 - Node per every 1 unit.\n" +
            "2 - Node per every 0.5 units.\n" +
            "4 - Node per every 0.25 units."
        )]
        [Min(1)]
        [SerializeField]
        private int nodesPerUnit = 4;

        /// <summary>
        /// How far away from corners should the nodes be placed.
        /// </summary>
        [field: Tooltip("How far away from corners should the nodes be placed.")]
        [field: SerializeField]
        [field: Min(0)]
        public int CornerSteps { get; private set; } = 3;        
        
        /// <summary>
        /// How close an agent can be to a location its seeking or pursuing to declare it as reached. Set negative for none.
        /// </summary>
        [Tooltip("How close an agent can be to a location its seeking or pursuing to declare it as reached. Set negative for none.")]
        [SerializeField]
        private float seekDistance = 0.1f;

        /// <summary>
        /// How far an agent can be to a location its fleeing or evading from to declare it as reached. Set negative for none.
        /// </summary>
        [Tooltip("How far an agent can be to a location its fleeing or evading from to declare it as reached. Set negative for none.")]
        [SerializeField]
        private float fleeDistance = 10f;

        /// <summary>
        /// If an agent is not moving, ensure it comes to a complete stop when its velocity is less than this.
        /// </summary>
        [Tooltip("If an agent is not moving, ensure it comes to a complete stop when its velocity is less than this.")]
        [Min(0)]
        [SerializeField]
        private float restVelocity = 0.1f;

        /// <summary>
        /// The radius of agents. This is for connecting navigation nodes to ensure enough space for movement.
        /// </summary>
        [Tooltip("The radius of agents. This is for connecting navigation nodes to ensure enough space for movement.")]
        [Min(0)]
        [SerializeField]
        private float agentRadius = 0.5f;

        /// <summary>
        /// The maximum number of messages any component can hold.
        /// </summary>
        [Tooltip("The maximum number of messages any component can hold.")]
        [Min(0)]
        [SerializeField]
        private int maxMessages = 100;
        
        /// <summary>
        /// How wide the details list is. Set to zero to disable details list rendering.
        /// </summary>
        [Header("UI")]
        [Tooltip("How wide the details list is. Set to zero to disable details list rendering.")]
        [Min(0)]
        [SerializeField]
        private float detailsWidth = 500;
        
        /// <summary>
        /// How wide the controls list is. Set to zero to disable controls list rendering.
        /// </summary>
        [Tooltip("How wide the controls list is. Set to zero to disable controls list rendering.")]
        [Min(0)]
        [SerializeField]
        private float controlsWidth = 120;

        /// <summary>
        /// Lock tracking cameras to the best agent.
        /// </summary>
        [Tooltip("Lock tracking cameras to the best agent.")]
        [SerializeField]
        private bool followBest = true;
    
        /// <summary>
        /// The currently selected camera. Set this to start with that camera active. Leaving empty will default to the first camera by alphabetic order.
        /// </summary>
        [Header("Visualization")]
        [Tooltip("The currently selected camera. Set this to start with that camera active. Leaving empty will default to the first camera by alphabetic order.")]
        [SerializeField]
        private Camera selectedCamera;
    
        /// <summary>
        /// Determine what path lines are drawn.
        /// Off - No lines are drawn.
        /// All - Lines for every move, navigation, and connection is drawn.
        /// Active - Lines for every move and navigation drawn.
        /// Selected - Only lines for the moves and navigation of the selected agent are drawn.
        /// </summary>
        [Tooltip(
            "Determine what path lines are drawn.\n" +
            "Off - No lines are drawn.\n" +
            "All - Lines for every move, navigation, and connection is drawn.\n" +
            "Active - Lines for every move and navigation drawn.\n"+
            "Selected - Only lines for the moves and navigation of the selected agent are drawn."
        )]
        [SerializeField]
        private PathState paths = PathState.Selected;

        /// <summary>
        /// All cameras in the scene.
        /// </summary>
        private Camera[] _cameras = Array.Empty<Camera>();
        
        /// <summary>
        /// The global messages.
        /// </summary>
        private readonly List<string> _globalMessages = new();

        /// <summary>
        /// All agents which move during an update tick.
        /// </summary>
        private readonly List<EasyAgent> _updateAgents = new();

        /// <summary>
        /// All agents which move during a fixed update tick.
        /// </summary>
        private readonly List<EasyAgent> _fixedUpdateAgents = new();
    
        /// <summary>
        /// State of the GUI system.
        /// </summary>
        private GuiState _state;

        /// <summary>
        /// The agent which is currently thinking.
        /// </summary>
        private int _currentAgentIndex;

        /// <summary>
        /// How many node spaces there are on the X axis.
        /// </summary>
        private int RangeX => (corner1.x - corner2.x) * nodesPerUnit + 1;
    
        /// <summary>
        /// How many node spaces there are on the Z axis.
        /// </summary>
        private int RangeZ => (corner1.y - corner2.y) * nodesPerUnit + 1;

        /// <summary>
        /// Data map.
        /// </summary>
        private char[,] _data;

        /// <summary>
        /// The nodes.
        /// </summary>
        private readonly List<Vector3> _nodes = new();

        /// <summary>
        /// True if the scene is taking a single time step.
        /// </summary>
        private bool _stepping;

        /// <summary>
        /// If the details menu is currently open.
        /// </summary>
        private bool _detailsOpen = true;

        /// <summary>
        /// If the controls menu is currently open.
        /// </summary>
        private bool _controlsOpen = true;

        /// <summary>
        /// The line renderer that has last been used.
        /// </summary>
        private int _currentLineRenderer;

        /// <summary>
        /// Get the distance of a path.
        /// </summary>
        /// <param name="path">The path to get the distance of.</param>
        /// <returns>The distance of the path.</returns>
        public static float PathLength(List<Vector3> path)
        {
            float distance = 0;
            for (int i = 1; i < path.Count; i++)
            {
                distance += Vector3.Distance(path[i - 1], path[i]);
            }

            return distance;
        }

        /// <summary>
        /// Lookup a path to take from a starting position to an end goal.
        /// </summary>
        /// <param name="position">The starting position.</param>
        /// <param name="goal">The end goal position.</param>
        /// <returns>A list of the points to move to reach the goal destination.</returns>
        public static List<Vector3> LookupPath(Vector3 position, Vector3 goal)
        {
            // Check if there is a direct line of sight, so we can skip pathing and just move directly towards the goal.
            // Also, if there are no nodes in the lookup table simply return the end goal position.
            if (!HitObstacle(position, goal) || Singleton.lookupTable == null || Singleton.lookupTable.Paths.Length == 0)
            {
                return new() { goal };
            }

            // Start at the node that is closest to the goal but visible from the current position.
            int positionIndex = Best(goal, position);
            
            // End at the node that is closest to and visible from the goal position.
            int goalIndex = Best(position, goal);

            // Add the starting index to the path.
            List<int> path = new() { positionIndex };

            // Loop until the path is finished or the end goal cannot be reached.
            while (true)
            {
                try
                {
                    // Get the next node to move to.
                    int shifted = goalIndex;
                    if (goalIndex > positionIndex)
                    {
                        shifted--;
                    }
                
                    // Move to the next node and add it to the path.
                    positionIndex = Singleton.lookupTable.Paths[positionIndex].goal[shifted];
                    path.Add(positionIndex);
                    
                    // If the node is the goal destination, all nodes in the path have been finished so stop the loop.
                    if (positionIndex == goalIndex)
                    {
                        break;
                    }
                }
                catch
                {
                    // Should never happen but just in case an invalid index was attempted.
                    break;
                }
            }

            // List to convert indexes to positions, seeded with the actual goal as it may not be directly on a node.
            List<Vector3> positions = new(path.Count + 1) { goal };

            // Build path in reverse so only one reverse call is needed when string pulling.
            for (int i = path.Count - 1; i >= 0; i--)
            {
                positions.Add(Singleton.lookupTable.Nodes[path[i]]);
            }

            // Try to pull the string from both sides.
            StringPull(positions);
            positions.Reverse();
            StringPull(positions);

            return positions;
        }
        
        /// <summary>
        /// Perform string pulling to shorten a path. Path list does not need to be returned, simply remove nodes from it.
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
                    // Remove connections that are not needed.
                    if (!HitObstacle(path[i], path[j]))
                    {
                        path.RemoveAt(j-- - 1);
                    }
                }
            }
        }

        /// <summary>
        /// Check if there is any obstacles between two positions.
        /// </summary>
        /// <param name="a">The first position.</param>
        /// <param name="b">The second position.</param>
        /// <returns>True if an obstacle was hit, false otherwise.</returns>
        public static bool HitObstacle(Vector3 a, Vector3 b)
        {
            // Offset above the ground, mainly for sphere casting.
            a.y += Singleton.agentRadius;
            b.y += Singleton.agentRadius;

            // Always do a simple line cast which ensures not clipping inside of close walls, and also do a sphere cast if there is a set navigation radius.
            return Physics.Linecast(a, b, Singleton.obstacleLayers) || (Singleton.agentRadius > 0 && Physics.SphereCast(a, Singleton.agentRadius, (b - a).normalized, out _, Vector3.Distance(a, b), Singleton.obstacleLayers));
        }

        /// <summary>
        /// Find the best node index for a position with it having sight of a position and the closest to another.
        /// </summary>
        /// <param name="goal">The position to find a node with line of sight to.</param>
        /// <param name="start">The position which will later try to find a path towards.</param>
        /// <returns>The index of the nearest node.</returns>
        private static int Best(Vector3 goal, Vector3 start)
        {
            // Try to find the most ideal nodes being the ones with the lowest total distance between both positions.
            List<Vector3> potential = Singleton.lookupTable.Nodes.OrderBy(n => Vector3.Distance(n, goal) + Vector3.Distance(n, start)).ToList();
            foreach (Vector3 node in potential.Where(node => !HitObstacle(start, node)))
            {
                return Array.IndexOf(Singleton.lookupTable.Nodes, node);
            }

            // If no nodes are in line of sight, return the closest node.
            return Array.IndexOf(Singleton.lookupTable.Nodes,Singleton.lookupTable.Nodes.OrderBy(n => Vector3.Distance(n, start)).First());
        }

        /// <summary>
        /// Lookup a state type from the dictionary.
        /// </summary>
        /// <typeparam name="T">The type of state to register</typeparam>
        /// <returns>The state of the requested type.</returns>
        public static EasyState GetState<T>() where T : EasyState
        {
            return RegisteredStates.ContainsKey(typeof(T)) ? RegisteredStates[typeof(T)] : CreateState<T>();
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
        protected static bool GuiButton(float x, float y, float w, float h, string message)
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
        public static void GlobalLog(string message)
        {
            if (Singleton._globalMessages.Count > 0 && Singleton._globalMessages[0] == message)
            {
                return;
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
        /// <param name="easyAgent">The agent to add.</param>
        public static void AddAgent(EasyAgent easyAgent)
        {
            // Ensure the agent is only added once.
            if (Singleton.Agents.Contains(easyAgent))
            {
                return;
            }
            
            // Add to their movement handling list.
            Singleton.Agents.Add(easyAgent);
            switch (easyAgent)
            {
                case EasyTransformAgent updateAgent:
                    Singleton._updateAgents.Add(updateAgent);
                    break;
                case EasyRigidbodyAgent fixedUpdateAgent:
                    Singleton._fixedUpdateAgents.Add(fixedUpdateAgent);
                    break;
            }
            
            // If the agent had any cameras attached to it, we need to add them.
            FindCameras();
            
            CheckGizmos();
        }

        /// <summary>
        /// Remove an agent from the agent manager.
        /// </summary>
        /// <param name="easyAgent">The agent to remove.</param>
        public static void RemoveAgent(EasyAgent easyAgent)
        {
            // This should always be true as agents are added at their creation but check just in case.
            if (!Singleton.Agents.Contains(easyAgent))
            {
                return;
            }

            // Remove the agent and update the current index accordingly so no agents are skipped in Update.
            int index = Singleton.Agents.IndexOf(easyAgent);
            Singleton.Agents.Remove(easyAgent);
            if (Singleton._currentAgentIndex > index)
            {
                Singleton._currentAgentIndex--;
            }
            if (Singleton._currentAgentIndex < 0 || Singleton._currentAgentIndex >= Singleton.Agents.Count)
            {
                Singleton._currentAgentIndex = 0;
            }

            // Remove from their movement handling list.
            switch (easyAgent)
            {
                case EasyTransformAgent updateAgent:
                {
                    if (Singleton._updateAgents.Contains(updateAgent))
                    {
                        Singleton._updateAgents.Remove(updateAgent);
                    }

                    break;
                }
                case EasyRigidbodyAgent fixedUpdateAgent:
                {
                    if (Singleton._fixedUpdateAgents.Contains(fixedUpdateAgent))
                    {
                        Singleton._fixedUpdateAgents.Remove(fixedUpdateAgent);
                    }

                    break;
                }
            }

            // If the agent had any cameras attached to it, we need to remove them.
            FindCameras();
            
            CheckGizmos();
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
            Singleton._cameras = FindObjectsByType<Camera>(FindObjectsSortMode.None).OrderBy(c => c.name).ToArray();
        }
        
        /// <summary>
        /// Register a state type into the dictionary for future reference.
        /// </summary>
        /// <param name="easyStateToAdd">The state itself.</param>
        /// <typeparam name="T">The type of state to register</typeparam>
        private static void RegisterState<T>(EasyState easyStateToAdd) where T : EasyState
        {
            RegisteredStates[typeof(T)] = easyStateToAdd;
        }

        /// <summary>
        /// Create a state if there was not one within the dictionary.
        /// </summary>
        /// <typeparam name="T">The type of state to register</typeparam>
        /// <returns>The state instance that was created</returns>
        private static EasyState CreateState<T>() where T : EasyState
        {
            RegisterState<T>(ScriptableObject.CreateInstance(typeof(T)) as EasyState);
            return RegisteredStates[typeof(T)];
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
            if (next < 0)
            {
                next = scenes - 1;
            }

            SceneManager.LoadScene(next);
        }

        /// <summary>
        /// OnGUI is called for rendering and handling GUI events.
        /// </summary>
        private void OnGUI()
        {
            Render(10, 10, 20, 5);
        }
        
        private void OnRenderObject()
        {
            // We don't want to make rendering calls if there is no need because it can be a problem on certain platforms like web.
            if (Agents.Count == 0 || paths is PathState.Off)
            {
                return;
            }

            // Check if there is any paths to render.
            switch (paths)
            {
                case PathState.All:
                    if ((lookupTable != null && lookupTable.Connections.Length > 0) || Agents.Any(a => a.Path.Count > 0))
                    {
                        break;
                    }
                    return;
                case PathState.Active:
                {
                    if (Agents.Any(a => a.Path.Count > 0))
                    {
                        break;
                    }

                    return;
                }
                case PathState.Off:
                case PathState.Selected:
                default:
                {
                    if (SelectedAgent != null && SelectedAgent.Path.Count > 0)
                    {
                        break;
                    }

                    return;
                }
            }
            
            _lineMaterial.SetPass(0);

            GL.PushMatrix();
            GL.MultMatrix(Matrix4x4.identity);
            GL.Begin(GL.LINES);

            // Render all nodes as white if they should be.
            if (paths == PathState.All && lookupTable != null)
            {
                GL.Color(Color.white);
                foreach (EasyConnection connection in lookupTable.Connections)
                {
                    Vector3 a = Singleton.lookupTable.Nodes[connection.A];
                    a.y += NavigationVisualOffset;
                    Vector3 b = Singleton.lookupTable.Nodes[connection.B];
                    b.y += NavigationVisualOffset;
                    GL.Vertex(a);
                    GL.Vertex(b);
                }
            }

            // Render active nodes in green for either all agents or only the selected agent.
            if (paths is not PathState.Selected)
            {
                foreach (EasyAgent agent in Agents)
                {
                    AgentGizmos(agent);
                }
            }
            else if (SelectedAgent != null)
            {
                AgentGizmos(SelectedAgent);
            }

            GL.End();
            GL.PopMatrix();
        }
        
        /// <summary>
        /// Display gizmos for an agent.
        /// </summary>
        /// <param name="agent">The agent to display gizmos for.</param>
        private static void AgentGizmos(EasyAgent agent)
        {
            Transform agentTransform = agent.transform;
            Vector3 position = agentTransform.position;
            Quaternion rotation = agentTransform.rotation;
            
            // If the agent is moving, draw a yellow line indicating the direction it is currently moving in.
            if (agent.moveAcceleration > 0 && agent.MoveVelocity != Vector2.zero)
            {
                GL.Color(Color.yellow);
                GL.Vertex(position);
                GL.Vertex(position + rotation * (agent.MoveVelocity3.normalized * 2));
            }

            // Display the path the agent is following.
            if (agent.Path.Count < 1)
            {
                return;
            }

            GL.Color(EasySteering.GizmosColor(agent.MoveType));
            GL.Vertex(position);
            GL.Vertex(agent.Path[0]);
            for (int i = 0; i < agent.Path.Count - 1; i++)
            {
                GL.Vertex(agent.Path[i]);
                GL.Vertex(agent.Path[i + 1]);
            }
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
        /// Override for custom detail rendering on the automatic GUI.
        /// </summary>
        /// <param name="x">X rendering position. In most cases this should remain unchanged.</param>
        /// <param name="y">Y rendering position. Update this with every component added and return it.</param>
        /// <param name="w">Width of components. In most cases this should remain unchanged.</param>
        /// <param name="h">Height of components. In most cases this should remain unchanged.</param>
        /// <param name="p">Padding of components. In most cases this should remain unchanged.</param>
        /// <returns>The updated Y position after all custom rendering has been done.</returns>
        protected virtual float DisplayDetails(float x, float y, float w, float h, float p)
        {
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
            y = Singleton.DisplayDetails(x, y, w, h, p);

            if (Singleton.SelectedAgent == null && Singleton._state == GuiState.Agent)
            {
                Singleton._state = GuiState.Main;
            }

            if (Singleton._state == GuiState.Main && Singleton.Agents.Count == 1)
            {
                Singleton.SelectedAgent = Singleton.Agents[0];
                Singleton._state = GuiState.Agent;
            }

            switch (Singleton._state)
            {
                // Handle agent view rendering.
                case GuiState.Agent:
                {
                    // Can only go to the main view if there is more than one agent.
                    if (Singleton.Agents.Count > 1)
                    {
                        // Button to go back to the main view.
                        y = NextItem(y, h, p);
                        if (GuiButton(x, y, w, h, "Back to Agent List" + (Singleton.followBest ? " - Stop Following Best" : string.Empty)))
                        {
                            Singleton.followBest = false;
                            Singleton._state = GuiState.Main;
                        }
                    }
                
                    RenderAgent(x, y, w, h, p);

                    return;
                }
                // Handle components view rendering.
                case GuiState.Components:
                {
                    if (Singleton.SelectedAgent != null)
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
                    }
                    else
                    {
                        Singleton._state = GuiState.Main;
                    }

                    return;
                }
                case GuiState.Main:
                default:
                    break;
            }

            // Display all agents.
            y = NextItem(y, h, p);
            GuiBox(x, y, w, h, p, 1);
            GuiLabel(x, y, w, h, p, $"{Singleton.Agents.Count} Agents");

            foreach (EasyAgent agent in Singleton.Agents.OrderBy(z => z.name))
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
            int length = 1;
            if (Singleton.Agents.Count > 1)
            {
                length++;
            }

            if (Singleton.SelectedAgent.State != null)
            {
                length++;
            }

            if (Singleton.SelectedAgent.performanceMeasure != null)
            {
                length++;
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
        
            if (Singleton.SelectedAgent.performanceMeasure != null)
            {
                GuiLabel(x, y, w, h, p, $"Performance: {Singleton.SelectedAgent.Performance}");
                y = NextItem(y, h, p);
            }

            if (Singleton.SelectedAgent.Destination != null)
            {
                Vector3 destination = Singleton.SelectedAgent.Destination.Value;
                string toFrom = EasySteering.IsApproachingBehaviour(Singleton.SelectedAgent.MoveType) ? "to" : "from";
                string tr = Singleton.SelectedAgent.MoveTarget != null ? Singleton.SelectedAgent.MoveTarget.name : $"({destination.x}, {destination.z})";
                GuiLabel(x, y, w, h, p, $"{Singleton.SelectedAgent.MoveType} {toFrom} {tr}");
            }
            else
            {
                GuiLabel(x, y, w, h, p, "Not moving");
            }

            // Display any custom details implemented for the agent.
            y = Singleton.SelectedAgent.DisplayDetails(x, y, w, h, p);

            // Display all sensors for the agent.
            if (Singleton.SelectedAgent.sensors.Length > 0 || Singleton.SelectedAgent.actuators.Length > 0)
            {
                y = NextItem(y, h, p);
                if (GuiButton(x, y, w, h, "Sensors and Actuators"))
                {
                    Singleton._state = GuiState.Components;
                }
            }

            if (!Singleton.SelectedAgent.HasMessages)
            {
                return;
            }
            
            // Render all messages for the agent.
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
            GuiLabel(x, y, w, h, p, Singleton.SelectedAgent.sensors.Length switch
            {
                0 => "No Sensors",
                1 => "1 Sensor",
                _ => $"{Singleton.SelectedAgent.sensors.Length} Sensors"
            });

            if (Singleton.SelectedAgent.sensors.Length > 0)
            {
                y = NextItem(y, h, p);
                GuiBox(x, y, w, h, p, Singleton.SelectedAgent.sensors.Length);
                for (int i = 0; i < Singleton.SelectedAgent.sensors.Length; i++)
                {
                    GuiLabel(x, y, w, h, p, Singleton.SelectedAgent.sensors[i].ToString());
                    if (i < Singleton.SelectedAgent.sensors.Length - 1)
                    {
                        y = NextItem(y, h, p);
                    }
                }
            }
            
            // Display all actuators.
            y = NextItem(y, h, p);
            GuiBox(x, y, w, h, p, 1);
            GuiLabel(x, y, w, h, p, Singleton.SelectedAgent.actuators.Length switch
            {
                0 => "No Actuators",
                1 => "1 Actuator",
                _ => $"{Singleton.SelectedAgent.actuators.Length} Actuators"
            });

            if (Singleton.SelectedAgent.actuators.Length < 1)
            {
                return;
            }
            
            y = NextItem(y, h, p);
            GuiBox(x, y, w, h, p, Singleton.SelectedAgent.actuators.Length);
            for (int i = 0; i < Singleton.SelectedAgent.actuators.Length; i++)
            {
                GuiLabel(x, y, w, h, p, Singleton.SelectedAgent.actuators[i].ToString());
                if (i < Singleton.SelectedAgent.actuators.Length - 1)
                {
                    y = NextItem(y, h, p);
                }
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
                if (GuiButton(x, y, w, h, Singleton.followBest ? "Stop Following" : "Follow Best"))
                {
                    Singleton.followBest = !Singleton.followBest;
                    if (Singleton.followBest && Singleton._state == GuiState.Main)
                    {
                        Singleton._state = GuiState.Agent;
                    }
                }
            
                y = NextItem(y, h, p);
            }
            else
            {
                Singleton.followBest = false;
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

            if ((Singleton.lookupTable != null && Singleton.lookupTable.Paths.Length > 0) || Singleton.Agents.Count > 0)
            {
                // Button to change paths rendering..
                y = NextItem(y, h, p);
                if (GuiButton(x, y, w, h, Singleton.paths switch
                {
                    PathState.Off => "Gizmos: Off",
                    PathState.Active => "Gizmos: Active",
                    PathState.All => "Gizmos: All",
                    _ => "Gizmos: Selected"
                }))
                {
                    ChangeGizmos();
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
            
#if (!UNITY_EDITOR && !UNITY_WEBGL)
            // Button to quit.
            y = NextItem(y, h, p);
            if (GuiButton(x, y, w, h, "Quit"))
            {
                Application.Quit();
            }
#endif
        }

        /// <summary>
        /// Change the gizmo mode.
        /// </summary>
        private static void ChangeGizmos()
        {
            Singleton.paths++;

            CheckGizmos();
        }

        /// <summary>
        /// Ensure gizmos are valid.
        /// </summary>
        private static void CheckGizmos()
        {
            bool change = true;
            while (change)
            {
                change = false;
                if (Singleton.paths == PathState.All && Singleton.Agents.Count == 0)
                {
                    change = true;
                    Singleton.paths++;
                }

                if (Singleton.paths == PathState.Selected && Singleton.Agents.Count == 1)
                {
                    change = true;
                    Singleton.paths++;
                }
            
                if (Singleton.paths > PathState.Selected)
                {
                    change = true;
                    Singleton.paths = PathState.Off;
                }
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
        /// Clear all messages.
        /// </summary>
        protected static void ClearMessages()
        {
            Singleton._globalMessages.Clear();
            foreach (EasyAgent agent in Singleton.Agents)
            {
                agent.Messages.Clear();
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

            foreach (EasyAgent agent in FindObjectsByType<EasyAgent>(FindObjectsSortMode.None))
            {
                AddAgent(agent);
            }
        }
        
        /// <summary>
        /// Bake navigation data.
        /// </summary>
#if UNITY_EDITOR
        [MenuItem("Easy-AI/Bake Navigation")]
        public static void BakeNavigation()
        {
            if (Application.isPlaying)
            {
                Debug.LogError("Can't bake in play mode.");
                return;
            }
            
            Stopwatch stopwatch = new();
            stopwatch.Start();
            
            Singleton = FindAnyObjectByType<EasyManager>();
            if (Singleton == null)
            {
                Debug.LogError("No manager found in the scene.");
                return;
            }

            if (Singleton.lookupTable == null)
            {
                Debug.LogError("No lookup table attached to the manager.");
                return;
            }

            Singleton._nodes.Clear();
            
            // Ensure X coordinates are in the required order.
            if (Singleton.corner2.x > Singleton.corner1.x)
            {
                (Singleton.corner1.x, Singleton.corner2.x) = (Singleton.corner2.x, Singleton.corner1.x);
            }
        
            // Ensure Z coordinates are in the required order.
            if (Singleton.corner2.y > Singleton.corner1.y)
            {
                (Singleton.corner1.y, Singleton.corner2.y) = (Singleton.corner2.y, Singleton.corner1.y);
            }

            // Ensure floor and ceiling are in the required order.
            if (Singleton.floorCeiling.x > Singleton.floorCeiling.y)
            {
                (Singleton.floorCeiling.x, Singleton.floorCeiling.y) = (Singleton.floorCeiling.y, Singleton.floorCeiling.x);
            }

            // Initialize the data table.
            Singleton._data = new char[Singleton.RangeX, Singleton.RangeZ];
        
            // Scan each position to determine if it is open or closed.
            for (int x = 0; x < Singleton.RangeX; x++)
            {
                for (int z = 0; z < Singleton.RangeZ; z++)
                {
                    float2 pos = Singleton.GetRealPosition(x, z);
                    Singleton._data[x, z] = Physics.Raycast(new(pos.x, Singleton.floorCeiling.y, pos.y), Vector3.down, out RaycastHit hit, Singleton.floorCeiling.y - Singleton.floorCeiling.x, GroundLayers | ObstacleLayers) && (GroundLayers.value & (1 << hit.transform.gameObject.layer)) > 0 ? Open : Closed;
                }
            }

            // Check all X coordinates, skipping the padding required.
            for (int x = Singleton.CornerSteps * 2; x < Singleton.RangeX - Singleton.CornerSteps * 2; x++)
            {
                // Check all Z coordinates, skipping the padding required.
                for (int z = Singleton.CornerSteps * 2; z < Singleton.RangeZ - Singleton.CornerSteps * 2; z++)
                {
                    EasyCornerGraph.Perform(Singleton, x, z);
                }
            }

            // Ensure the folder to save the map data exists.
            const string folder = "Maps";
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            
            if (Directory.Exists(folder))
            {
                // Write to the file.
                StreamWriter writer = new($"{folder}/{SceneManager.GetActiveScene().name}.txt", false);
                writer.Write(Singleton.ToString());
                writer.Close();
            }

            Singleton._nodes.AddRange(FindObjectsByType<EasyNode>(FindObjectsSortMode.None).Select(node => node.transform.position));

            List<EasyVectorConnection> raw = new();

            // Setup all freely-placed nodes.
            for (int i = 0; i < Singleton._nodes.Count; i++)
            {
                for (int j = i + 1; j < Singleton._nodes.Count; j++)
                {
                    // If a clear path, add the connection.
                    if (!HitObstacle(Singleton._nodes[i], Singleton._nodes[j]))
                    {
                        raw.Add(new(Singleton._nodes[i], Singleton._nodes[j]));
                    }
                }
            }

            // If any nodes are not a part of any connections, remove them.
            for (int i = 0; i < Singleton._nodes.Count; i++)
            {
                if (!raw.Any(c => c.A == Singleton._nodes[i] || c.B == Singleton._nodes[i]))
                {
                    Singleton._nodes.RemoveAt(i--);
                }
            }

            // Convert the connections to index-based lookup values.
            List<EasyConnection> connections = raw.Select(connection => new EasyConnection(Singleton._nodes.IndexOf(connection.A), Singleton._nodes.IndexOf(connection.B))).ToList();

            // Store all new lookup tables and a helper variable to flag which lookups are properly set.
            EasyPath[] lookups = new EasyPath[Singleton._nodes.Count];
            bool[][] set = new bool[lookups.Length][];
            for (int i = 0; i < lookups.Length; i++)
            {
                lookups[i].goal = new int[Singleton._nodes.Count - 1];
                set[i] = new bool[lookups[i].goal.Length];
            }

            // Create helper class to help with A*.
            EasyAStarPaths paths = new(raw);

            try
            {
                // Loop through all nodes.
                System.Threading.Tasks.Parallel.For(0, Singleton._nodes.Count, i =>
                {
                    // Loop through all nodes again so pathfinding can be done on each pair.
                    for (int j = i + 1; j < Singleton._nodes.Count; j++)
                    {
                        // Get the A* path from one node to another.
                        EasyAStarNode node = EasyAStar.Perform(new() {new(Singleton._nodes[i], Singleton._nodes[j])}, Singleton._nodes[j], paths);

                        if (node == null)
                        {
                            throw new("Always return a node even if destination can't be reached. Return the closest node when unable to reach.");
                        }

                        // Go from the last to node to the first adding all positions to the path.
                        List<Vector3> path = new();
                        while (node != null)
                        {
                            // Ensure no duplicates in the path.
                            if (!path.Contains(node.Position))
                            {
                                path.Add(node.Position);
                            }

                            node = node.Previous;
                        }

                        // Ensure multithreading does not add duplicate values.
                        lock (lookups)
                        {
                            // Loop through all nodes in the path and add them to the lookup table.
                            for (int k = 0; k < path.Count - 1; k++)
                            {
                                // Forward pass.
                                AddLookup(Singleton._nodes, lookups, path, k, i, k + 1, set);

                                // Backwards pass since it is the same path in reverse.
                                AddLookup(Singleton._nodes, lookups, path, path.Count - 1 - k, j, path.Count - 2 - k, set);
                            }
                        }
                    }
                });
            }
            catch (Exception e)
            {
                Debug.LogError(e.Message);
            }

            // Compare how many lookups are expected with how many were actually defined to ensure the pathfinding is valid.
            int expected = Singleton._nodes.Count * (Singleton._nodes.Count - 1);
            int created = set.Sum(t => t.Count(t1 => t1));
            
            // Write the lookup table to a file for fast reading on future runs.
            // To avoid navigation errors, the lookups are only stored if they are valid.
            Singleton.lookupTable.Write(Singleton._nodes, connections, expected == created ? lookups : Array.Empty<EasyPath>());
            
            stopwatch.Stop();

            if (expected == created)
            {
                Debug.Log($"{Singleton._nodes.Count} Nodes | {raw.Count} Connections | {created} Lookups | {stopwatch.Elapsed}");
            }
            else
            {
                Debug.LogError($"{Singleton._nodes.Count} Nodes | {raw.Count} Connections | {expected} Expected Lookups | {created} Created Lookups | {stopwatch.Elapsed}");
            }
            
            // Select the lookup table in the inspector for easily checking it.
            Selection.SetActiveObjectWithContext(Singleton.lookupTable, Singleton.lookupTable);
        }
#endif

        /// <summary>
        /// Helper method to add a point on a navigation path to the lookup table.
        /// </summary>
        /// <param name="nodes">The nodes being built from.</param>
        /// <param name="lookups">The lookup table being built.</param>
        /// <param name="path">The path currently being added.</param>
        /// <param name="current">The current index.</param>
        /// <param name="goal">The goal index.</param>
        /// <param name="next">The next index.</param>
        /// <param name="set">Helper to track how many lookups are set.</param>
        private static void AddLookup(IList<Vector3> nodes, IList<EasyPath> lookups, IReadOnlyList<Vector3> path, int current, int goal, int next, bool[][] set)
        {
            current = nodes.IndexOf(path[current]);
            next = nodes.IndexOf(path[next]);

            if (goal > current)
            {
                goal--;
            }

            lookups[current].goal[goal] = next;
            set[current][goal] = true;
        }

        /// <summary>
        /// Get the actual coordinate from the node generator indexes.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="z">The Z coordinate.</param>
        /// <returns>The real (X, Z) position.</returns>
        private float2 GetRealPosition(int x, int z)
        {
            return new(corner2.x + x * 1f / nodesPerUnit, corner2.y + z * 1f / nodesPerUnit);
        }

        /// <summary>
        /// Check if a given coordinate is open.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="z">The Z coordinate.</param>
        /// <returns>True if the space is open, false otherwise.</returns>
        public bool IsOpen(int x, int z)
        {
            return x >= 0 && x < RangeX && z >= 0 && z < RangeZ && _data[x, z] != Closed;
        }
        
        /// <summary>
        /// Add a node at a given position.
        /// </summary>
        /// <param name="x">The X coordinate.</param>
        /// <param name="z">The Z coordinate.</param>
        public void AddNode(int x, int z)
        {
            // If out of bounds or already opened, nothing to do.
            if (x < 0 || x >= RangeX || z < 0 || z >= RangeZ || _data[x, z] == Node)
            {
                return;
            }
            
            // Set that it is a node in the map data.
            _data[x, z] = Node;
        
            // Get the position of the node.
            float2 pos = GetRealPosition(x, z);
            float y = floorCeiling.x;
            if (Physics.Raycast(new(pos.x, floorCeiling.y, pos.y), Vector3.down, out RaycastHit hit, floorCeiling.y - floorCeiling.x, GroundLayers))
            {
                y = hit.point.y;
            }
        
            // Add the node.
            Vector3 v = new(pos.x, y, pos.y);
            if (!_nodes.Contains(v))
            {
                _nodes.Add(v);
            }
        }
        
        public override string ToString()
        {
            // Nothing to write if there is no data.
            if (_data == null)
            {
                return "No data.";
            }

            // Add all map data.
            string s = string.Empty;
            for (int i = 0; i < RangeX; i++)
            {
                for (int j = 0; j < RangeZ; j++)
                {
                    s += _data[i, j];
                }

                if (i != RangeX - 1)
                {
                    s += '\n';
                }
            }

            return s;
        }

        /// <summary>
        /// Start is called on the frame when a script is enabled just before any of the Update methods are called the first time.
        /// </summary>
        protected virtual void Start()
        {
            // Clean up all navigation related components in the scene as they are no longer needed after generation.
            foreach (EasyNode node in FindObjectsByType<EasyNode>(FindObjectsSortMode.None).OrderBy(n => n.transform.childCount))
            {
                node.Remove();
            }
            
            CheckGizmos();

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
                FindCameras();
                SwitchCamera(_cameras[0]);
            }
        }

        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        protected virtual void Update()
        {
            if (Agents.Count == 1)
            {
                SelectedAgent = Agents[0];
            }

            // Click to select an agent.
            if (Mouse.current.leftButton.wasPressedThisFrame && Physics.Raycast(selectedCamera.ScreenPointToRay(new(Mouse.current.position.x.ReadValue(), Mouse.current.position.y.ReadValue(), 0)), out RaycastHit hit, Mathf.Infinity))
            {
                // See if an agent was actually hit with the click and select it if so.
                Transform tr = hit.collider.transform;
                do
                {
                    EasyAgent clicked = tr.GetComponent<EasyAgent>();
                    if (clicked != null)
                    {
                        SelectedAgent = clicked;
                        followBest = false;
                        _state = GuiState.Agent;
                        break;
                    }
                    tr = tr.parent;
                } while (tr != null);
            }

            if (!followBest)
            {
                return;
            }

            // If locked to following the best agent, select the best agent.
            float best = float.MinValue;
            SelectedAgent = null;
            foreach (EasyAgent agent in Agents.Where(a => a.Alive && a.performanceMeasure != null))
            {
                float score = agent.performanceMeasure.CalculatePerformance();
                if (score <= best)
                {
                    continue;
                }

                best = score;
                SelectedAgent = agent;
            }

            if (SelectedAgent == null)
            {
                followBest = false;
                return;
            }

            if (Singleton._state == GuiState.Main)
            {
                Singleton._state = GuiState.Agent;
            }
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
        
        private void OnDrawGizmosSelected()
        {
            // Vertical lines.
            Gizmos.DrawLine(new(corner1.x, floorCeiling.x, corner1.y), new(corner1.x, floorCeiling.y, corner1.y));
            Gizmos.DrawLine(new(corner1.x, floorCeiling.x, corner2.y), new(corner1.x, floorCeiling.y, corner2.y));
            Gizmos.DrawLine(new(corner2.x, floorCeiling.x, corner1.y), new(corner2.x, floorCeiling.y, corner1.y));
            Gizmos.DrawLine(new(corner2.x, floorCeiling.x, corner2.y), new(corner2.x, floorCeiling.y, corner2.y));
        
            // Top horizontal lines.
            Gizmos.DrawLine(new(corner1.x, floorCeiling.y, corner1.y), new(corner1.x, floorCeiling.y, corner2.y));
            Gizmos.DrawLine(new(corner1.x, floorCeiling.y, corner1.y), new(corner2.x, floorCeiling.y, corner1.y));
            Gizmos.DrawLine(new(corner2.x, floorCeiling.y, corner2.y), new(corner1.x, floorCeiling.y, corner2.y));
            Gizmos.DrawLine(new(corner2.x, floorCeiling.y, corner2.y), new(corner2.x, floorCeiling.y, corner1.y));
        
            // Bottom horizontal lines.
            Gizmos.DrawLine(new(corner1.x, floorCeiling.x, corner1.y), new(corner1.x, floorCeiling.x, corner2.y));
            Gizmos.DrawLine(new(corner1.x, floorCeiling.x, corner1.y), new(corner2.x, floorCeiling.x, corner1.y));
            Gizmos.DrawLine(new(corner2.x, floorCeiling.x, corner2.y), new(corner1.x, floorCeiling.x, corner2.y));
            Gizmos.DrawLine(new(corner2.x, floorCeiling.x, corner2.y), new(corner2.x, floorCeiling.x, corner1.y));
        }
    }
}