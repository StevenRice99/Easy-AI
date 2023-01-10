using System.Collections.Generic;
using System.Linq;
using EasyAI.Actuators;
using EasyAI.AgentActions;
using EasyAI.Managers;
using EasyAI.Navigation;
using EasyAI.Percepts;
using EasyAI.Thinking;
using EasyAI.Utility;
using UnityEngine;
using UnityEngine.InputSystem;
using Sensor = EasyAI.Sensors.Sensor;

namespace EasyAI.Agents
{
    /// <summary>
    /// Base class for all agents.
    /// </summary>
    public abstract class Agent : MessageComponent
    {
        /// <summary>
        /// Class to store all targets the agent is moving in relation to.
        /// </summary>
        public class Movement
        {
            /// <summary>
            /// The move type so proper behaviours can be performed.
            /// </summary>
            public readonly Steering.Behaviour Behaviour;

            /// <summary>
            /// The transform to move in relation to.
            /// </summary>
            public readonly Transform Transform;

            /// <summary>
            /// True if this move data was setup with a transform so if at any point the transform is destroyed this is removed as well.
            /// </summary>
            public readonly bool IsTransformTarget;
            
            /// <summary>
            /// Store the position which is only used if the transform is null.
            /// </summary>
            private readonly Vector2 _position;

            /// <summary>
            /// How much time has elapsed since the last time this was called for predictive move types.
            /// </summary>
            public float DeltaTime;

            /// <summary>
            /// The last position this was in since 
            /// </summary>
            public Vector2 LastPosition;
        
            /// <summary>
            /// The movement vector for visualizing move data.
            /// </summary>
            public Vector2 MoveVector = Vector2.zero;
        
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
                    return new(pos3.x, pos3.z);
                }
            }

            /// <summary>
            /// Create a move data for a transform.
            /// </summary>
            /// <param name="behaviour">The move type.</param>
            /// <param name="transform">The transform.</param>
            public Movement(Steering.Behaviour behaviour, Transform transform)
            {
                Behaviour = behaviour;
                Transform = transform;
                Vector3 pos3 = transform.position;
                _position = new(pos3.x, pos3.z);
                LastPosition = _position;
                IsTransformTarget = true;
            }

