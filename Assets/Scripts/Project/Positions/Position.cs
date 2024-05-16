using UnityEngine;

namespace Project.Positions
{
    /// <summary>
    /// Base class for points that soldiers can interact with.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public abstract class Position : MonoBehaviour
    {
        /// <summary>
        /// If this is for the red team.
        /// </summary>
        public bool RedTeam => redTeam;
        
        [Tooltip("If this is for the red team.")]
        [SerializeField]
        private bool redTeam = true;
        
        /// <summary>
        /// How many soldiers are within this point.
        /// </summary>
        protected int Count;
        
        /// <summary>
        /// Start is called on the frame when a script is enabled just before any of the Update methods are called the first time.
        /// </summary>
        private void Start()
        {
            // Ensure it is a trigger.
            GetComponent<BoxCollider>().isTrigger = true;
        }

        /// <summary>
        /// When a GameObject collides with another GameObject, Unity calls OnTriggerEnter.
        /// </summary>
        /// <param name="other">The trigger that was entered.</param>
        private void OnTriggerEnter(Collider other)
        {
            ++Count;
        }

        /// <summary>
        /// OnTriggerExit is called when the Collider other has stopped touching the trigger.
        /// </summary>
        /// <param name="other">The trigger that was exited.</param>
        private void OnTriggerExit(Collider other)
        {
            --Count;
        }
    }
}