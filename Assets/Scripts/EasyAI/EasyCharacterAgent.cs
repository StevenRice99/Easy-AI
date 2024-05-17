using UnityEngine;

namespace EasyAI
{
    /// <summary>
    /// Agent which moves through a character controller.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    public class EasyCharacterAgent : EasyTransformAgent
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
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        protected override void Update()
        {
            if (!Alive)
            {
                return;
            }
            
            // Call the base method to update the look rotation.
            base.Update();
            
            // Reset gravity if grounded.
            if (Character.isGrounded)
            {
                _velocityY = 0;
            }
        
            // Apply gravity.
            _velocityY += Physics.gravity.y * Time.deltaTime;

            // Calculate the movement velocity.
            CalculateMoveVelocity();
            
            // Apply the movement to the character controller.
            Vector2 scaled = MoveVelocity * Time.deltaTime;
            Character.Move(new(scaled.x, _velocityY, scaled.y));
        }

        /// <summary>
        /// Editor-only function that Unity calls when the script is loaded or a value changes in the Inspector.
        /// </summary>
        protected override void OnValidate()
        {
            base.OnValidate();
            
            // Get the character controller.
            Character = GetComponent<CharacterController>();
            if (Character == null)
            {
                Character = gameObject.AddComponent<CharacterController>();
            }
        }
    }
}