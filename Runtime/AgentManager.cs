using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Singleton to handle agents and GUI rendering. Must be exactly one of this or an extension of this present in every scene.
/// </summary>
public class AgentManager : MonoBehaviour
{
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
    /// Determine what gizmos lines are drawn.
    /// Off - No lines are drawn.
    /// All - Every line from every agent, sensor, and actuator is drawn.
    /// Selected - If an agent is selected, only it and its sensors and actuators are drawn. If an individual sensor or actuator is selected, only it is drawn.
    /// </summary>
    public enum GizmosState : byte
    {
        Off,
        All,
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
    /// The width of the GUI buttons to open their respective menus when they are closed.
    /// </summary>
    private const float ClosedSize = 70;

    /// <summary>
    /// The singleton agent manager.
    /// </summary>
    public static AgentManager Singleton;

    /// <summary>
    /// The auto-generated material for displaying lines.
    /// </summary>
    private static Material _lineMaterial;

    [SerializeField]
    [Min(0)]
    [Tooltip("The maximum number of agents which can be updated in a single frame. Set to zero to be unlimited.")]
    private int maxAgentsPerUpdate;

    [SerializeField]
    [Min(0)]
    [Tooltip("The maximum number of messages any component can hold.")]
    private int maxMessages = 100;
        
    [SerializeField]
    [Min(0)]
    [Tooltip("How wide the details list is. Set to zero to disable details list rendering.")]
    private float detailsWidth = 500;
        
    [SerializeField]
    [Min(0)]
    [Tooltip("How wide the controls list is. Set to zero to disable controls list rendering.")]
    private float controlsWidth = 120;

    /// Determine what gizmos lines are drawn.
    /// Off - No lines are drawn.
    /// All - Every line from every agent, sensor, and actuator is drawn.
    /// Selected - If an agent is selected, only it and its sensors and actuators are drawn. If an individual sensor or actuator is selected, only it is drawn.
        
    [SerializeField]
    [Tooltip(
        "Determine what gizmos lines are drawn.\n" +
        "Off - No lines are drawn.\n" +
        "All - Every line from every agent, sensor, and actuator is drawn.\n" +
        "Selected - If an agent is selected, only it and its sensors and actuators are drawn. If an individual sensor or actuator is selected, only it is drawn."
    )]
    private GizmosState gizmos;

    [SerializeField]
    [Tooltip("The currently selected camera. Set this to start with that camera active. Leaving empty will default to the first camera by alphabetic order.")]
    public Camera selectedCamera;

    /// <summary>
    /// Getter for the maximum number of messages any component can hold.
    /// </summary>
    public int MaxMessages => maxMessages;

    /// <summary>
    /// If the scene is currently playing or not.
    /// </summary>
    public bool Playing => !_stepping && Time.timeScale > 0;

    /// <summary>
    /// The current message mode.
    /// </summary>
    public MessagingMode MessageMode { get; private set; }
        
    /// <summary>
    /// The global messages.
    /// </summary>
    public List<string> GlobalMessages { get; private set; } = new List<string>();

    /// <summary>
    /// The currently selected agent.
    /// </summary>
    public Agent SelectedAgent { get; private set; }

    /// <summary>
    /// All agents in the scene.
    /// </summary>
    public List<Agent> Agents { get; } = new List<Agent>();

    /// <summary>
    /// All cameras in the scene.
    /// </summary>
    public Camera[] Cameras { get; protected set; } = Array.Empty<Camera>();

    /// <summary>
    /// All agents which move during an update tick.
    /// </summary>
    private List<Agent> UpdateAgents = new List<Agent>();

    /// <summary>
    /// All agents which move during a fixed update tick.
    /// </summary>
    private List<Agent> FixedUpdateAgents = new List<Agent>();
        
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
    /// Resume playing.
    /// </summary>
    public static void Resume()
    {
        Time.timeScale = 1;
    }

    /// <summary>
    /// Pause playing.
    /// </summary>
    public static void Pause()
    {
        Time.timeScale = 0;
    }

    public void RefreshAgents()
    {
        foreach (Agent agent in Agents)
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
        return !(y + h > Screen.height) && GUI.Button(new Rect(x, y, w, h), message);
    }
        
