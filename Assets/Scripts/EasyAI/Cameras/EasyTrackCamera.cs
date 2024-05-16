using UnityEngine;
using UnityEngine.InputSystem;

namespace EasyAI.Cameras
{
    /// <summary>
    /// Camera for tracking above an agent.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class EasyTrackCamera : MonoBehaviour
    {
        /// <summary>
        /// How fast the camera should move to the agent for smooth movement. Set to zero for instant camera movement.
        /// </summary>
        [Tooltip("How fast the camera should move to the agent for smooth movement. Set to zero for instant camera movement.")]
        [Min(0)]
        [SerializeField]
        private float moveSpeed = 5;

        /// <summary>
        /// How high from the agent should the camera be.
        /// </summary>
        [Tooltip("How high from the agent should the camera be.")]
        [Min(0)]
        [SerializeField]
        private float height = 10;

        /// <summary>
        /// How low the camera can zoom in to.
        /// </summary>
        [Tooltip("How low the camera can zoom in to.")]
        [Min(0)]
        [SerializeField]
        private float minHeight = 3;

        /// <summary>
        /// The attached camera.
        /// </summary>
        private Camera _camera;

        /// <summary>
        /// The target position to look at.
        /// </summary>
        private Vector3 _target;
        
        /// <summary>
        /// Start is called on the frame when a script is enabled just before any of the Update methods are called the first time.
        /// </summary>
        private void Start()
        {
            _camera = GetComponent<Camera>();
            
            // Snap look right away.
            float move = moveSpeed;
            moveSpeed = 0;
            LateUpdate();
            moveSpeed = move;
        }
        
        /// <summary>
        /// LateUpdate is called every frame, if the Behaviour is enabled.
        /// </summary>
        private void LateUpdate()
        {
            // Get the agent to look towards.
            EasyAgent easyAgent = EasyManager.CurrentlySelectedEasyAgent;
            if (easyAgent == null)
            {
                if (EasyManager.CurrentAgents.Count > 0)
                {
                    easyAgent = EasyManager.CurrentAgents[0];
                }
            }

            if (easyAgent != null)
            {
                _target = (easyAgent.Visuals == null ? easyAgent.transform : easyAgent.Visuals).position;
            }

            // Allow for zooming in if this is the selected camera.
            if (EasyManager.SelectedCamera == _camera)
            {
                Vector2 scroll = Mouse.current.scroll.ReadValue();
                height -= scroll.y * Time.unscaledDeltaTime;
                height = Mathf.Max(height, minHeight);
            }

            // Move over the agent.
            Vector3 position = new(_target.x, _target.y + height, _target.z);
            transform.position = moveSpeed <= 0 ? position : Vector3.Slerp(transform.position, position, moveSpeed * Time.unscaledDeltaTime);
        }
    }
}