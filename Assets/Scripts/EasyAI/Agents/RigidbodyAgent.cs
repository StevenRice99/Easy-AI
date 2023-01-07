using UnityEngine;

namespace EasyAI.Agents
{
    /// <summary>
    /// Agent which moves through a rigidbody.
    /// </summary>
    public class RigidbodyAgent : Agent
    {
        /// <summary>
        /// This agent's rigidbody.
        /// </summary>
        protected Rigidbody Rigidbody;

        protected override void Start()
        {
            base.Start();
        
            // Get the rigidbody.
            Rigidbody = GetComponent<Rigidbody>();
            if (Rigidbody == null)
            {
                Rigidbody = gameObject.AddComponent<Rigidbody>();
            }

            if (Rigidbody == null)
            {
                return;
            }

            // Since rotation is all done with the root visuals transform, freeze rigidbody rotation.
            Rigidbody.freezeRotation = true;
            Rigidbody.drag = 0;
            Rigidbody.angularDrag = 0;
            Rigidbody.interpolation = RigidbodyInterpolation.Interpolate;
            Rigidbody.isKinematic = false;
        }
        
        public override void Move()
        {
            if (Rigidbody == null)
            {
                return;
            }
        
            CalculateMoveVelocity(Time.fixedDeltaTime);
            Rigidbody.velocity = new(MoveVelocity.x, Rigidbody.velocity.y, MoveVelocity.y);
        }
    }
}