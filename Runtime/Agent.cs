using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Base class for all agents.
/// </summary>
public abstract class Agent : MessageComponent
{
    /// <summary>
    /// The various move types available for agents.
    /// </summary>
    public enum MoveType : byte
    {
        Seek,
        Flee,
        Pursuit,
        Evade
    }

    /// <summary>
    /// Class to store all targets the agent is moving in relation to.
    /// </summary>
    public class MoveData
    {
        /// <summary>
        /// Store the position which is only used if the transform is null.
        /// </summary>
        private readonly Vector2 _position;
        
        /// <summary>
        /// How much time has elapsed since the last time this was called for predictive move types.
        /// </summary>
        public float DeltaTime { get; set; }
        
        /// <summary>
        /// The last position this was in since 
        /// </summary>
        public Vector2 LastPosition { get; set; }
        
        /// <summary>
        /// The movement vector for visualizing move data.
        /// </summary>
        public Vector2 MoveVector { get; set; } = Vector2.zero;
        
        /// <summary>
        /// The move type so proper behaviours can be performed.
        /// </summary>
        public MoveType MoveType { get; }

        /// <summary>
        /// The transform to move in relation to.
        /// </summary>
        public Transform Transform { get; }
        
        /// <summary>
        /// True if this move data was setup with a transform so if at any point the transform is destroyed this is removed as well.
        /// </summary>
        public bool IsTransformTarget { get; }
        
        /// <summary>
        /// Get the position of the transform if it has one otherwise the position it was set to have.
        /// </summary>
        public Vector2 Position
        {
            get
            {
                if (Transform == null)
                {
                    return _position;
                }

                Vector3 pos3 = Transform.position;
                return new Vector2(pos3.x, pos3.z);
            }
        }

        /// <summary>
        /// Create a move data for a transform.
        /// </summary>
        /// <param name="moveType">The move type.</param>
        /// <param name="transform">The transform.</param>
        public MoveData(MoveType moveType, Transform transform)
        {
            MoveType = moveType;
            Transform = transform;
            Vector3 pos3 = transform.position;
            _position = new Vector2(pos3.x, pos3.z);
            LastPosition = _position;
            IsTransformTarget = true;
        }

        /// <summary>
        /// Create a move data for a position.
        /// </summary>
        /// <param name="moveType">The move type.</param>
        /// <param name="position">The position.</param>
        public MoveData(MoveType moveType, Vector2 position)
        {
            // Since pursuit and evade are for moving objects and this is only with a static position,
            // switch pursuit to seek and evade to flee.
            moveType = moveType switch
            {
                MoveType.Pursuit => MoveType.Seek,
                MoveType.Evade => MoveType.Flee,
                _ => moveType
            };

            MoveType = moveType;
            Transform = null;
            _position = position;
            LastPosition = _position;
            IsTransformTarget = false;
        }
    }
    
    [Min(0)]
    [Tooltip("How fast this agent can move in units per second.")]
    public float moveSpeed = 10;
    
    [Min(0)]
    [Tooltip("How fast this agent can increase in move speed in units per second.")]
    public float moveAcceleration;

    [Tooltip("How close an agent can be to a location its seeking or pursuing to declare it as reached?. Set negative for none.")]
    public float seekAcceptableDistance = 0.1f;

    [Tooltip("How far an agent can be to a location its fleeing or evading from to declare it as reached?. Set negative for none.")]
    public float fleeAcceptableDistance = 10f;

    [Min(0)]
    [Tooltip("If the agent is not moving, ensure it comes to a complete stop when its velocity is less than this.")]
    public float restVelocity = 0.1f;
    
    [Min(0)]
    [Tooltip("How fast this agent can look in degrees per second.")]
    public float lookSpeed;

    [Min(0)]
    [Tooltip("How many degrees at max this agent could shift when wandering.")]
    public float maxWanderTurn = 30;

    [SerializeField]
    [Tooltip("The global state the agent is in. Initialize it with the global state to start it.")]
    private State globalState;
    
