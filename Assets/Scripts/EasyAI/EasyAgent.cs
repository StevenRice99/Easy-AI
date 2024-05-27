using System;
using System.Collections.Generic;
using System.Linq;
using EasyAI.Navigation;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.InputSystem;

namespace EasyAI
{
    /// <summary>
    /// Base class for all agents.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class EasyAgent : MonoBehaviour
    {
        /// <summary>
        /// The actions of this agent that are not yet completed.
        /// </summary>
        private readonly List<object> _inProgressActions = new();

        /// <summary>
        /// How fast this agent can move in units per second.
        /// </summary>
        [Tooltip("How fast this agent can move in units per second.")]
        [Min(0)]
        public float moveSpeed = 10;

        /// <summary>
        /// How fast this agent can increase in move speed in units per second. Set to zero for instant.
        /// </summary>
        [Tooltip("How fast this agent can increase in move speed in units per second. Set to zero for instant.")]
        [Min(0)]
        public float moveAcceleration;
    
        /// <summary>
        /// How fast this agent can look in degrees per second. Set to zero for instant.
        /// </summary>
        [Tooltip("How fast this agent can look in degrees per second. Set to zero for instant.")]
        [Min(0)]
        public float lookSpeed;

        /// <summary>
        /// The current move velocity if move acceleration is being used.
        /// </summary>
        public Vector2 MoveVelocity { get; protected set; }

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
        /// The sensors of this agent.
        /// </summary>
        [HideInInspector]
        public EasySensor[] sensors = Array.Empty<EasySensor>();

        /// <summary>
        /// The actuators of this agent.
        /// </summary>
        [HideInInspector]
        public EasyActuator[] actuators = Array.Empty<EasyActuator>();

        /// <summary>
        /// The performance measure of this agent.
        /// </summary>
        [HideInInspector]
        public EasyPerformanceMeasure performanceMeasure;

        /// <summary>
        /// The root transform that holds the visuals for this agent used to rotate the agent towards its look target.
        /// </summary>
        [field: Tooltip("The root transform that holds the visuals for this agent used to rotate the agent towards its look target.")]
        [field: SerializeField]
        public Transform Visuals { get; private set; }

        /// <summary>
        /// The current move behaviour.
        /// </summary>
        public EasySteering.Behaviour MoveType { get; private set; }

        /// <summary>
        /// The target to move in relation to.
        /// </summary>
        public Transform MoveTarget { get; private set; }

        /// <summary>
        /// If this agent is alive and thus should perform actions or not.
        /// </summary>
        public bool Alive { get; set; } = true;

        /// <summary>
        /// The last position of the target to move in relation to.
        /// </summary>
        private Vector2 _moveTargetLastPosition;

        /// <summary>
        /// The current path an agent is following.
        /// </summary>
        public List<Vector3> Path { get; private set; } = new();

        /// <summary>
        /// True if the agent is trying to move, false otherwise.
        /// </summary>
        public bool Moving => Path.Count > 0;
        
        /// <summary>
        /// The mind of the agent.
        /// </summary>
        [field: Tooltip("The mind of the agent.")]
        [field: SerializeField]
        public EasyState Mind { get; private set; }
        
        /// <summary>
        /// The state the agent is in.
        /// </summary>
        [field: Tooltip("The state the agent is in.")]
        [field: SerializeField]
        public EasyState State { get; private set; }
        
        /// <summary>
        /// The state the agent was last in.
        /// </summary>
        public EasyState PreviousState { get; private set; }

        /// <summary>
        /// The path destination.
        /// </summary>
        public Vector3? Destination => Path.Count > 0 ? Path[^1] : null;

        /// <summary>
        /// The current move velocity if move acceleration is being used as a Vector3.
        /// </summary>
        public Vector3 MoveVelocity3 => new(MoveVelocity.x, 0, MoveVelocity.y);
        
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
        public readonly List<string> Messages = new();

        /// <summary>
        /// Set the state the agent is in.
        /// </summary>
        /// <param name="execute">If the new state should be executed after entering it.</param>
        /// <typeparam name="T">The state to put the agent in.</typeparam>
        public void SetState<T>(bool execute = false) where T : EasyState
        {
            EasyState value = EasyManager.GetState<T>();
            
            // If already in this state, do nothing.
            if (State == value)
            {
                return;
            }
            
            // Exit the current state.
            if (State != null)
            {
                State.Exit(this);
            }

            PreviousState = State;

            // Set the new state.
            State = value;

            if (State == null)
            {
                return;
            }

            // Enter the new state.
            State.Enter(this);

            // Execute the new state if set to do so.
            if (execute)
            {
                State.Execute(this);
            }
        }

        /// <summary>
        /// Return to the agent's last state.
        /// </summary>
        /// <param name="execute">If the previous state should be executed after entering it.</param>
        public void ReturnToPreviousState(bool execute = false)
        {
            // Exit the current state.
            if (State != null)
            {
                State.Exit(this);
            }

            // Return to the previous state.
            State = PreviousState;
            
            if (State == null)
            {
                return;
            }
            
            // Enter the previous state.
            State.Enter(this);
            
            // Execute the previous state if set to do so.
            if (execute)
            {
                State.Execute(this);
            }
        }

        /// <summary>
        /// Handle a message.
        /// </summary>
        /// <param name="sender">The agent that sent the message.</param>
        /// <param name="id">The message type.</param>
        /// <returns>True if the message was accepted and acted upon, false otherwise.</returns>
        public bool HandleMessage(EasyAgent sender, int id)
        {
            return State != null && State.HandleMessage(this, sender, id) || Mind != null && Mind.HandleMessage(this, sender, id);
        }

        /// <summary>
        /// Send a message to an agent.
        /// </summary>
        /// <param name="receiver">The agent to receive the message.</param>
        /// <param name="id">The message type.</param>
        /// <returns>True if the agent accepted and acted upon the message, false otherwise.</returns>
        public bool SendMessage(EasyAgent receiver, int id)
        {
            return receiver.HandleMessage(this, id);
        }

        /// <summary>
        /// Send a message and get the first agent that accepts and acts upon the message.
        /// Agents are sent the message in order of distance from the sender.
        /// </summary>
        /// <param name="id">The message type.</param>
        /// <returns>The first agent that accepts and acts upon the message or null otherwise.</returns>
        public EasyAgent FirstResponseMessage(int id)
        {
            return EasyManager.CurrentAgents.Where(a => a != this).OrderBy(a => Vector3.Distance(transform.position, a.transform.position)).FirstOrDefault(a => a.HandleMessage(this, id));
        }

        /// <summary>
        /// Send a message to every agent.
        /// </summary>
        /// <param name="id">The message type.</param>
        /// <returns>All agents that accepted and acted upon the message.</returns>
        public IEnumerable<EasyAgent> BroadcastMessage(int id)
        {
            return EasyManager.CurrentAgents.Where(a => a != this).Where(a => a.HandleMessage(this, id));
        }

        /// <summary>
        /// Get if the agent is in a given state.
        /// </summary>
        /// <typeparam name="T">The type of state to check.</typeparam>
        /// <returns>True if in the state, false otherwise.</returns>
        public bool IsInState<T>()
        {
            return State != null && State.GetType() == typeof(T);
        }

        /// <summary>
        /// Read a sensor and receive a given data piece type.
        /// </summary>
        /// <typeparam name="TSensor">The sensor type to read.</typeparam>
        /// <typeparam name="TData">The expected data to return.</typeparam>
        /// <returns>The data piece if it is returned by the given sensor type, default otherwise.</returns>
        public TData Sense<TSensor, TData>() where TSensor : EasySensor
        {
            // Loop through all sensors.
            foreach (EasySensor sensor in sensors)
            {
                if (sensor is not TSensor)
                {
                    continue;
                }

                // If the correct type of sensor and correct data returned, return it.
                object data = sensor.Sense();
                if (data is TData correctType)
                {
                    return correctType;
                }
            }
            
            // Return null if the given sensor returning the requested data type does not exist.
            return default;
        }
        
        /// <summary>
        /// Read all of a give sensor type and receive all of a given data piece type.
        /// </summary>
        /// <typeparam name="TSensor">The sensor type to read.</typeparam>
        /// <typeparam name="TData">The expected data to return.</typeparam>
        /// <returns>A list of the data type returned by the given sensors.</returns>
        public List<TData> SenseAll<TSensor, TData>() where TSensor : EasySensor
        {
            List<TData> dataList = new();
            
            // Loop through all sensors.
            foreach (EasySensor sensor in sensors)
            {
                if (sensor is not TSensor)
                {
                    continue;
                }

                // If the correct type of sensor and correct data returned, return it.
                object data = sensor.Sense();
                if (data is TData correctType)
                {
                    dataList.Add(correctType);
                }
            }
            
            return dataList;
        }

        /// <summary>
        /// Read all of a give sensor type and receive all potential types of data from those sensors.
        /// </summary>
        /// <typeparam name="TSensor">The sensor type to read.</typeparam>
        /// <returns>A list of the objects returned by the given sensors.</returns>
        public List<object> SenseAll<TSensor>() where TSensor : EasySensor
        {
            return (from sensor in sensors where sensor is TSensor select sensor.Sense()).ToList();
        }

        /// <summary>
        /// Read all sensors and receive all data.
        /// </summary>
        /// <returns>A list of the objects returned by all the sensors.</returns>
        public List<object> SenseAll()
        {
            return (from sensor in sensors select sensor.Sense()).ToList();
        }

        /// <summary>
        /// Add an action to perform.
        /// </summary>
        /// <param name="action"></param>
        public void Act(object action)
        {
            // Try the action on all actuators.
            if (actuators.Any(actuator => actuator.Act(action)))
            {
                for (int i = 0; i < _inProgressActions.Count; i++)
                {
                    if (_inProgressActions[i].GetType() != action.GetType())
                    {
                        continue;
                    }

                    _inProgressActions.RemoveAt(i);
                    break;
                }
                
                return;
            }

            // If there were previous actions, keep actions of types which were not in the current decisions.
            for (int i = 0; i < _inProgressActions.Count; i++)
            {
                if (_inProgressActions[i].GetType() != action.GetType())
                {
                    continue;
                }

                _inProgressActions[i] = action;
                return;
            }
            
            _inProgressActions.Add(action);
        }

        /// <summary>
        /// Clear any remaining in progress actions.
        /// </summary>
        public void ClearActions()
        {
            _inProgressActions.Clear();
        }

        /// <summary>
        /// Check if there is already an action type that is not yet complete.
        /// </summary>
        /// <typeparam name="T">The type of action to look for.</typeparam>
        /// <returns>True if the action of the type exists, false otherwise.</returns>
        public bool HasAction<T>()
        {
            return _inProgressActions.OfType<T>().Any();
        }

        /// <summary>
        /// Remove a given action type, if it exists.
        /// </summary>
        /// <typeparam name="T">The action type to remove.</typeparam>
        public void RemoveAction<T>()
        {
            for (int i = 0; i < _inProgressActions.Count; i++)
            {
                if (_inProgressActions[i] is not T)
                {
                    continue;
                }

                _inProgressActions.RemoveAt(i);
                return;
            }
        }

        /// <summary>
        /// Calculate a path towards a position.
        /// </summary>
        /// <param name="goal">The position to navigate to.</param>
        /// <returns>True if the path has been set, false if the agent was already navigating towards this point.</returns>
        private void CreatePath(Vector3 goal)
        {
            Path = EasyManager.LookupPath(transform.position, goal);
        }

        /// <summary>
        /// Set a transform to move based upon.
        /// </summary>
        /// <param name="tr">The transform.</param>
        /// <param name="behaviour">The move type.</param>
        public void Move(Transform tr, EasySteering.Behaviour behaviour = EasySteering.Behaviour.Seek)
        {
            MoveTarget = tr;
            if (MoveTarget == null)
            {
                Path.Clear();
                return;
            }
            
            Vector3 pos = MoveTarget.position;
            _moveTargetLastPosition = new(pos.x, pos.z);
            MoveType = behaviour;
            CreatePath(pos);
        }

        /// <summary>
        /// Set a position to move based upon.
        /// </summary>
        /// <param name="pos">The position.</param>
        /// <param name="behaviour">The move type.</param>
        public void Move(Vector3 pos, EasySteering.Behaviour behaviour = EasySteering.Behaviour.Seek)
        {
            MoveTarget = null;
            _moveTargetLastPosition = new(pos.x, pos.z);
            
            // When going to a static position only, pursue and evade have no impact so ensure only seek or flee.
            MoveType = EasySteering.IsApproachingBehaviour(behaviour) ? EasySteering.Behaviour.Seek : EasySteering.Behaviour.Flee;
            CreatePath(pos);
        }

        /// <summary>
        /// Set a position to move based upon.
        /// </summary>
        /// <param name="pos">The position.</param>
        /// <param name="behaviour">The move type.</param>
        public void Move(Vector2 pos, EasySteering.Behaviour behaviour = EasySteering.Behaviour.Seek)
        {
            MoveTarget = null;
            _moveTargetLastPosition = pos;
            
            // When going to a static position only, pursue and evade have no impact so ensure only seek or flee.
            MoveType = EasySteering.IsApproachingBehaviour(behaviour) ? EasySteering.Behaviour.Seek : EasySteering.Behaviour.Flee;
            CreatePath(new(pos.x, transform.position.y, pos.y));
        }

        /// <summary>
        /// Clear all move data.
        /// </summary>
        public void StopMoving()
        {
            MoveTarget = null;
            Path.Clear();
        }

        /// <summary>
        /// Resume looking towards the look target currently assigned to the agent.
        /// </summary>
        public void Look()
        {
            LookingToTarget = LookTarget != transform.position;
        }

        /// <summary>
        /// Set a target position for the agent to look towards.
        /// </summary>
        /// <param name="target">The target position to look to.</param>
        public void Look(Vector3 target)
        {
            LookTarget = target;
            Look();
        }

        /// <summary>
        /// Set a target transform for the agent to look towards.
        /// </summary>
        /// <param name="target">The target transform to look to.</param>
        public void Look(Transform target)
        {
            if (target == null)
            {
                StopLooking();
                return;
            }
            
            Look(target.position);
        }

        /// <summary>
        /// Have the agent stop looking towards its look target.
        /// </summary>
        public void StopLooking()
        {
            LookingToTarget = false;
        }

        /// <summary>
        /// Called to have the agent sense, think, and act.
        /// </summary>
        public virtual void Perform()
        {
            if (Mind != null)
            {
                Mind.Execute(this);
            }
            
            if (State != null)
            {
                State.Execute(this);
            }
        }

        /// <summary>
        /// Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
        /// </summary>
        protected virtual void OnValidate()
        {
            // Find the performance measure.
            performanceMeasure = GetComponent<EasyPerformanceMeasure>();
            if (performanceMeasure == null)
            {
                performanceMeasure = GetComponentInChildren<EasyPerformanceMeasure>();
            }
            if (performanceMeasure != null)
            {
                performanceMeasure.agent = this;
            }

            // Find all attached actuators.
            List<EasyActuator> a = GetComponents<EasyActuator>().ToList();
            a.AddRange(GetComponentsInChildren<EasyActuator>());
            actuators = a.Distinct().ToArray();
            foreach (EasyActuator actuator in actuators)
            {
                actuator.agent = this;
            }
            
            // Find all attached sensors.
            List<EasySensor> s = GetComponents<EasySensor>().ToList();
            s.AddRange(GetComponentsInChildren<EasySensor>());
            sensors = s.Distinct().ToArray();
            foreach (EasySensor sensor in sensors)
            {
                sensor.agent = this;
            }

            // Set up the root visuals transform for agent rotation.
            Transform[] children = GetComponentsInChildren<Transform>();
            if (children.Length == 0)
            {
                GameObject go = new("Visuals");
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
        /// Look towards the agent's look target.
        /// </summary>
        public void LookCalculations()
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
                target = new(target.x, Visuals.position.y, target.z);
            }
            else
            {
                // We only want to rotate along the Y axis so update the target rotation to be at the same Y level.
                target = new(LookTarget.x, Visuals.position.y, LookTarget.z);
            }

            // If the position to look at is the current position, simply return.
            if (Visuals.position == target)
            {
                return;
            }

            // Face towards the target.
            Vector3 rotation = Vector3.RotateTowards(Visuals.forward, target - Visuals.position, (lookSpeed > 0 ? lookSpeed : Mathf.Infinity) * Time.deltaTime, 0.0f);
            Visuals.rotation = rotation == Vector3.zero || float.IsNaN(rotation.x) || float.IsNaN(rotation.y) || float.IsNaN(rotation.z) ? Visuals.rotation : Quaternion.LookRotation(rotation);
        }

        protected virtual void OnEnable()
        {
            Alive = true;
            
            try
            {
                EasyManager.AddAgent(this);
            }
            catch
            {
                // Ignored.
            }
            
            if (Mind != null)
            {
                Mind.Enter(this);
            }

            if (State != null)
            {
                State.Enter(this);
            }
        }

        protected virtual void OnDisable()
        {
            Alive = false;
            
            try
            {
                EasyManager.RemoveAgent(this);
            }
            catch
            {
                // Ignored.
            }
            
            if (Mind != null)
            {
                Mind.Exit(this);
            }

            if (State != null)
            {
                State.Exit(this);
            }
        }

        /// <summary>
        /// Calculate movement for the agent.
        /// </summary>
        protected void CalculateMoveVelocity()
        {
            float deltaTime = Time.deltaTime;
            
            // Initialize the movement for this time step.
            Vector2 movement = Vector2.zero;
        
            // If not using acceleration, have everything move as quick as possible to be clamped later.
            float acceleration = moveAcceleration > 0 ? moveAcceleration : float.MaxValue;
        
            // Convert the position into a Vector2 for use with steering methods.
            Vector3 positionVector3 = transform.position;
            Vector2 position = new(positionVector3.x, positionVector3.z);
            
            // If there is move data, perform it.
            if (MoveTarget != null)
            {
                Vector3 target3 = MoveTarget.position;
                Vector2 target = new(target3.x, target3.z);

                if (EasySteering.IsMoveComplete(MoveType, position, target))
                {
                    MoveTarget = null;
                }
                else if (_moveTargetLastPosition != target)
                {
                    CreatePath(target3);
                }
            }

            // If there is a path the agent is following, follow it.
            if (Path.Count > 0)
            {
                // Remove path locations which have been satisfied in being reached.
                while (Path.Count > 0 && Vector2.Distance(position, new(Path[0].x, Path[0].z)) <= EasyManager.SeekDistance)
                {
                    Path.RemoveAt(0);
                }
                
                // See if any points along the path can be skipped.
                while (Path.Count >= 2)
                {
                    if (EasyManager.HitObstacle(positionVector3, Path[1]))
                    {
                        break;
                    }

                    Path.RemoveAt(0);
                }

                // If there is still a path to follow, seek towards the next point and if not, remove the path list.
                if (Path.Count > 0)
                {
                    Vector2 current = new(Path[0].x, Path[0].z);
                    movement += EasySteering.Move(MoveType, position, MoveVelocity, current, _moveTargetLastPosition, acceleration, deltaTime);
                    _moveTargetLastPosition = current;
                }
            }

            // If there was no movement, bring the agent to a stop.
            if (movement == Vector2.zero)
            {
                // Can only slow down at the rate of acceleration but this will instantly stop if there is no acceleration.
                MoveVelocity = Vector2.Lerp(MoveVelocity, Vector2.zero, acceleration * deltaTime);
            
                // After reaching below a velocity threshold, set directly to zero.
                if (MoveVelocity.magnitude < EasyManager.RestVelocity)
                {
                    MoveVelocity = Vector2.zero;
                }
            
                return;
            }
        
            // Add the new velocity to the agent's velocity.
            MoveVelocity += movement * deltaTime;
                
            double x = MoveVelocity.x;
            double y = MoveVelocity.y;

            double magnitude = math.sqrt(x * x + y * y);

            // If the agent's velocity is too fast, normalize it and then set it back to the max speed.
            if (magnitude <= moveSpeed)
            {
                return;
            }

            x /= magnitude;
            y /= magnitude;

            x *= moveSpeed;
            y *= moveSpeed;

            MoveVelocity = new((float) x, (float) y);
        }

        /// <summary>
        /// Perform actions that are still incomplete.
        /// </summary>
        private void ActIncomplete()
        {
            for (int i = 0; i < _inProgressActions.Count; i++)
            {
                bool completed = false;
                
                foreach (EasyActuator actuator in actuators)
                {
                    completed = actuator.Act(_inProgressActions[i]);
                    if (completed)
                    {
                        break;
                    }
                }

                if (!completed)
                {
                    continue;
                }

                _inProgressActions.RemoveAt(i--);
            }
        }

        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        protected virtual void Update()
        {
            if (!Alive)
            {
                return;
            }
            
            // If there is nothing controlling the agent, move with the mouse manually.
            if (Mind == null && State == null && EasyManager.CurrentlySelectedAgent == this && Mouse.current.rightButton.wasPressedThisFrame && Physics.Raycast(EasyManager.SelectedCamera.ScreenPointToRay(new(Mouse.current.position.x.ReadValue(), Mouse.current.position.y.ReadValue(), 0)), out RaycastHit hit, Mathf.Infinity, EasyManager.GroundLayers | EasyManager.ObstacleLayers))
            {
                StopMoving();
                CreatePath(hit.point);
            }

            // Act on the actions.
            ActIncomplete();

            // After all actions are performed, calculate the agent's new performance.
            if (performanceMeasure != null)
            {
                Performance = performanceMeasure.CalculatePerformance();
            }

            LookCalculations();
        }

        /// <summary>
        /// Frame-rate independent MonoBehaviour. FixedUpdate message for physics calculations.
        /// </summary>
        protected virtual void FixedUpdate()
        {
            if (Alive)
            {
                Perform();
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
        public virtual float DisplayDetails(float x, float y, float w, float h, float p)
        {
            return y;
        }

        /// <summary>
        /// Add a message to this agent.
        /// </summary>
        /// <param name="message">The message to add.</param>
        public void Log(string message)
        {
            if (Messages.Count > 0 && Messages[0] == message)
            {
                return;
            }

            Messages.Insert(0, message);
            if (Messages.Count > EasyManager.MaxMessages)
            {
                Messages.RemoveAt(Messages.Count - 1);
            }
            
            EasyManager.GlobalLog($"{name} - {message}");
        }

        /// <summary>
        /// Override to easily display the type of the component for easy usage in messages.
        /// </summary>
        /// <returns>Name of this type.</returns>
        public override string ToString()
        {
            return GetType().Name;
        }
    }
}