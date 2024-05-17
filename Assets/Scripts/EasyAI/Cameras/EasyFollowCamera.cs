using UnityEngine;
using UnityEngine.InputSystem;

namespace EasyAI.Cameras
{
    /// <summary>
    /// Camera for following behind an agent.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class EasyFollowCamera : MonoBehaviour
    {
        /// <summary>
        /// How much to vertically offset the camera for viewing agents.
        /// </summary>
        [Tooltip("How much to vertically offset the camera for viewing agents.")]
        [Min(0)]
        [SerializeField]
        private float offset = 1;
        
        /// <summary>
        /// How fast the camera should look to the agent for smooth looking. Set to zero for instant camera looking.
        /// </summary>
        [Tooltip("How fast the camera should look to the agent for smooth looking. Set to zero for instant camera looking.")]
        [Min(0)]
        [SerializeField]
        private float lookSpeed;
        
        /// <summary>
        /// How fast the camera should move to the agent for smooth movement. Set to zero for instant camera movement.
        /// </summary>
        [Tooltip("How fast the camera should move to the agent for smooth movement. Set to zero for instant camera movement.")]
        [Min(0)]
        [SerializeField]
        private float moveSpeed = 5;

        /// <summary>
        /// How far away from the agent should the camera be. Set this, height, and min distance to zero for a first-person camera.
        /// </summary>
        [Tooltip("How far away from the agent should the camera be. Set this, height, and min distance to zero for a first-person camera.")]
        [Min(0)]
        [SerializeField]
        private float depth = 5;

        /// <summary>
        /// How high from the agent should the camera be. Set this, depth and min distance to zero for a first-person camera.
        /// </summary>
        [Tooltip("How high from the agent should the camera be. Set this, depth and min distance to zero for a first-person camera.")]
        [Min(0)]
        [SerializeField]
        public float height = 5;

        /// <summary>
        /// How close the camera can zoom in to on either its depth or height. Set to zero for a first-person camera.
        /// </summary>
        [Tooltip("How close the camera can zoom in to on either its depth or height. Set to zero for a first-person camera.")]
        [Min(0)]
        [SerializeField]
        private float minDistance = 3;

        /// <summary>
        /// The attached camera.
        /// </summary>
        private Camera _camera;

        /// <summary>
        /// Depth relative to the height.
        /// </summary>
        private float _ratio;

        /// <summary>
        /// Start is called on the frame when a script is enabled just before any of the Update methods are called the first time.
        /// </summary>
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
        
        /// <summary>
        /// LateUpdate is called every frame, if the Behaviour is enabled
        /// </summary>
        private void LateUpdate()
        {
            // Get the agent to look towards.
            EasyAgent easyAgent = EasyManager.CurrentlySelectedAgent;
            if (easyAgent == null)
            {
                if (EasyManager.CurrentAgents.Count > 0)
                {
                    easyAgent = EasyManager.CurrentAgents[0];
                }
                else
                {
                    return;
                }
            }
        
            // Allow for zooming in if this is the selected camera.
            if (EasyManager.SelectedCamera == _camera)
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
            Transform agentTransform = easyAgent.Visuals == null ? easyAgent.transform : easyAgent.Visuals;
            Vector3 target = agentTransform.position;
            target = new(target.x, target.y + offset, target.z);

            // Lock to first person if height and distance are zero.
            if (height <= 0 && depth <= 0)
            {
                transform.position = target;
                if (easyAgent.LookingToTarget)
                {
                    transform.LookAt(easyAgent.LookTarget);
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
            transform.position = moveSpeed <= 0 ? move : Vector3.Slerp(position, move, moveSpeed * Time.unscaledDeltaTime);

            // Look at the agent.
            if (lookSpeed <= 0)
            {
                transform.LookAt(target);
            }
            else
            {
                transform.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(target - position), lookSpeed * Time.unscaledDeltaTime);
            }
        }
    }
}