    [SerializeField]
    [Tooltip("The current state the agent is in. Initialize it with the state to start in.")]
    private State state;

    [SerializeField]
    [Min(0)]
    [Tooltip("The height to draw visuals looking from.")]
    private float sightHeight;

    /// <summary>
    /// The global state the agent is in.
    /// </summary>
    public State GlobalState
    {
        get => globalState;
        set
        {
            if (globalState != null)
            {
                globalState.Exit(this);
            }

            globalState = value;

            if (globalState != null)
            {
                globalState.Enter(this);
            }
        }
    }

    /// <summary>
    /// The state the agent is in.
    /// </summary>
    public State State
    {
        get => state;
        set
        {
            PreviousState = state;
            
            if (state != null)
            {
                state.Exit(this);
            }

            state = value;

            if (state != null)
            {
                state.Enter(this);
            }
        }
    }
    
    /// <summary>
    /// Whether this agent should wander when it has no other movement data types.
    /// </summary>
    public bool Wander { get; set; }

    /// <summary>
    /// The path destination.
    /// </summary>
    public Vector3? Destination => Path?[^1];

    /// <summary>
    /// The previous state the agent was in.
    /// </summary>
    public State PreviousState { get; private set; }

    /// <summary>
    /// The current move velocity if move acceleration is being used as a Vector3.
    /// </summary>
    public Vector3 MoveVelocity3 => new(MoveVelocity.x, 0, MoveVelocity.y);

    /// <summary>
    /// The current move velocity if move acceleration is being used.
    /// </summary>
    public Vector2 MoveVelocity { get; protected set; }
        
    /// <summary>
    /// The time passed since the last time the agent's mind made decisions. Use this instead of Time.DeltaTime.
    /// </summary>
    public float DeltaTime { get; set; }

    /// <summary>
    /// The target the agent is currently trying to look towards.
    /// </summary>
    public Vector3 LookTarget { get; private set; }

    /// <summary>
    /// True if the agent is trying to look to a target, false otherwise.
    /// </summary>
    public bool LookingToTarget { get; private set; }

    /// <summary>
    /// The performance measure of the agent.
    /// </summary>
    public float Performance { get; private set; }

    /// <summary>
    /// Get the currently selected mind of the agent.
    /// </summary>
    public Mind SelectedMind => Minds is { Length: > 0 } ? Minds[_selectedMindIndex] : null;
    
    /// <summary>
    /// The mind of this agent.
    /// </summary>
    public Mind[] Minds { get; private set; }

    /// <summary>
    /// The sensors of this agent.
    /// </summary>
    public Sensor[] Sensors { get; private set; }

    /// <summary>
    /// The percepts of this agent.
    /// </summary>
    public Percept[] Percepts { get; private set; }

    /// <summary>
    /// The actuators of this agent.
    /// </summary>
    public Actuator[] Actuators { get; private set; }

    /// <summary>
    /// The actions of this agent.
    /// </summary>
    public Action[] Actions { get; private set; }

    /// <summary>
    /// The root transform that holds the visuals for this agent used to rotate the agent towards its look target.
    /// </summary>
    public Transform Visuals { get; private set; }

    /// <summary>
    /// The performance measure of this agent.
    /// </summary>
    public PerformanceMeasure PerformanceMeasure { get; private set; }

    /// <summary>
    /// All movement the agent is doing without path finding.
    /// </summary>
    public List<MoveData> MovesData { get; private set; } = new();

    /// <summary>
    /// The current path an agent is following.
    /// </summary>
    public List<Vector3> Path { get; private set; }

    /// <summary>
    /// The index of the currently selected mind.
    /// </summary>
    private int _selectedMindIndex;

    /// <summary>
    /// Helper transform for storing the wander guide.
    /// </summary>
    private Transform _wanderRoot;

    /// <summary>
    /// Helper transform for storing the wander forward.
    /// </summary>
    private Transform _wanderForward;
    
