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
        /// Transform movement.
        /// </summary>
        public override void MovementCalculations()
        {
            CalculateMoveVelocity();
            transform.position += MoveVelocity3 * Time.deltaTime;
        }
    }
}