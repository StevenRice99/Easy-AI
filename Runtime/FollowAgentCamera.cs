using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Camera for following behind an agent.
/// </summary>
[RequireComponent(typeof(Camera))]
public class FollowAgentCamera : MonoBehaviour
{
    [Tooltip("How much to vertically offset the camera for viewing agents.")]
    public float offset = 1;
        
    [SerializeField]
    [Min(0)]
    [Tooltip("How fast the camera should look to the agent for smooth looking. Set to zero for instant camera looking.")]
    private float lookSpeed;
        
    [Min(0)]
    [Tooltip("How fast the camera should move to the agent for smooth movement. Set to zero for instant camera movement.")]
    public float moveSpeed = 5;

    [Min(0)]
    [Tooltip("How far away from the agent should the camera be. Set this, height, and min distance to zero for a first-person camera.")]
    public float depth = 5;

    [Min(0)]
    [Tooltip("How high from the agent should the camera be. Set this, depth and min distance to zero for a first-person camera.")]
    public float height = 5;

    [Min(0)]
    [Tooltip("How close the camera can zoom in to on either its depth or height. Set to zero for ")]
    public float minDistance = 3;

    /// <summary>
    /// The attached camera.
    /// </summary>
    private Camera _camera;

    /// <summary>
    /// Depth relative to the height.
    /// </summary>
    private float _ratio;

    private void Start()
    {
        _camera = GetComponent<Camera>();
        _ratio = depth / height;
            
        // Snap look right away.
        float look = lookSpeed;
        float move = moveSpeed;
        lookSpeed = 0;
        moveSpeed = 0;
        LateUpdate();
        lookSpeed = look;
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
            if (_ratio >= 0)
            {
                depth -= scroll.y * Time.unscaledDeltaTime;
                depth = Mathf.Max(depth, minDistance);
                height = depth / _ratio;
            }
            else
            {
                height -= scroll.y * Time.unscaledDeltaTime;
                height = Mathf.Max(height, minDistance);
                depth = height * _ratio;
            }

            depth = Mathf.Max(depth, 0);
            height = Mathf.Max(height, 0);
        }

        // Determine where to move and look to.
        Transform agentTransform = agent.Visuals == null ? agent.transform : agent.Visuals;
        Vector3 target = agentTransform.position;
        target = new Vector3(target.x, target.y + offset, target.z);

        // Lock to first person if height and distance are zero.
        if (height <= 0 && depth <= 0)
        {
            transform.position = target;
            if (agent.LookingToTarget)
            {
                transform.LookAt(agent.LookTarget);
            }
            else
            {
                transform.rotation = agentTransform.rotation;
            }
            return;
        }
            
        Vector3 move = target + agentTransform.rotation * new Vector3(0, height, -depth);

        Transform t = transform;
        Vector3 position = t.position;

        // Move to the location.
        transform.position = moveSpeed <= 0 ? move : Vector3.Slerp(position, move, moveSpeed * Time.deltaTime);

        // Look at the agent.
        if (lookSpeed <= 0)
        {
            transform.LookAt(target);
        }
        else
        {
            transform.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(target - position), lookSpeed * Time.deltaTime);
        }
    }
}