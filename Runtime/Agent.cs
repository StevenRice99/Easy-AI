using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Base class for all agents.
/// </summary>
public abstract class Agent : MonoBehaviour
{
    [SerializeField]
    [Min(0)]
    [Tooltip("How fast this agent can move in units per second.")]
    protected float moveSpeed = 10;
        
    [SerializeField]
    [Min(0)]
    [Tooltip("How fast this agent can look in degrees per second.")]
    protected float lookSpeed = 5;

    [SerializeField]
    [Min(0)]
    [Tooltip("How fast the agent's movement can accelerate. Set to zero for instantaneous accelerate.")]
    protected float moveAcceleration;

    [SerializeField]
    [Min(0)]
    [Tooltip("How fast the agent's rotation can accelerate Set to zero for instantaneous accelerate.")]
    protected float lookAcceleration;

    /// <summary>
    /// Getter for the move speed.
    /// </summary>
    public float MoveSpeed => moveSpeed;

    /// <summary>
    /// Getter for the look speed.
    /// </summary>
    public float LookSpeed => lookSpeed;

    /// <summary>
    /// Getter for the move acceleration.
    /// </summary>
    public float MoveAcceleration => moveAcceleration;

    /// <summary>
    /// Getter for the look acceleration.
    /// </summary>
    public float LookAcceleration => lookAcceleration;

    /// <summary>
    /// The current move velocity if move acceleration is being used.
    /// </summary>
    public float MoveVelocity { get; protected set; }

    /// <summary>
    /// The current look velocity if look acceleration is being used.
    /// </summary>
    public float LookVelocity { get; protected set; }
        
    /// <summary>
    /// The time passed since the last time the agent's mind made decisions. Use this instead of Time.DeltaTime.
    /// </summary>
    public float DeltaTime { get; set; }
        
    /// <summary>
    /// The target the agent is currently trying to move towards.
    /// </summary>
    public Vector3 MoveTarget { get; private set; }

    /// <summary>
    /// The target the agent is currently trying to look towards.
    /// </summary>
    public Vector3 LookTarget { get; private set; }

    /// <summary>
    /// True if the agent is trying to move to a target, false otherwise.
    /// </summary>
    public bool MovingToTarget { get; private set; }

    /// <summary>
    /// True if the agent is trying to look to a target, false otherwise.
    /// </summary>
    public bool LookingToTarget { get; private set; }
        
    /// <summary>
    /// True if the agent moved in the last update call, false otherwise.
    /// </summary>
    public bool DidMove { get; protected set; }

    /// <summary>
    /// True if the agent looked in the last update call, false otherwise.
    /// </summary>
    public bool DidLook { get; private set; }

    /// <summary>
    /// The performance measure of the agent.
    /// </summary>
    public float Performance { get; private set; }

    /// <summary>
    /// Get the currently selected mind of the agent.
    /// </summary>
    public Mind SelectedMind => Minds != null && Minds.Length > 0 ? Minds[_selectedMindIndex] : null;
        
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
    /// The position of this agent.
    /// </summary>
    public Vector3 Position => transform.position;

    /// <summary>
    /// The rotation of this agent.
    /// </summary>
    public Quaternion Rotation => Visuals.rotation;

    /// <summary>
    /// The local position of this agent.
    /// </summary>
    public Vector3 LocalPosition => transform.localPosition;

    /// <summary>
    /// The local rotation of this agent.
    /// </summary>
    public Quaternion LocalRotation => Visuals.localRotation;

    /// <summary>
    /// The performance measure of this agent.
    /// </summary>
    private PerformanceMeasure _performanceMeasure;