    /// <summary>
    /// Render a GUI label.
    /// </summary>
    /// <param name="x">X rendering position. In most cases this should remain unchanged.</param>
    /// <param name="y">Y rendering position. In most cases this should remain unchanged.</param>
    /// <param name="w">Width of components. In most cases this should remain unchanged.</param>
    /// <param name="h">Height of components. In most cases this should remain unchanged.</param>
    /// <param name="message">The message to display.</param>
    public static void GuiLabel(float x, float y, float w, float h, float p, string message)
    {
        if (y + h > Screen.height)
        {
            return;
        }
            
        GUI.Label(new Rect(x + p, y, w - p, h), message);
    }

    /// <summary>
    /// Render a GUI box.
    /// </summary>
    /// <param name="x">X rendering position. In most cases this should remain unchanged.</param>
    /// <param name="y">Y rendering position. In most cases this should remain unchanged.</param>
    /// <param name="w">Width of components. In most cases this should remain unchanged.</param>
    /// <param name="h">Height of components. In most cases this should remain unchanged.</param>
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
            
        GUI.Box(new Rect(x,y,w,(h + p) * number - p), string.Empty);
    }

    /// <summary>
    /// Determine the updated Y value for the next GUI to be placed with.
    /// </summary>
    /// <param name="x">X rendering position. In most cases this should remain unchanged.</param>
    /// <param name="y">Y rendering position. In most cases this should remain unchanged.</param>
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
    public void AddGlobalMessage(string message)
    {
        switch (MessageMode)
        {
            case MessagingMode.Compact when GlobalMessages.Count > 0 && GlobalMessages[0] == message:
                return;
            case MessagingMode.Unique:
                GlobalMessages = GlobalMessages.Where(m => m != message).ToList();
                break;
        }

        GlobalMessages.Insert(0, message);
        if (GlobalMessages.Count > Singleton.MaxMessages)
        {
            GlobalMessages.RemoveAt(GlobalMessages.Count - 1);
        }
    }

    /// <summary>
    /// Register an agent with the agent manager.
    /// </summary>
    /// <param name="agent">The agent to add.</param>
    public void AddAgent(Agent agent)
    {
        // Ensure the agent is only added once.
        if (Agents.Contains(agent))
        {
            return;
        }
            
        // Add to their movement handling list.
        Agents.Add(agent);
        switch (agent)
        {
            case TransformAgent updateAgent:
                UpdateAgents.Add(updateAgent);
                break;
            case RigidbodyAgent fixedUpdateAgent:
                FixedUpdateAgents.Add(fixedUpdateAgent);
                break;
        }
            
        // If the agent had any cameras attached to it we need to add them.
        FindCameras();
    }

    /// <summary>
    /// Remove an agent from the agent manager.
    /// </summary>
    /// <param name="agent">The agent to remove.</param>
    public void RemoveAgent(Agent agent)
    {
        // This should always be true as agents are added at their creation but check just in case.
        if (!Agents.Contains(agent))
        {
            return;
        }

        // Remove the agent and update the current index accordingly so no agents are skipped in Update.
        int index = Agents.IndexOf(agent);
        Agents.Remove(agent);
        if (_currentAgentIndex > index)
        {
            _currentAgentIndex--;
        }
        if (_currentAgentIndex < 0 || _currentAgentIndex >= Agents.Count)
        {
            _currentAgentIndex = 0;
        }

        // Remove from their movement handling list.
        switch (agent)
        {
            case TransformAgent updateAgent:
            {
                if (UpdateAgents.Contains(updateAgent))
                {
                    UpdateAgents.Remove(updateAgent);
                }

                break;
            }
            case RigidbodyAgent fixedUpdateAgent:
            {
                if (FixedUpdateAgents.Contains(fixedUpdateAgent))
                {
                    FixedUpdateAgents.Remove(fixedUpdateAgent);
                }

                break;
            }
        }

        // If the agent had any cameras attached to it we need to remove them.
        FindCameras();
    }

    /// <summary>
    /// Find all cameras in the scene so buttons can be setup for them.
    /// </summary>
    public void FindCameras()
    {
        Cameras = FindObjectsOfType<Camera>().OrderBy(c => c.name).ToArray();
    }