            /// <summary>
            /// Create a move data for a position.
            /// </summary>
            /// <param name="behaviour">The move type.</param>
            /// <param name="position">The position.</param>
            public Movement(Steering.Behaviour behaviour, Vector2 position)
            {
                // Since pursuit and evade are for moving objects and this is only with a static position,
                // switch pursuit to seek and evade to flee.
                behaviour = behaviour switch
                {
                    Steering.Behaviour.Pursue => Steering.Behaviour.Seek,
                    Steering.Behaviour.Evade => Steering.Behaviour.Flee,
                    _ => behaviour
                };

                Behaviour = behaviour;
                Transform = null;
                _position = position;
                LastPosition = _position;
                IsTransformTarget = false;
            }
        }
    
        [Tooltip("The current state the agent is in. Initialize it with the state to start in.")]
        [SerializeField]
        private State state;
    
        [Tooltip("How fast this agent can move in units per second.")]
        [Min(0)]
        public float moveSpeed = 10;
    
        [Min(0)]
        [Tooltip("How fast this agent can increase in move speed in units per second. Set to zero for instant.")]
        public float moveAcceleration;
    
        [Tooltip("How fast this agent can look in degrees per second. Set to zero for instant.")]
        [Min(0)]
        public float lookSpeed;

        /// <summary>
        /// The current move velocity if move acceleration is being used.
        /// </summary>
        public Vector2 MoveVelocity { get; protected set; }

        /// <summary>
        /// The time passed since the last time the agent's mind made decisions. Use this instead of Time.DeltaTime.
        /// </summary>
        public float DeltaTime { get; private set; }

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
        public Sensor[] Sensors { get; private set; }

        /// <summary>
        /// The actuators of this agent.
        /// </summary>
        public Actuator[] Actuators { get; private set; }

        /// <summary>
        /// The actions of this agent.
        /// </summary>
        public List<AgentAction> Actions { get; private set; } = new();

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
        public List<Movement> Moves { get; private set; } = new();

        /// <summary>
        /// The current path an agent is following.
        /// </summary>
        public List<Vector3> Path { get; private set; }

        public bool Moving => Moves.Count > 0 || Path != null;

        /// <summary>
        /// The state the agent is in.
        /// </summary>
        public State State => state;

        /// <summary>
        /// The path destination.
        /// </summary>
        public Vector3? Destination => Path?[^1];

        /// <summary>
        /// The current move velocity if move acceleration is being used as a Vector3.
        /// </summary>
        protected Vector3 MoveVelocity3 => new(MoveVelocity.x, 0, MoveVelocity.y);
    
        /// <summary>
        /// Display lines to highlight agent movement.
        /// </summary>
        public override void DisplayGizmos()
        {
            Vector3 position = transform.position;
            position.y += Manager.NavigationVisualOffset;

            if (Path == null || Path.Count == 0)
            {
                // Display the movement vectors of all move types.
                foreach (Movement movement in Moves)
                {
                    // Assign different colors for different behaviours.
                    GL.Color(Steering.GizmosColor(movement.Behaviour));
            
                    // Draw a line from the agent's position showing the force of this movement.
                    GL.Vertex(position);
                    GL.Vertex(position + transform.rotation * (new Vector3(movement.MoveVector.x, position.y, movement.MoveVector.y).normalized * 2));

                    if (movement.Behaviour is Steering.Behaviour.Seek or Steering.Behaviour.Flee)
                    {
                        continue;
                    }
            
                    // Draw another line from the agent's position to where the agent is seeking/pursuing/fleeing/evading to/from.
                    GL.Vertex(position);
                    GL.Vertex(new(movement.Position.x, position.y, movement.Position.y));
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
            if (Manager.SightHeight > 0)
            {
                GL.Vertex(transform.position + new Vector3(0, Manager.SightHeight, 0));
            }
            else
            {
                GL.Vertex(position);
            }
            GL.Vertex(LookTarget);
        }

        /// <summary>
        /// Increase the time that has elapsed.
        /// </summary>
        public void IncreaseDeltaTime()
        {
            DeltaTime += Time.deltaTime;
        }

        public void SetState<T>() where T : State
        {
            State value = Manager.GetState<T>();
            
            // If already in this state, do nothing.
            if (state == value)
            {
                return;
            }
                
            // Exit the current state.
            if (state != null)
            {
                state.Exit(this);
            }

            // Set the new state.
            state = value;

            // Enter the new state.
            if (state != null)
            {
                state.Enter(this);
            }
        }

        /// <summary>
        /// Read a sensor and receive a given data piece type.
        /// </summary>
        /// <typeparam name="TSensor">The sensor type to read.</typeparam>
        /// <typeparam name="TDataPiece">The expected piece to return.</typeparam>
        /// <returns>The data piece if it is returned by the given sensor type, null otherwise.</returns>
        public TDataPiece Sense<TSensor, TDataPiece>() where TSensor : Sensor where TDataPiece : DataPiece 
        {
            // Loop through all sensors.
            foreach (Sensor sensor in Sensors)
            {
                if (sensor is not TSensor)
                {
                    continue;
                }

                // If the correct type of sensor and correct data returned, return it.
                PerceivedData data = sensor.Read();
                if (data is TDataPiece correctType)
                {
                    return correctType;
                }
            }
            
            // Return null if the given sensor returning the requested data type does not exist.
            return null;
        }

        /// <summary>
        /// Add an action to perform.
        /// </summary>
        /// <param name="action"></param>
        public void Act(AgentAction action)
        {
            // If there were previous actions, keep actions of types which were not in the current decisions.
            for (int i = 0; i < Actions.Count; i++)
            {
                if (Actions[i].GetType() == action.GetType())
                {
                    Actions[i] = action;
                    return;
                }
            }
            
            Actions.Add(action);
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
        
            Path = Manager.LookupPath(transform.position, goal);
            return true;
        }
    
        /// <summary>
        /// Clear the path.
        /// </summary>
        public void StopNavigating()
        {
            Path = null;
        }

        /// <summary>
        /// Set a transform to move based upon.
        /// </summary>
        /// <param name="behaviour">The move type.</param>
        /// <param name="tr">The transform.</param>
        public void Move(Steering.Behaviour behaviour, Transform tr)
        {
            Moves.Clear();
            AddMove(behaviour, tr);
        }

        /// <summary>
        /// Set a position to move based upon.
        /// </summary>
        /// <param name="behaviour">The move type.</param>
        /// <param name="pos">The position.</param>
        public void Move(Steering.Behaviour behaviour, Vector3 pos)
        {
            Moves.Clear();
            AddMove(behaviour, pos);
        }

        /// <summary>
        /// Set a position to move based upon.
        /// </summary>
        /// <param name="behaviour">The move type.</param>
        /// <param name="pos">The position.</param>
        public void Move(Steering.Behaviour behaviour, Vector2 pos)
        {
            Moves.Clear();
            AddMove(behaviour, pos);
        }
    
        /// <summary>
        /// Add a transform to move based upon.
        /// </summary>
        /// <param name="behaviour">The move type.</param>
        /// <param name="tr">The transform.</param>
        public void AddMove(Steering.Behaviour behaviour, Transform tr)
        {
            if (Moves.Exists(m => m.Behaviour == behaviour && m.Transform == tr) || Steering.IsMoveComplete(behaviour, new(transform.position.x, transform.position.z), new(tr.position.x, tr.position.z)))
            {
                return;
            }

            Path = null;
            RemoveMove(tr);
            Moves.Add(new(behaviour, tr));
        }

        /// <summary>
        /// Add a position to move based upon.
        /// </summary>
        /// <param name="behaviour">The move type.</param>
        /// <param name="pos">The position.</param>
        public void AddMove(Steering.Behaviour behaviour, Vector3 pos)
        {
            AddMove(behaviour, new Vector2(pos.x, pos.z));
        }

        /// <summary>
        /// Add a position to move based upon.
        /// </summary>
        /// <param name="behaviour">The move type.</param>
        /// <param name="pos">The position.</param>
        public void AddMove(Steering.Behaviour behaviour, Vector2 pos)
        {
            if (Moves.Exists(m => m.Behaviour == behaviour && m.Transform == null && m.Position == pos) || Steering.IsMoveComplete(behaviour, new(transform.position.x, transform.position.z), pos))
            {
                return;
            }
        
            Path = null;
            RemoveMove(pos);
            Moves.Add(new(behaviour, pos));
        }

        /// <summary>
        /// Clear all move data.
        /// </summary>
        public void StopMoving()
        {
            Moves.Clear();
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
            return receiver != null && receiver != this && receiver.HandleEvent(new(eventId, this, details));
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
            foreach (bool result in Manager.CurrentAgents.Where(a => a != this).Select(a => a.HandleEvent(new(eventId, this, details))))
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
        /// Called by the AgentManager to have the agent sense, think, and act.
        /// </summary>
        public virtual void Perform()
        {
            if (Manager.Mind != null)
            {
                Manager.Mind.Execute(this);
            }
            else
            {
                if (Manager.CurrentlySelectedAgent == this && Mouse.current.rightButton.wasPressedThisFrame && Physics.Raycast(Manager.SelectedCamera.ScreenPointToRay(new(Mouse.current.position.x.ReadValue(), Mouse.current.position.y.ReadValue(), 0)), out RaycastHit hit, Mathf.Infinity, Manager.GroundLayers | Manager.ObstacleLayers))
                {
                    StopMoving();
                    Navigate(hit.point);
                }
            }

            if (state != null)
            {
                state.Execute(this);
            }

            // Act on the actions.
            Act();

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
            Manager.AddAgent(this);
            
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
            
            // Setup the percepts array to match the size of the sensors so each sensor can return a percepts to its index.
            foreach (Sensor sensor in Sensors)
            {
                sensor.Agent = this;
            }

            // Setup the root visuals transform for agent rotation.
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
        /// Implement movement behaviour.
        /// </summary>
        public abstract void MovementCalculations();

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
            float maxSpeed = lookSpeed > 0 ? lookSpeed : Mathf.Infinity;
            Vector3 rotation = Vector3.RotateTowards(Visuals.forward, target - Visuals.position, maxSpeed * Time.deltaTime, 0.0f);
            Visuals.rotation = rotation == Vector3.zero || float.IsNaN(rotation.x) || float.IsNaN(rotation.y) || float.IsNaN(rotation.z) ? Visuals.rotation : Quaternion.LookRotation(rotation);
        }

        protected virtual void Start()
        {
            // Setup the agent.
            Setup();
        
            // Enter its global and normal states if they are set.
            if (Manager.Mind != null)
            {
                Manager.Mind.Enter(this);
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
                Manager.AddAgent(this);
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
                Manager.RemoveAgent(this);
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
                Manager.RemoveAgent(this);
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
            
                    if (Manager.NavigationRadius <= 0)
                    {
                        if (!Physics.Linecast(transform.position, Path[^1], Manager.ObstacleLayers))
                        {
                            canReachEnd = true;
                        }
                    }
                    else
                    {
                        Vector3 p1 = transform.position;
                        p1.y += Manager.NavigationRadius;
                        Vector3 p2 = Path[^1];
                        p2.y += Manager.NavigationRadius;
                        if (!Physics.SphereCast(p1, Manager.NavigationRadius, (p2 - p1).normalized, out _, Vector3.Distance(p1, p2), Manager.ObstacleLayers))
                        {
                            canReachEnd = true;
                        }
                    }
                
                    if (canReachEnd)
                    {
                        Path = new() { Path[^1] };
                    }
                }

                // Remove path locations which have been satisfied in being reached.
                while (Path.Count > 0 && Vector2.Distance(position, new(Path[0].x, Path[0].z)) <= Manager.SeekAcceptableDistance)
                {
                    Path.RemoveAt(0);
                }

                // If there is still a path to follow, seek towards the next point and if not, remove the path list.
                if (Path.Count > 0)
                {
                    movement += Steering.Seek(position, MoveVelocity, new(Path[0].x, Path[0].z), acceleration);
                }
                else
                {
                    Path = null;
                }
            }

            // If there is still a path to follow, stop.
            if (Path != null)
            {
                return;
            }

            // If there is move data, perform it.
            if (Moves.Count > 0)
            {
                // Look through every move data.
                for (int i = 0; i < Moves.Count; i++)
                {
                    // Get the position to move to/from.
                    Vector2 target = Moves[i].Position;

                    // If this was a transform movement and the transform is now gone or the move has been satisfied, remove it.
                    if (Moves[i].IsTransformTarget && Moves[i].Transform == null || Steering.IsMoveComplete(Moves[i].Behaviour, position, target))
                    {
                        Moves.RemoveAt(i--);
                        continue;
                    }

                    // Increase the elapsed time for the move data.
                    Moves[i].DeltaTime += deltaTime;
                    
                    // Update the movement vector of the data based on its given move type.
                    Moves[i].MoveVector = Steering.Move(Moves[i].Behaviour, position, MoveVelocity, target, Moves[i].LastPosition, acceleration, Moves[i].DeltaTime);

                    // Add the newly calculated movement data to the movement vector for this time step.
                    movement += Moves[i].MoveVector;

                    // Update the last position so the next time step could calculated predictive movement.
                    Moves[i].LastPosition = target;

                    // Zero the elapsed time since the action was completed for this move data.
                    Moves[i].DeltaTime = 0;
                }
            }

            // If there was no movement, bring the agent to a stop.
            if (movement == Vector2.zero)
            {
                // Can only slow down at the rate of acceleration but this will instantly stop if there is no acceleration.
                MoveVelocity = Vector2.Lerp(MoveVelocity, Vector2.zero, acceleration * deltaTime);
            
                // After reaching below a velocity threshold, set directly to zero.
                if (MoveVelocity.magnitude < Manager.RestVelocity)
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
        /// Handle receiving an event.
        /// </summary>
        /// <param name="stateEvent">The event to handle.</param>
        /// <returns>True if either the global state or normal state handles the event, false otherwise.</returns>
        private bool HandleEvent(StateEvent stateEvent)
        {
            return state != null && state.HandleEvent(this, stateEvent) || Manager.Mind != null && Manager.Mind.HandleEvent(this, stateEvent);
        }

        /// <summary>
        /// Perform actions.
        /// </summary>
        private void Act()
        {
            if (Actions == null || Actions.Count == 0)
            {
                return;
            }
            
            // Pass all actions to all actuators.
            foreach (Actuator actuator in Actuators)
            {
                actuator.Act(Actions);
            }

            foreach (AgentAction action in Actions.Where(action => action.Complete))
            {
                AddMessage($"Completed action {action}.");
            }

            // Remove actions which were completed.
            Actions = Actions.Where(a => !a.Complete).ToList();
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

        /// <summary>
        /// Remove move data for a transform.
        /// </summary>
        /// <param name="tr">The transform.</param>
        private void RemoveMove(Transform tr)
        {
            Moves = Moves.Where(m => m.Transform != tr).ToList();
        }

        /// <summary>
        /// Remove move data for a position.
        /// </summary>
        /// <param name="pos">The position.</param>
        private void RemoveMove(Vector2 pos)
        {
            Moves = Moves.Where(m => m.Transform == null && m.Position != pos).ToList();
        }
    }
}