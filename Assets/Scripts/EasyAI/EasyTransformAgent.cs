using UnityEngine;

namespace EasyAI
{
    /// <summary>
    /// Agent which moves directly through its transform.
    /// </summary>
    [DisallowMultipleComponent]
    public class EasyTransformAgent : EasyAgent
    {
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
            
            // Calculate the movement velocity.
            CalculateMoveVelocity();
            
            // Set the position accordingly.
            transform.position += MoveVelocity3 * Time.deltaTime;
        }
    }
}