    /// <summary>
    /// Change to the next messaging mode.
    /// </summary>
    public void ChangeMessageMode()
    {
        if (MessageMode == MessagingMode.Unique)
        {
            MessageMode = MessagingMode.All;
        }
        else
        {
            MessageMode++;
        }

        if (MessageMode == MessagingMode.Unique)
        {
            ClearMessages();
        }
    }

    /// <summary>
    /// Change the messaging mode.
    /// </summary>
    /// <param name="mode">The mode to change to.</param>
    public void ChangeMessageMode(MessagingMode mode)
    {
        MessageMode = mode;
    }

    /// <summary>
    /// Change to the next gizmos state.
    /// </summary>
    public void ChangeGizmosState()
    {
        if (gizmos == GizmosState.Selected)
        {
            gizmos = GizmosState.Off;
            return;
        }

        gizmos++;
    }

    /// <summary>
    /// Change the gizmos state.
    /// </summary>
    /// <param name="state">The state to change to.</param>
    public void ChangeGizmosState(GizmosState state)
    {
        gizmos = state;
    }

    /// <summary>
    /// Step for a single frame.
    /// </summary>
    public void Step()
    {
        StartCoroutine(StepOneFrame());
    }

    /// <summary>
    /// Clear all messages.
    /// </summary>
    public void ClearMessages()
    {
        GlobalMessages.Clear();
        foreach (IntelligenceComponent component in FindObjectsOfType<IntelligenceComponent>())
        {
            component.ClearMessages();
        }
    }

    /// <summary>
    /// Switch to a camera.
    /// </summary>
    /// <param name="cam">The camera to switch to.</param>
    public void SwitchCamera(Camera cam)
    {
        selectedCamera = cam;
        cam.enabled = true;
        foreach (Camera cam2 in Cameras)
        {
            if (cam != cam2)
            {
                cam2.enabled = false;
            }
        }
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
        FindCameras();
        if (selectedCamera != null)
        {
            SwitchCamera(selectedCamera);
        }
        else if (Cameras.Length > 0)
        {
            SwitchCamera(Cameras[0]);
        }
        else
        {
            EasyAIStatic.CreateFollowAgentCamera();
            EasyAIStatic.CreateTrackAgentCamera();
            FindCameras();
            SwitchCamera(Cameras[0]);
        }
    }

    protected virtual void Update()
    {
        // Perform for all agents if there is no limit or only the next allowable number of agents if there is.
        if (maxAgentsPerUpdate <= 0)
        {
            foreach (Agent agent in Agents)
            {
                agent.Perform();
            }
        }
        else
        {
            for (int i = 0; i < maxAgentsPerUpdate && i < Agents.Count; i++)
            {
                Agents[_currentAgentIndex].Perform();
                NextAgent();
            }
        }

        // Update the delta time for all agents and look towards their targets.
        foreach (Agent agent in Agents)
        {
            agent.DeltaTime += Time.deltaTime;
            agent.Look();
        }
            
        // Move agents that do not require physics.
        MoveAgents(UpdateAgents);
    }