    /// <summary>
    /// Display lines to highlight agent movement.
    /// </summary>
    public override void DisplayGizmos()
    {
        Vector3 position = transform.position;
        position.y += AgentManager.Singleton.navigationVisualOffset;

        if (Path == null || Path.Count == 0)
        {
            // Display the movement vectors of all move types.
            foreach (MoveData moveData in MovesData)
            {
                // Assign different colors for different behaviours:
                // Blue for seek, cyan for pursuit, red for flee, and orange for evade.
                GL.Color(moveData.MoveType switch
                {
                    MoveType.Seek => Color.blue,
                    MoveType.Pursuit => Color.cyan,
                    MoveType.Flee => Color.red,
                    _ => new Color(1f, 0.65f, 0f),
                });
            
                // Draw a line from the agent's position showing the force of this movement.
                GL.Vertex(position);
                GL.Vertex(position + transform.rotation * (new Vector3(moveData.MoveVector.x, position.y, moveData.MoveVector.y).normalized * 2));

                if (moveData.MoveType is MoveType.Seek or MoveType.Flee)
                {
                    continue;
                }
            
                // Draw another line from the agent's position to where the agent is seeking/pursuing/fleeing/evading to/from.
                GL.Vertex(position);
                GL.Vertex(new Vector3(moveData.Position.x, position.y, moveData.Position.y));
            }

            // If the agent is moving, draw a green line indicating the direction it is currently moving in.
            if (MoveVelocity != Vector2.zero)
            {
                GL.Color(Color.green);
                GL.Vertex(position);
                GL.Vertex(position + transform.rotation * (MoveVelocity3.normalized * 2));
            }
        }

        if (!LookingToTarget)
        {
            return;
        }

        // If the agent is looking towards a particular target (not just based on where it is moving), draw a line towards the target.
        GL.Color(Color.yellow);
        if (sightHeight > 0)
        {
            GL.Vertex(transform.position + new Vector3(0, sightHeight, 0));
        }
        else
        {
            GL.Vertex(position);
        }
        GL.Vertex(LookTarget);
    }

    /// <summary>
    /// Calculate a path towards a position.
    /// </summary>
    /// <param name="goal">The position to navigate to.</param>
    /// <returns>True if the path has been set, false if the agent was already navigating towards this point.</returns>
    public bool Navigate(Vector3 goal)
    {
        if (Destination == goal)
        {
            return false;
        }
        
        Path = AgentManager.Singleton.LookupPath(transform.position, goal);
        return true;
    }
    
    /// <summary>
    /// Clear the path.
    /// </summary>
    public void ClearPath()
    {
        Path = null;
    }

    /// <summary>
    /// Set a transform to move based upon.
    /// </summary>
    /// <param name="moveType">The move type.</param>
    /// <param name="tr">The transform.</param>
    public void SetMoveData(MoveType moveType, Transform tr)
    {
        MovesData.Clear();
        AddMoveData(moveType, tr);
    }

    /// <summary>
    /// Set a position to move based upon.
    /// </summary>
    /// <param name="moveType">The move type.</param>
    /// <param name="pos">The position.</param>
    public void SetMoveData(MoveType moveType, Vector3 pos)
    {
        MovesData.Clear();
        AddMoveData(moveType, pos);
    }

    /// <summary>
    /// Set a position to move based upon.
    /// </summary>
    /// <param name="moveType">The move type.</param>
    /// <param name="pos">The position.</param>
    public void SetMoveData(MoveType moveType, Vector2 pos)
    {
        MovesData.Clear();
        AddMoveData(moveType, pos);
    }
    
    /// <summary>
    /// Add a transform to move based upon.
    /// </summary>
    /// <param name="moveType">The move type.</param>
    /// <param name="tr">The transform.</param>
    public void AddMoveData(MoveType moveType, Transform tr)
    {
        if (MovesData.Exists(m => m.MoveType == moveType && m.Transform == tr) || IsCompleteMove(moveType, new Vector2(transform.position.x, transform.position.z), new Vector2(tr.position.x, tr.position.z)))
        {
            return;
        }

        Path = null;
        RemoveMoveData(tr);
        MovesData.Add(new MoveData(moveType, tr));
    }