    /// <summary>
    /// The index of the currently selected mind.
    /// </summary>
    private int _selectedMindIndex;

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
        _performanceMeasure = performanceMeasure;
        ConfigurePerformanceMeasure();
    }

    /// <summary>
    /// Resume movement towards the move target currently assigned to the agent.
    /// </summary>
    public void MoveToTarget()
    {
        MovingToTarget = MoveTarget != transform.position;
    }

    /// <summary>
    /// Set a target position for the agent to move towards.
    /// </summary>
    /// <param name="target">The target position to move to.</param>
    public void MoveToTarget(Vector3 target)
    {
        MoveTarget = target;
        MoveToTarget();
    }

    /// <summary>
    /// Set a target transform for the agent to move towards.
    /// </summary>
    /// <param name="target">The target transform to move to.</param>
    public void MoveToTarget(Transform target)
    {
        if (target == null)
        {
            StopMoveToTarget();
            return;
        }

        MoveToTarget(target.position);
    }

    /// <summary>
    /// Have the agent stop moving towards its move target.
    /// </summary>
    public void StopMoveToTarget()
    {
        MovingToTarget = false;
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
    /// Resume moving towards the move target currently assigned and looking towards the look target currently assigned to the agent.
    /// </summary>
    public void MoveToLookAtTarget()
    {
        MoveToTarget();
        LookAtTarget();
    }

    /// <summary>
    /// Set a target position for the agent to move and look towards.
    /// </summary>
    /// <param name="target">The target position to move and look to.</param>
    public void MoveToLookAtTarget(Vector3 target)
    {
        MoveToTarget(target);
        LookAtTarget(target);
    }

    /// <summary>
    /// Set a target transform for the agent to move and look towards.
    /// </summary>
    /// <param name="target">The target transform to move and look to.</param>
    public void MoveToLookAtTarget(Transform target)
    {
        if (target == null)
        {
            StopMoveToLookAtTarget();
            return;
        }
            
        MoveToLookAtTarget(target.position);
    }
        
    /// <summary>
    /// Have the agent stop moving towards its move target and looking towards its look target.
    /// </summary>
    public void StopMoveToLookAtTarget()
    {
        StopMoveToTarget();
        StopLookAtTarget();
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
    public void Perform()
    {
        // Can only sense, think, and act if there is a mind attached.
        if (Minds != null && Minds.Length > 0)
        {
            // Sense the agent's surroundings.
            Sense();
                
            // Have the mind make decisions on what actions to take.
            Action[] decisions = Minds[_selectedMindIndex].Think(Percepts);
            
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
        if (_performanceMeasure != null)
        {
            Performance = _performanceMeasure.GetPerformance();
        }
            
        // Reset the elapsed time for the next time this method is called.
        DeltaTime = 0;
    }

    /// <summary>
    /// Add a message to this agent's mind as the agent itself does not hold its own messages.
    /// </summary>
    /// <param name="message">The message to add.</param>
    public void AddMessage(string message)
    {
        if (Minds == null || Minds.Length == 0)
        {
            return;
        }
            
        Minds[_selectedMindIndex].AddMessage(message);
    }

    /// <summary>
    /// Override to easily display the type of the component for easy usage in messages.
    /// </summary>
    /// <returns>Name of this type.</returns>
    public override string ToString()
    {
        return GetType().Name;
    }

    public void Setup()
    {
                // Register this agent with the manager.
        AgentManager.Singleton.AddAgent(this);
            
        // Find all minds.
        List<Mind> minds = GetComponents<Mind>().ToList();
        minds.AddRange(GetComponentsInChildren<Mind>());
        Minds = minds.Distinct().ToArray();
        foreach (Mind mind in minds)
        {
            mind.Agent = this;
        }
            
        // Find the performance measure.
        _performanceMeasure = GetComponent<PerformanceMeasure>();
        if (_performanceMeasure == null)
        {
            _performanceMeasure = GetComponentInChildren<PerformanceMeasure>();
            if (_performanceMeasure == null)
            {
                _performanceMeasure = FindObjectsOfType<PerformanceMeasure>().FirstOrDefault(m => m.Agent == null);
            }
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
            GameObject go = new GameObject("Visuals");
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

    protected virtual void Start()
    {
        Setup();
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
        // If the agent should not be looking simply return.
        if (!LookingToTarget)
        {
            LookVelocity = 0;
            DidLook = false;
            return;
        }
            
        // We only want to rotate along the Y axis so update the target rotation to be at the same Y level.
        Transform visuals = Visuals;
        Vector3 target = new Vector3(LookTarget.x, visuals.position.y, LookTarget.z);
            
        // If the position to look at is the current position, simply return.
        if (visuals.position == target)
        {
            LookVelocity = 0;
            DidLook = false;
            return;
        }

        // Calculate how fast we can look this frame.
        CalculateLookSpeed();

        // Look towards the target.
        Quaternion rotation = Visuals.rotation;
        Quaternion lastRotation = rotation;
        rotation = Quaternion.LookRotation(Vector3.RotateTowards(visuals.forward, target - visuals.position, LookVelocity * Time.deltaTime, 0.0f));
        Visuals.rotation = rotation;
        DidLook = rotation != lastRotation;

        if (DidLook)
        {
            AddMessage($"Looked towards {LookTarget}.");
        }
    }

    protected virtual void OnEnable()
    {
        try
        {
            AgentManager.Singleton.AddAgent(this);
        }
        catch { }
    }

    protected virtual void OnDisable()
    {
        try
        {
            AgentManager.Singleton.RemoveAgent(this);
        }
        catch { }
    }

    protected virtual void OnDestroy()
    {
        try
        {
            AgentManager.Singleton.RemoveAgent(this);
        }
        catch { }
    }

    protected void CalculateMoveVelocity()
    {
        if (moveAcceleration <= 0)
        {
            MoveVelocity = moveSpeed;
            return;
        }

        MoveVelocity = Mathf.Clamp(MoveVelocity + moveAcceleration, 0, moveSpeed);
    }

    private void CalculateLookSpeed()
    {
        if (lookAcceleration <= 0)
        {
            LookVelocity = lookSpeed;
            return;
        }

        LookVelocity = Mathf.Clamp(LookVelocity + lookAcceleration, 0, lookSpeed);
    }

    /// <summary>
    /// Read percepts from all the agent's sensors.
    /// </summary>
    private void Sense()
    {
        List<Percept> perceptsRead = new List<Percept>();
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

        if (sensed == 0)
        {
            AddMessage("Did not perceive anything.");
        }
        else if (sensed > 1)
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
            AddMessage("Did not perform any actions.");
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
        if (_performanceMeasure != null)
        {
            _performanceMeasure.Agent = this;
        }
    }
}