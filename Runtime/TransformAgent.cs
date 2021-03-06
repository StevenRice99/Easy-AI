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
        CalculateMoveVelocity(Time.deltaTime);
        transform.position += MoveVelocity3 * DeltaTime;
    }
}