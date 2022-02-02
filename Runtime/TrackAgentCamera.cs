using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Camera for tracking above an agent.
/// </summary>
[RequireComponent(typeof(Camera))]
public class TrackAgentCamera : MonoBehaviour
{
    [Min(0)]
    [Tooltip("How fast the camera should move to the agent for smooth movement. Set to zero for instant camera movement.")]
    public float moveSpeed = 5;

    [Min(0)]
    [Tooltip("How high from the agent should the camera be.")]
    public float height = 10;

    [Min(0)]
    [Tooltip("How low the camera can zoom in to.")]
    public float minHeight = 3;

    /// <summary>
    /// The attached camera.
    /// </summary>
    private Camera _camera;
        
    private void Start()
    {
        _camera = GetComponent<Camera>();
            
        // Snap look right away.
        float move = moveSpeed;
        moveSpeed = 0;
        LateUpdate();
        moveSpeed = move;
    }
        
    private void LateUpdate()
    {
        // Get the agent to look towards.
        Agent agent = AgentManager.Singleton.SelectedAgent;
        if (agent == null)
        {
            if (AgentManager.Singleton.Agents.Count > 0)
            {
                agent = AgentManager.Singleton.Agents[0];
            }
            else
            {
                return;
            }
        }

        // Allow for zooming in if this is the selected camera.
        if (AgentManager.Singleton.selectedCamera == _camera)
        {
            Vector2 scroll = Mouse.current.scroll.ReadValue();
            height -= scroll.y * Time.unscaledDeltaTime;
            height = Mathf.Max(height, minHeight);
        }

        // Move over the agent.
        Vector3 target = (agent.Visuals == null ? agent.transform : agent.Visuals).position;
        target = new Vector3(target.x, target.y + height, target.z);
        transform.position = moveSpeed <= 0 ? target : Vector3.Slerp(transform.position, target, moveSpeed * Time.deltaTime);
    }
}