    /// <summary>
    /// Add a position to move based upon.
    /// </summary>
    /// <param name="moveType">The move type.</param>
    /// <param name="pos">The position.</param>
    public void AddMoveData(MoveType moveType, Vector3 pos)
    {
        AddMoveData(moveType, new Vector2(pos.x, pos.z));
    }

    /// <summary>
    /// Add a position to move based upon.
    /// </summary>
    /// <param name="moveType">The move type.</param>
    /// <param name="pos">The position.</param>
    public void AddMoveData(MoveType moveType, Vector2 pos)
    {
        if (MovesData.Exists(m => m.MoveType == moveType && m.Transform == null && m.Position == pos) || IsCompleteMove(moveType, new Vector2(transform.position.x, transform.position.z), pos))
        {
            return;
        }
        
        Path = null;
        RemoveMoveData(pos);
        MovesData.Add(new MoveData(moveType, pos));
    }

    /// <summary>
    /// Clear all move data.
    /// </summary>
    public void ClearMoveData()
    {
        MovesData.Clear();
    }

    /// <summary>
    /// Remove move data for a transform.
    /// </summary>
    /// <param name="tr">The transform.</param>
    public void RemoveMoveData(Transform tr)
    {
        MovesData = MovesData.Where(m => m.Transform != tr).ToList();
    }

    /// <summary>
    /// Remove move data for a position.
    /// </summary>
    /// <param name="pos">The position.</param>
    public void RemoveMoveData(Vector3 pos)
    {
        RemoveMoveData(new Vector2(pos.x, pos.z));
    }

    /// <summary>
    /// Remove move data for a position.
    /// </summary>
    /// <param name="pos">The position.</param>
    public void RemoveMoveData(Vector2 pos)
    {
        MovesData = MovesData.Where(m => m.Transform == null && m.Position != pos).ToList();
    }

    /// <summary>
    /// Fire an event to an agent.
    /// </summary>
    /// <param name="receiver">The agent to send the event to.</param>
    /// <param name="eventId">The event ID which the receiver will use to identify the type of message.</param>
    /// <param name="details">Object which contains all data for this message.</param>
    /// <returns>True if the receiver handled the message, false otherwise.</returns>
    public bool FireEvent(Agent receiver, int eventId, object details = null)
    {
        return receiver != null && receiver != this && receiver.HandleEvent(new AIEvent(eventId, this, details));
    }

    /// <summary>
    /// Broadcast a message to all other agents.
    /// </summary>
    /// <param name="eventId">The event ID which the receivers will use to identify the type of message.</param>
    /// <param name="details">Object which contains all data for this message.</param>
    /// <param name="requireAll">Setting to true will check for all agents handling the message, false means only one agent needs to handle it.</param>
    /// <returns>If require all is true, true if all agents handle the message and false otherwise and if require all is false, true if at least one agent handles the message, false otherwise.</returns>
    public bool BroadcastEvent(int eventId, object details = null, bool requireAll = false)
    {
        bool all = true;
        bool one = false;
        foreach (bool result in AgentManager.Singleton.Agents.Where(a => a != this).Select(a => a.HandleEvent(new AIEvent(eventId, this, details))))
        {
            if (result)
            {
                one = true;
            }
            else
            {
                all = false;
            }
        }

        return requireAll ? all : one;
    }

    /// <summary>
    /// Assign a mind to this agent.
    /// </summary>
    /// <param name="type">The type of mind to assign.</param>
    public void AssignMind(Type type)
    {
        if (Minds == null || Minds.Length == 0 || type == null)
        {
            _selectedMindIndex = 0;
            return;
        }

        Mind mind = Minds.FirstOrDefault(m => m.GetType() == type);
        if (mind == null)
        {
            _selectedMindIndex = 0;
            return;
        }

        _selectedMindIndex = Minds.ToList().IndexOf(mind);
    }

