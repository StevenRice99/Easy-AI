using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Base component for sensors, actuators, minds, and performance measures.
/// </summary>
public abstract class IntelligenceComponent : MonoBehaviour
{
    /// <summary>
    /// The agent this component is connected to.
    /// </summary>
    public Agent Agent { get; set; }

    /// <summary>
    /// The time passed since the agent last updated. Always use this over Time.DeltaTime because if the
    /// AgentManager is limiting how many agents are updated in a single frame then Time.DeltaTime will not be
    /// accurate for tracking elapsed time.
    /// </summary>
    public float DeltaTime => Agent.DeltaTime;
        
    /// <summary>
    /// The position the agent is currently moving towards.
    /// </summary>
    public Vector3 MoveTarget => Agent.MoveTarget;

    /// <summary>
    /// The position the agent is currently looking at.
    /// </summary>
    public Vector3 LookTarget => Agent.LookTarget;

    /// <summary>
    /// If the agent is currently moving towards a target or not.
    /// </summary>
    public bool MovingToTarget => Agent.MovingToTarget;

    /// <summary>
    /// If the agent is currently looking towards a target or not.
    /// </summary>
    public bool LookingAtTarget => Agent.LookingToTarget;

    /// <summary>
    /// If the agent moved towards its move target last time it updated or not.
    /// </summary>
    public bool DidMove => Agent.DidMove;

    /// <summary>
    /// If the agent looked towards its look target last time it updated or not.
    /// </summary>
    public bool DidLook => Agent.DidLook;

    /// <summary>
    /// The current performance measure of the agent.
    /// </summary>
    public float Performance => Agent.Performance;

    /// <summary>
    /// The sensors connected to the agent.
    /// </summary>
    public Sensor[] Sensors => Agent.Sensors;

    /// <summary>
    /// The percepts last read by the agent's sensors.
    /// </summary>
    public Percept[] Percepts => Agent.Percepts;

    /// <summary>
    /// The actuators connected to the agent.
    /// </summary>
    public Actuator[] Actuators => Agent.Actuators;

    /// <summary>
    /// The actions last decided to be performed by the agent.
    /// </summary>
    public Action[] Actions => Agent.Actions;

    /// <summary>
    /// The position of the agent.
    /// </summary>
    public Vector3 Position => Agent.Position;

    /// <summary>
    /// The rotation of the agent.
    /// </summary>
    public Quaternion Rotation => Agent.Rotation;

    /// <summary>
    /// The local position of the agent.
    /// </summary>
    public Vector3 LocalPosition => Agent.LocalPosition;

    /// <summary>
    /// The local rotation of the agent.
    /// </summary>
    public Quaternion LocalRotation => Agent.LocalRotation;
        
    /// <summary>
    /// If this component has any messages or not.
    /// </summary>
    public bool HasMessages => Messages.Count > 0;

    /// <summary>
    /// The number of messages this component has.
    /// </summary>
    public int MessageCount => Messages.Count;
        
    /// <summary>
    /// The messages of this component.
    /// </summary>
    public List<string> Messages { get; private set; } = new List<string>();

    /// <summary>
    /// Override for custom detail rendering on the automatic GUI.
    /// </summary>
    /// <param name="x">X rendering position. In most cases this should remain unchanged.</param>
    /// <param name="y">Y rendering position. Update this with every component added and return it.</param>
    /// <param name="w">Width of components. In most cases this should remain unchanged.</param>
    /// <param name="h">Height of components. In most cases this should remain unchanged.</param>
    /// <param name="p">Padding of components. In most cases this should remain unchanged.</param>
    /// <returns>The updated Y position after all custom rendering has been done.</returns>
    public virtual float DisplayDetails(float x, float y, float w, float h, float p)
    {
        return y;
    }
        
    /// <summary>
    /// Override to display custom GL.LINES gizmos.
    /// </summary>
    public virtual void DisplayGizmos() { }

    /// <summary>
    /// Add a message to this component.
    /// </summary>
    /// <param name="message">The message to add.</param>
    public void AddMessage(string message)
    {
        AgentManager.Singleton.AddGlobalMessage($"{name} - {message}");
            
        switch (AgentManager.Singleton.MessageMode)
        {
            case AgentManager.MessagingMode.Compact when Messages.Count > 0 && Messages[0] == message:
                return;
            case AgentManager.MessagingMode.Unique:
                Messages = Messages.Where(m => m != message).ToList();
                break;
        }

        Messages.Insert(0, message);
        if (Messages.Count > AgentManager.Singleton.MaxMessages)
        {
            Messages.RemoveAt(Messages.Count - 1);
        }
    }

    /// <summary>
    /// Clear all messages of this component.
    /// </summary>
    public void ClearMessages()
    {
        Messages.Clear();
    }

    /// <summary>
    /// Override to easily display the type of the component for easy usage in messages.
    /// </summary>
    /// <returns>Name of this type.</returns>
    public override string ToString()
    {
        return GetType().Name;
    }

    protected virtual void Start()
    {
        Setup();
    }
    
    protected virtual void OnEnable()
    {
        try
        {
            Setup();
        }
        catch { }
    }

    protected virtual void OnDisable()
    {
        try
        {
            Setup();
        }
        catch { }
    }

    protected virtual void OnDestroy()
    {
        try
        {
            Setup();
        }
        catch { }
    }

    /// <summary>
    /// If this was added to the agent later, it won't yet be connected to it, so call the configuration again.
    /// </summary>
    private void Setup()
    {
        if (Agent == null)
        {
            AgentManager.Singleton.RefreshAgents();
        }
    }
}