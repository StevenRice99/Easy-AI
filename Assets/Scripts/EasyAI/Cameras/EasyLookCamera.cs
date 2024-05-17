using UnityEngine;

namespace EasyAI.Cameras
{
    /// <summary>
    /// Camera for looking at an agent from a set position.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Camera))]
    public class EasyLookCamera : MonoBehaviour
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
        private float lookSpeed = 5;

        /// <summary>
        /// Start is called on the frame when a script is enabled just before any of the Update methods are called the first time.
        /// </summary>
        private void Start()
        {
            // Snap look right away.
            float look = lookSpeed;
            lookSpeed = 0;
            LateUpdate();
            lookSpeed = look;
        }

        /// <summary>
        /// LateUpdate is called every frame, if the Behaviour is enabled.
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
            
            // Determine where to look at.
            Vector3 target = easyAgent.Visuals == null ? easyAgent.transform.position : easyAgent.Visuals.position;
            target = new(target.x, target.y + offset, target.z);

            // Look instantly.
            if (lookSpeed <= 0)
            {
                transform.LookAt(target);
                return;
            }

            // Look towards it.
            Transform t = transform;
            transform.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(target - t.position), lookSpeed * Time.unscaledDeltaTime);
        }
    }
}