    /// <summary>
    /// Assign a performance measure to this agent.
    /// </summary>
    /// <param name="performanceMeasure">The performance measure to assign.</param>
    public void AssignPerformanceMeasure(PerformanceMeasure performanceMeasure)
    {
        PerformanceMeasure = performanceMeasure;
        ConfigurePerformanceMeasure();
    }

    /// <summary>
    /// Resume looking towards the look target currently assigned to the agent.
    /// </summary>
    public void LookAtTarget()
    {
        LookingToTarget = LookTarget != transform.position;
    }

    /// <summary>
    /// Set a target position for the agent to look towards.
    /// </summary>
    /// <param name="target">The target position to look to.</param>
    public void LookAtTarget(Vector3 target)
    {
        LookTarget = target;
        LookAtTarget();
    }

    /// <summary>
    /// Set a target transform for the agent to look towards.
    /// </summary>
    /// <param name="target">The target transform to look to.</param>
    public void LookAtTarget(Transform target)
    {
        if (target == null)
        {
            StopLookAtTarget();
            return;
        }
            
        LookAtTarget(target.position);
    }

    /// <summary>
    /// Have the agent stop looking towards its look target.
    /// </summary>
    public void StopLookAtTarget()
    {
        LookingToTarget = false;
    }

    /// <summary>
    /// Instantly stop all actions this agent is performing.
    /// </summary>
    public void StopAllActions()
    {
        Actions = null;
    }

    /// <summary>
    /// Called by the AgentManager to have the agent sense, think, and act.
    /// </summary>
    public virtual void Perform()
    {
        if (globalState != null)
        {
            globalState.Execute(this);
        }

        if (state != null)
        {
            state.Execute(this);
        }

        // Can only sense, think, and act if there is a mind attached.
        if (Minds is { Length: > 0 })
        {
            // Sense the agent's surroundings.
            Sense();
                
            // Have the mind make decisions on what actions to take.
            Action[] decisions = Minds[_selectedMindIndex].Think();
            
            // If new decisions were made, update the actions to be them.
            if (decisions != null)
            {
                // Remove any null actions.
                List<Action> updated = decisions.Where(a => a != null).ToList();

                // If there were previous actions, keep actions of types which were not in the current decisions.
                if (Actions != null)
                {
                    foreach (Action action in Actions)
                    {
                        if (action == null)
                        {
                            continue;
                        }
            
                        if (!updated.Exists(a => a.GetType() == action.GetType()))
                        {
                            updated.Add(action);
                        }
                    }
                }
        
                Actions = updated.ToArray();
            }

            // Act on the actions.
            Act();
        }

        // After all actions are performed, calculate the agent's new performance.
        if (PerformanceMeasure != null)
        {
            Performance = PerformanceMeasure.GetPerformance();
        }
            
        // Reset the elapsed time for the next time this method is called.
        DeltaTime = 0;
    }

    /// <summary>
    /// Override to easily display the type of the component for easy usage in messages.
    /// </summary>
    /// <returns>Name of this type.</returns>
    public override string ToString()
    {
        return GetType().Name;
    }

