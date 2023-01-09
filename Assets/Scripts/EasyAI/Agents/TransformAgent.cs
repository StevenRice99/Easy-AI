using UnityEngine;

namespace EasyAI.Agents
{
    /// <summary>
    /// Agent which moves directly through its transform.
    /// </summary>
    public class TransformAgent : Agent
    {
        /// <summary>
        /// Transform movement.
        /// </summary>
        public override void MovementCalculations()
        {
            CalculateMoveVelocity(Time.deltaTime);
            transform.position += MoveVelocity3 * DeltaTime;
        }
    }
}