using UnityEngine;

/// <summary>
/// Agent which moves directly through its transform.
/// </summary>
public class TransformAgent : Agent
{
    /// <summary>
    /// Transform movement.
    /// </summary>
    public override void Move()
    {
        // If the agent should not be moving simply return.
        if (!MovingToTarget)
        {
            MoveVelocity = 0;
            DidMove = false;
            return;
        }
            
        // Calculate how fast we can move this frame.
        CalculateMoveVelocity();

        // Move towards the target position.
        Vector3 position = transform.position;
        Vector3 lastPosition = position;
        position = Vector3.MoveTowards(position, MoveTarget, MoveVelocity * Time.deltaTime);
        DidMove = position != lastPosition;
        transform.position = position;
            
        if (DidMove)
        {
            AddMessage($"Moved towards {MoveTarget}.");
        }
    }
}