    protected void FixedUpdate()
    {
        // Move agents that require physics.
        MoveAgents(FixedUpdateAgents);
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

    /// <summary>
    /// Handle moving of agents.
    /// </summary>
    /// <param name="agents">The agents to move.</param>
    private static void MoveAgents(List<Agent> agents)
    {
        foreach (Agent agent in agents)
        {
            agent.Move();
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
        Shader shader = Shader.Find("Hidden/Internal-Colored");
        _lineMaterial = new Material(shader)
        {
            hideFlags = HideFlags.HideAndDontSave
        };
            
        // Turn on alpha blending.
        _lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            
        // Turn backface culling off.
        _lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            
        // Turn off depth writes.
        _lineMaterial.SetInt("_ZWrite", 0);
    }

    /// <summary>
    /// Display gizmos for an agent.
    /// </summary>
    /// <param name="agent">The agent to display gizmos for.</param>
    private static void AgentGizmos(Agent agent)
    {
        if (agent.SelectedMind != null)
        {
            agent.SelectedMind.DisplayGizmos();
        }

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
        // Nothing to do if gizmos are turned off.
        if (gizmos == GizmosState.Off)
        {
            return;
        }
            
        LineMaterial();
        _lineMaterial.SetPass(0);

        GL.PushMatrix();
        GL.MultMatrix(transform.localToWorldMatrix);
        GL.Begin(GL.LINES);

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
    private void Render(float x, float y, float h, float p)
    {
        if (detailsWidth > 0)
        {
            RenderDetails(x, y, detailsWidth, h, p);
        }

        if (controlsWidth > 0)
        {
            RenderControls(x, y, controlsWidth, h, p);
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
    private float RenderMessageOptions(float x, float y, float w, float h, float p)
    {
        // Button to change messaging mode.
        y = NextItem(y, h, p);
        if (GuiButton(x, y, w / 2 - p, h, MessageMode switch
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
    private void RenderDetails(float x, float y, float w, float h, float p)
    {
        if (Agents.Count < 1)
        {
            return;
        }

        if (!_detailsOpen)
        {
            w = ClosedSize;
        }

        if (w + 4 * p > Screen.width)
        {
            w = Screen.width - 4 * p;
        }
            
        // Button open/close details.
        if (GuiButton(x, y, w, h, _detailsOpen ? "Close" : "Details"))
        {
            _detailsOpen = !_detailsOpen;
        }
            
        if (!_detailsOpen)
        {
            return;
        }

        if (SelectedAgent == null && _state == GuiState.Agent || _selectedComponent == null && _state == GuiState.Component)
        {
            _state = GuiState.Main;
        }

        if (_state == GuiState.Main && Agents.Count == 1)
        {
            SelectedAgent = Agents[0];
            _state = GuiState.Agent;
        }

        // Handle agent view rendering.
        if (_state == GuiState.Agent)
        {
            // Can only go to the main view if there is more than one agent.
            if (Agents.Count > 1)
            {
                // Button to go back to the main view.
                y = NextItem(y, h, p);
                if (GuiButton(x, y, w, h, "Back to Overview"))
                {
                    _state = GuiState.Main;
                }
            }
                
            RenderAgent(x, y, w, h, p);

            return;
        }

        // Handle components view rendering.
        if (_state == GuiState.Components)
        {
            // Button to go back to the agents view.
            y = NextItem(y, h, p);
            if (GuiButton(x, y, w, h, $"Back to {SelectedAgent.name}"))
            {
                _state = GuiState.Agent;
            }
            else
            {
                RenderComponents(x, y, w, h, p);
            }

            return;
        }

        // Handle the component view.
        if (_state == GuiState.Component)
        {
            // Button to go back to the components view.
            y = NextItem(y, h, p);
            if (GuiButton(x, y, w, h, $"Back to {SelectedAgent.name} Sensors and Actuators"))
            {
                _selectedComponent = null;
                _state = GuiState.Components;
            }
                
            RenderComponent(x, y, w, h, p);
            return;
        }

        // Display all agents.
        y = NextItem(y, h, p);
        GuiBox(x, y, w, h, p, 1);
        GuiLabel(x, y, w, h, p, $"{Agents.Count} Agents:");

        foreach (Agent agent in Agents)
        {
            // Button to select an agent.
            y = NextItem(y, h, p);
            if (!GuiButton(x, y, w, h, $"{agent.name} - {agent}" + (agent.SelectedMind == null ? " - No Mind." : $" - {agent.SelectedMind}")))
            {
                continue;
            }

            SelectedAgent = agent;
            _state = GuiState.Agent;
        }
            
        // Display global messages.
        if (GlobalMessages.Count == 0)
        {
            y = NextItem(y, h, p);
            GuiBox(x, y, w, h, p, 1);
            GuiLabel(x, y, w, h, p, $"No messages.");
            return;
        }
            
        y = RenderMessageOptions(x, y, w, h, p);
            
        y = NextItem(y, h, p);
        GuiBox(x, y, w, h, p, GlobalMessages.Count);
            
        foreach (string message in GlobalMessages)
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
    private void RenderAgent(float x, float y, float w, float h, float p)
    {
        if (SelectedAgent == null)
        {
            _state = GuiState.Main;
            return;
        }
            
        y = NextItem(y, h, p);
        int length = 4;
        if (Agents.Count > 1)
        {
            length++;
        }
            
        // Display all agent details.
        GuiBox(x, y, w, h, p, length);
        if (Agents.Count > 1)
        {
            GuiLabel(x, y, w, h, p, SelectedAgent.name);
            y = NextItem(y, h, p);
        }

        GuiLabel(x, y, w, h, p, $"Type: {SelectedAgent}");
        y = NextItem(y, h, p);
        Mind mind = SelectedAgent.SelectedMind;
        GuiLabel(x, y, w, h, p, (mind != null ? $"Mind: {mind}" : "Mind: None") + $" | Performance: {SelectedAgent.Performance}");
        y = NextItem(y, h, p);
        GuiLabel(x, y, w, h, p, $"Position: {SelectedAgent.Position} | " + (SelectedAgent.MovingToTarget ? $"Moving to {SelectedAgent.MoveTarget} at {SelectedAgent.MoveVelocity} units/second." : "Not moving."));
        y = NextItem(y, h, p);
        GuiLabel(x, y, w, h, p, $"Rotation: {SelectedAgent.Rotation.eulerAngles.y} | " + (SelectedAgent.LookingToTarget ? $"Looking to {SelectedAgent.LookTarget} at {SelectedAgent.LookVelocity} degrees/second." : "Not looking."));

        // Display any custom details implemented for the mind.
        if (mind != null)
        {
            y = SelectedAgent.SelectedMind.DisplayDetails(x, y, w, h, p);
        }

        // Display all sensors for the agent.
        if (SelectedAgent.Sensors.Length > 0 && SelectedAgent.Actuators.Length > 0)
        {
            y = NextItem(y, h, p);
            if (GuiButton(x, y, w, h, "Sensors, Actuators, Percepts, and Actions"))
            {
                _state = GuiState.Components;
            }
        }

        if (mind == null)
        {
            return;
        }

        // Display all messages for the agent.
        y = RenderMessageOptions(x, y, w, h, p);
            
        y = NextItem(y, h, p);
        GuiBox(x, y, w, h, p, !mind.HasMessages ? 1 : mind.MessageCount);

        if (!mind.HasMessages)
        {
            GuiLabel(x, y, w, h, p, "No messages.");
            return;
        }
            
        foreach (string message in mind.Messages)
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
    private void RenderComponents(float x, float y, float w, float h, float p)
    {
        if (SelectedAgent == null)
        {
            _state = GuiState.Main;
            return;
        }
            
        // List all sensors.
        y = NextItem(y, h, p);
        GuiBox(x, y, w, h, p, 1);
        GuiLabel(x, y, w, h, p, SelectedAgent.Sensors.Length switch
        {
            0 => "No Sensors",
            1 => "1 Sensor",
            _ => $"{SelectedAgent.Sensors.Length} Sensors"
        });

        foreach (Sensor sensor in SelectedAgent.Sensors)
        {
            // Button to select a sensor.
            y = NextItem(y, h, p);
            if (!GuiButton(x, y, w, h, sensor.ToString()))
            {
                continue;
            }

            _selectedComponent = sensor;
            _state = GuiState.Component;
        }
            
        // Display all actuators.
        y = NextItem(y, h, p);
        GuiBox(x, y, w, h, p, 1);
        GuiLabel(x, y, w, h, p, SelectedAgent.Actuators.Length switch
        {
            0 => "No Actuators",
            1 => "1 Actuator",
            _ => $"{SelectedAgent.Actuators.Length} Actuators"
        });
            
        foreach (Actuator actuator in SelectedAgent.Actuators)
        {
            // Button to select an actuator.
            y = NextItem(y, h, p);
            if (!GuiButton(x, y, w, h, actuator.ToString()))
            {
                continue;
            }

            _selectedComponent = actuator;
            _state = GuiState.Component;
        }
            
        // Display all percepts.
        Percept[] percepts = SelectedAgent.Percepts.Where(p => p != null).ToArray();
        if (percepts.Length > 0)
        {
            y = NextItem(y, h, p);
            GuiBox(x, y, w, h, p, 1 + percepts.Length);
            
            GuiLabel(x, y, w, h, p, percepts.Length == 1 ? "1 Percept" :$"{percepts.Length} Percepts");

            foreach (Percept percept in percepts)
            {
                string msg = percept.DetailsDisplay();
                y = NextItem(y, h, p);
                GuiLabel(x, y, w, h, p, percept + (string.IsNullOrWhiteSpace(msg) ? string.Empty : $": {msg}"));
            }
        }

        // Display all actions.
        Action[] actions = SelectedAgent.Actions?.Where(a => a != null).ToArray();
        if (actions != null && actions.Length > 0)
        {
            y = NextItem(y, h, p);
            GuiBox(x, y, w, h, p, 1 + actions.Length);
            
            GuiLabel(x, y, w, h, p, actions.Length == 1 ? "1 Action" :$"{actions.Length} Actions");

            foreach (Action action in actions)
            {
                string msg = action.DetailsDisplay();
                y = NextItem(y, h, p);
                GuiLabel(x, y, w, h, p, action + (string.IsNullOrWhiteSpace(msg) ? string.Empty : $": {msg}"));
            }
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
    private void RenderComponent(float x, float y, float w, float h, float p)
    {
        if (_selectedComponent == null)
        {
            _state = GuiState.Components;
            return;
        }
            
        // Display component details.
        y = NextItem(y, h, p);
        GuiBox(x, y, w, h, p, 1);
        GuiLabel(x, y, w, h, p, $"{SelectedAgent.name} | {_selectedComponent}");
            
        // Display any custom details implemented for the component.
        y = _selectedComponent.DisplayDetails(x, y, w, h, p);
            
        // Display component messages.
        if (!_selectedComponent.HasMessages)
        {
            GuiBox(x, y, w, h, p, 1);
            GuiLabel(x, y, w, h, p, "No messages.");
            return;
        }
            
        y = RenderMessageOptions(x, y, w, h, p);
            
        y = NextItem(y, h, p);
        GuiBox(x, y, w, h, p, _selectedComponent.MessageCount);

        foreach (string message in _selectedComponent.Messages)
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
    private void RenderControls(float x, float y, float w, float h, float p)
    {
        if (!_controlsOpen)
        {
            w = ClosedSize;
        }
            
        if (Agents.Count == 0 && w + 4 * p > Screen.width)
        {
            w = Screen.width - 4 * p;
        }

        if (Agents.Count > 0 && Screen.width < (_detailsOpen ? detailsWidth : ClosedSize) + controlsWidth + 5 * p)
        {
            return;
        }
            
        x = Screen.width - x - w;

        // Button open/close controls.
        if (GuiButton(x, y, w, h, _controlsOpen ? "Close" : "Controls"))
        {
            _controlsOpen = !_controlsOpen;
        }
            
        if (!_controlsOpen)
        {
            return;
        }

        y = NextItem(y, h, p);
        y = CustomRendering(x, y, w, h, p);

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
        if (GuiButton(x, y, w, h, gizmos switch
            {
                GizmosState.Off => "Gizmos: Off",
                GizmosState.Selected => "Gizmos: Selected",
                _ => "Gizmos: All"
            }))
        {
            ChangeGizmosState();
        }

        if (Cameras.Length > 1)
        {
            // Buttons to switch cameras.
            foreach (Camera cam in Cameras)
            {
                y = NextItem(y, h, p);
                if (GUI.Button(new Rect(x, y, w, h), cam.name))
                {
                    SwitchCamera(cam);
                }
            }
        }

        if (SceneManager.sceneCountInBuildSettings > 1)
        {
            // Display button to go to the next scene.
            y = NextItem(y, h, p);
            if (GUI.Button(new Rect(x, y, w, h), "Next Scene"))
            {
                NextScene();
            }

            if (SceneManager.sceneCountInBuildSettings > 2)
            {
                // Display button to go to the previous scene.
                y = NextItem(y, h, p);
                if (GUI.Button(new Rect(x, y, w, h), "Last Scene"))
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
            UnityEditor.EditorApplication.ExitPlaymode();
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
    /// Coroutine lasts for exactly one frame to step though each time step.
    /// </summary>
    /// <returns>Nothing.</returns>
    private IEnumerator StepOneFrame()
    {
        _stepping = true;
        Resume();
        yield return 0;
        Pause();
        _stepping = false;
    }
}