    /// <summary>
    /// Setup the agent.
    /// </summary>
    public void Setup()
    {
        // Register this agent with the manager.
        AgentManager.Singleton.AddAgent(this);
        
        // Setup the wander guide.
        GameObject go = new("Wander Root");
        _wanderRoot = go.transform;
        _wanderRoot.parent = transform;
        _wanderRoot.localPosition = Vector3.zero;
        go = new GameObject("Wander Forward");
        _wanderForward = go.transform;
        _wanderForward.parent = _wanderRoot;
        _wanderForward.localPosition = new Vector3(0, 0, 1);
        
        // Find all minds.
        List<Mind> minds = GetComponents<Mind>().ToList();
        minds.AddRange(GetComponentsInChildren<Mind>());
        Minds = minds.Distinct().ToArray();
        foreach (Mind mind in minds)
        {
            mind.Agent = this;
        }
            
        // Find the performance measure.
        PerformanceMeasure = GetComponent<PerformanceMeasure>();
        if (PerformanceMeasure == null)
        {
            PerformanceMeasure = GetComponentInChildren<PerformanceMeasure>();
        }

        ConfigurePerformanceMeasure();

        // Find all attached actuators.
        List<Actuator> actuators = GetComponents<Actuator>().ToList();
        actuators.AddRange(GetComponentsInChildren<Actuator>());
        Actuators = actuators.Distinct().ToArray();
        foreach (Actuator actuator in Actuators)
        {
            actuator.Agent = this;
        }
        
        // Find all attached sensors.
        List<Sensor> sensors = GetComponents<Sensor>().ToList();
        sensors.AddRange(GetComponentsInChildren<Sensor>());
        Sensors = sensors.Distinct().ToArray();
            
        // Setup the percepts array to match the size of the sensors so each sensor can return a percept to its index.
        foreach (Sensor sensor in Sensors)
        {
            sensor.Agent = this;
        }

        // Setup the root visuals transform for agent rotation.
        Transform[] children = GetComponentsInChildren<Transform>();
        if (children.Length == 0)
        {
            go = new GameObject("Visuals");
            Visuals = go.transform;
            go.transform.parent = transform;
            go.transform.localPosition = Vector3.zero;
            go.transform.localRotation = Quaternion.identity;
            return;
        }

        Visuals = children.FirstOrDefault(t => t.name == "Visuals");
        if (Visuals == null)
        {
            Visuals = children[0];
        }
    }
    
    /// <summary>
    /// Implement movement behaviour.
    /// </summary>
    public abstract void Move();

    /// <summary>
    /// Look towards the agent's look target.
    /// </summary>
    public void Look()
    {
        Vector3 target;
        
        // If the agent has no otherwise set point to look, look in the direction it is moving.
        if (!LookingToTarget)
        {
            if (MoveVelocity == Vector2.zero)
            {
                return;
            }
            
            Transform t = transform;
            target = t.position + t.rotation * MoveVelocity3.normalized;
            target = new Vector3(target.x, Visuals.position.y, target.z);
        }
        else
        {
            // We only want to rotate along the Y axis so update the target rotation to be at the same Y level.
            target = new Vector3(LookTarget.x, Visuals.position.y, LookTarget.z);
        }

        // If the position to look at is the current position, simply return.
        if (Visuals.position == target)
        {
            return;
        }

        // Face towards the target.
        Visuals.rotation = Steering.Face(Visuals.position, Visuals.forward, target, lookSpeed > 0 ? lookSpeed : Mathf.Infinity, Time.deltaTime, Visuals.rotation);
    }

    protected virtual void Start()
    {
        // Setup the agent.
        Setup();
        
        // Enter its global and normal states if they are set.
        if (globalState != null)
        {
            globalState.Enter(this);
        }

        if (state != null)
        {
            state.Enter(this);
        }
    }

    protected virtual void OnEnable()
    {
        try
        {
            AgentManager.Singleton.AddAgent(this);
        }
        catch
        {
            // Ignored.
        }
    }

    protected virtual void OnDisable()
    {
        try
        {
            AgentManager.Singleton.RemoveAgent(this);
        }
        catch
        {
            // Ignored.
        }
    }

    protected virtual void OnDestroy()
    {
        try
        {
            AgentManager.Singleton.RemoveAgent(this);
        }
        catch
        {
            // Ignored.
        }
    }

