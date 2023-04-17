using UnityEngine;

namespace EasyAI
{
    /// <summary>
    /// Agent which moves through a rigidbody.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public class RigidbodyAgent : Agent
    {
        /// <summary>
        /// This agent's rigidbody.
        /// </summary>
        [field: HideInInspector]
        [field: SerializeField]
        public Rigidbody Body { get; private set; }
        
        public override void MovementCalculations()
        {
            CalculateMoveVelocity(Time.fixedDeltaTime);
            Body.velocity = new(MoveVelocity.x, Body.velocity.y, MoveVelocity.y);
        }

        private void OnValidate()
        {
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