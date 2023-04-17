using UnityEngine;

namespace EasyAI
{
    /// <summary>
    /// Agent which moves through a character controller.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public class CharacterAgent : TransformAgent
    {
        /// <summary>
        /// This agent's character controller.
        /// </summary>
        [field: HideInInspector]
        [field: SerializeField]
        public CharacterController Character { get; private set; }

        /// <summary>
        /// Used to manually apply gravity.
        /// </summary>
        private float _velocityY;

        /// <summary>
        /// Character controller movement.
        /// </summary>
        public override void MovementCalculations()
        {
            // Reset gravity if grounded.
            if (Character.isGrounded)
            {
                _velocityY = 0;
            }
        
            // Apply gravity.
            _velocityY += Physics.gravity.y * Time.deltaTime;

            CalculateMoveVelocity(Time.deltaTime);
            Vector2 scaled = MoveVelocity * Time.deltaTime;
            Character.Move(new(scaled.x, _velocityY, scaled.y));
        }

        private void OnValidate()
        {
            // Get the character controller.
            Character = GetComponent<CharacterController>();
            if (Character == null)
            {
                Character = gameObject.AddComponent<CharacterController>();
            }
        }
    }
}