    /// <summary>
    /// Calculate movement for the agent.
    /// </summary>
    /// <param name="deltaTime">The elapsed time step.</param>
    protected void CalculateMoveVelocity(float deltaTime)
    {
        // Initialize the movement for this time step.
        Vector2 movement = Vector2.zero;
        
        // If not using acceleration, have everything move as quick as possible to be clamped later.
        float acceleration = moveAcceleration > 0 ? moveAcceleration : float.MaxValue;
        
        // Convert the position into a Vector2 for use with steering methods.
        Vector3 positionVector3 = transform.position;
        Vector2 position = new(positionVector3.x, positionVector3.z);

        // If there is a path the agent is following, follow it.
        if (Path != null)
        {
            // If we are at the end of the path, see if the agent can skip to the end in case it was off of a node and thus sub-optimal.
            if (Path.Count == 2)
            {
                bool canReachEnd = false;
            
                if (AgentManager.Singleton.navigationRadius <= 0)
                {
                    if (!Physics.Linecast(transform.position, Path[^1], AgentManager.Singleton.obstacleLayers))
                    {
                        canReachEnd = true;
                    }
                }
                else
                {
                    Vector3 p1 = transform.position;
                    p1.y += AgentManager.Singleton.navigationRadius;
                    Vector3 p2 = Path[^1];
                    p2.y += AgentManager.Singleton.navigationRadius;
                    if (!Physics.SphereCast(p1, AgentManager.Singleton.navigationRadius, (p2 - p1).normalized, out _, Vector3.Distance(p1, p2), AgentManager.Singleton.obstacleLayers))
                    {
                        canReachEnd = true;
                    }
                }
                
                if (canReachEnd)
                {
                    Path = new List<Vector3> { Path[^1] };
                }
            }

            // Remove path locations which have been satisfied in being reached.
            while (Path.Count > 0 && Vector2.Distance(position, new Vector2(Path[0].x, Path[0].z)) <= seekAcceptableDistance)
            {
                Path.RemoveAt(0);
            }

            // If there is still a path to follow, seek towards the next point and if not, remove the path list.
            if (Path.Count > 0)
            {
                // Align the wander guide with the rotation so it will be properly aligned if switched to wandering.
                _wanderRoot.transform.localRotation = Visuals.rotation;
                movement += Steering.Seek(position, MoveVelocity, new Vector2(Path[0].x, Path[0].z), acceleration);
            }
            else
            {
                Path = null;
            }
        }

        // If there is not a path the agent is following, calculate movements.
        if (Path == null)
        {
            // If there is move data, perform it.
            if (MovesData.Count > 0)
            {
                // Align the wander guide with the rotation so it will be properly aligned if switched to wandering.
                _wanderRoot.transform.localRotation = Visuals.rotation;
                
                // Look through every move data.
                for (int i = 0; i < MovesData.Count; i++)
                {
                    // Get the position to move to/from.
                    Vector2 target = MovesData[i].Position;

                    // If this was a transform movement and the transform is now gone or the move has been satisfied, remove it.
                    if (MovesData[i].IsTransformTarget && MovesData[i].Transform == null || IsCompleteMove(MovesData[i].MoveType, position, target))
                    {
                        MovesData.RemoveAt(i--);
                        continue;
                    }

                    // Increase the elapsed time for the move data.
                    MovesData[i].DeltaTime += deltaTime;

                    // Update the movement vector of the data based on its given move type.
                    MovesData[i].MoveVector = MovesData[i].MoveType switch
                    {
                        MoveType.Seek => Steering.Seek(position, MoveVelocity, target, acceleration),
                        MoveType.Flee => Steering.Flee(position, MoveVelocity, target, acceleration),
                        MoveType.Pursuit => Steering.Pursuit(position, MoveVelocity, target, MovesData[i].LastPosition, acceleration, MovesData[i].DeltaTime),
                        MoveType.Evade => Steering.Evade(position, MoveVelocity, target, MovesData[i].LastPosition, acceleration, MovesData[i].DeltaTime),
                        _ => MovesData[i].MoveVector
                    };

                    // Add the newly calculated movement data to the movement vector for this time step.
                    movement += MovesData[i].MoveVector;

                    // Update the last position so the next time step could calculated predictive movement.
                    MovesData[i].LastPosition = target;
                    
                    // Zero the elapsed time since the action was completed for this move data.
                    MovesData[i].DeltaTime = 0;
                }
            }
            // Otherwise if there is no movement data and the agent should wander, have the agent randomly wander.
            else if (Wander)
            {
                // Get the desired angle to rotate by for the random wander sway.
                _wanderRoot.transform.localRotation = Quaternion.Euler(0, Steering.Wander(_wanderRoot.transform.rotation.eulerAngles.y, maxWanderTurn), 0);
                
                // Then simply seek towards the given wander guide position.
                Vector3 wander3 = _wanderForward.position;
                movement += Steering.Seek(position, MoveVelocity, new Vector2(wander3.x, wander3.z), acceleration);
            }
        }

        // If there was no movement, bring the agent to a stop.
        if (movement == Vector2.zero)
        {
            // Can only slow down at the rate of acceleration but this will instantly stop if there is no acceleration.
            MoveVelocity = Vector2.Lerp(MoveVelocity, Vector2.zero, acceleration * deltaTime);
            
            // After reaching below a velocity threshold, set directly to zero.
            if (MoveVelocity.magnitude < restVelocity)
            {
                MoveVelocity = Vector2.zero;
            }
            
            return;
        }
        
        // Add the new velocity to the agent's velocity.
        MoveVelocity += movement * deltaTime;

        // If the agent's velocity is too fast, normalize it and then set it back to the max speed.
        if (MoveVelocity.magnitude > moveSpeed)
        {
            MoveVelocity = MoveVelocity.normalized * moveSpeed;
        }
    }

