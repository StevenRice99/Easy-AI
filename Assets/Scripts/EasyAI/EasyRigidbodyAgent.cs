using UnityEngine;

namespace EasyAI
{
    /// <summary>
    /// Agent which moves through a rigidbody.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class EasyRigidbodyAgent : EasyAgent
    {
        /// <summary>
        /// This agent's rigidbody.
        /// </summary>
        [field: HideInInspector]
        [field: SerializeField]
        public Rigidbody Body { get; private set; }
        
        /// <summary>
        /// Frame-rate independent MonoBehaviour. FixedUpdate message for physics calculations.
        /// </summary>
        protected override void FixedUpdate()
        {
            if (!Alive)
            {
                return;
            }
            
            // Call the base method to perform the logic of the agent.
            base.FixedUpdate();
            
            // Calculate the movement velocity.
            CalculateMoveVelocity();
            
            // Set the velocity of the rigidbody.
            Body.velocity = new(MoveVelocity.x, Body.velocity.y, MoveVelocity.y);
        }

        /// <summary>
        /// Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();
            
            // Get the rigidbody.
            Body = GetComponent<Rigidbody>();
            if (Body == null)
            {
                Body = gameObject.AddComponent<Rigidbody>();
            }

            // Since rotation is all done with the root visuals transform, freeze rigidbody rotation.
            Body.freezeRotation = true;
            Body.drag = 0;
            Body.angularDrag = 0;
            Body.interpolation = RigidbodyInterpolation.Interpolate;
            Body.isKinematic = false;
        }
    }
}