    /// <summary>
    /// Determine if a move data is complete.
    /// </summary>
    /// <param name="moveType">The move type.</param>
    /// <param name="position">The agent position.</param>
    /// <param name="target">The move data target position.</param>
    /// <returns>True if the distance between the agent and the target is within or beyond their acceptable distance for completion.</returns>
    private bool IsCompleteMove(MoveType moveType, Vector2 position, Vector2 target)
    {
        return moveType is MoveType.Seek or MoveType.Pursuit && seekAcceptableDistance >= 0 && Vector2.Distance(position, target) <= seekAcceptableDistance ||
               moveType is MoveType.Flee or MoveType.Evade && fleeAcceptableDistance >= 0 && Vector2.Distance(position, target) >= fleeAcceptableDistance;
    }

    /// <summary>
    /// Handle receiving an event.
    /// </summary>
    /// <param name="aiEvent">The event to handle.</param>
    /// <returns>True if either the global state or normal state handles the event, false otherwise.</returns>
    private bool HandleEvent(AIEvent aiEvent)
    {
        return state != null && state.HandleEvent(this, aiEvent) || globalState != null && globalState.HandleEvent(this, aiEvent);
    }

    /// <summary>
    /// Read percepts from all the agent's sensors.
    /// </summary>
    private void Sense()
    {
        List<Percept> perceptsRead = new();
        int sensed = 0;
            
        // Read from every sensor.
        foreach (Sensor sensor in Sensors)
        {
            Percept percept = sensor.Read();
            if (percept == null)
            {
                continue;
            }

            AddMessage($"Perceived {percept} from sensor {sensor}.");
            perceptsRead.Add(percept);
            sensed++;
        }
        
        if (sensed > 1)
        {
            AddMessage($"Perceived {sensed} percepts.");
        }

        Percepts = perceptsRead.ToArray();
    }

    /// <summary>
    /// Perform actions.
    /// </summary>
    private void Act()
    {
        if (Actions == null || Actions.Length == 0)
        {
            return;
        }
            
        // Pass all actions to all actuators.
        foreach (Actuator actuator in Actuators)
        {
            actuator.Act(Actions);
        }

        foreach (Action action in Actions)
        {
            if (action.Complete)
            {
                AddMessage($"Completed action {action}.");
            }
        }

        // Remove actions which were completed.
        Actions = Actions.Where(a => !a.Complete).ToArray();
    }
        
    /// <summary>
    /// Link the performance measure to this agent.
    /// </summary>
    private void ConfigurePerformanceMeasure()
    {
        if (PerformanceMeasure != null)
        {
            PerformanceMeasure.Agent = this;
        }
    }
}