using UnityEngine;

/// <summary>
/// Steering behaviours implemented based upon Mat Buckland's Programming Game AI by Example and Dr. Goodwin's Slides.
/// These are static calls using simple parameters so they are not directly tied to agents but are easily implementable by them.
/// </summary>
public static class Steering
{
    /// <summary>
    /// Seek - Move directly towards a position.
    /// Based upon the implementation detailed in Programming Game AI by Example page 91.
    /// </summary>
    /// <param name="position">The position of the agent.</param>
    /// <param name="velocity">The current velocity of the agent.</param>
    /// <param name="evader">The position of the target to seek to.</param>
    /// <param name="speed">The speed at which the agent can move.</param>
    /// <returns>The velocity to apply to the agent to perform the seek.</returns>
    public static Vector2 Seek(Vector2 position, Vector2 velocity, Vector2 evader, float speed)
    {
        return (evader - position).normalized * speed - velocity;
    }

    /// <summary>
    /// Flee - Move directly away from a position.
    /// Based upon the implementation detailed in Programming Game AI by Example page 92.
    /// </summary>
    /// <param name="position">The position of the agent.</param>
    /// <param name="velocity">The current velocity of the agent.</param>
    /// <param name="pursuer">The position of the target to flee from.</param>
    /// <param name="speed">The speed at which the agent can move.</param>
    /// <returns>The velocity to apply to the agent to perform the flee.</returns>
    public static Vector2 Flee(Vector2 position, Vector2 velocity, Vector2 pursuer, float speed)
    {
        // Flee is almost identical to seek except the initial subtraction of positions is reversed.
        return (position - pursuer).normalized * speed - velocity;
    }

    /// <summary>
    /// Pursuit - Move towards a position factoring in its current speed to predict where it is moving.
    /// Based upon the implementation detailed in Programming Game AI by Example page 94.
    /// </summary>
    /// <param name="position">The position of the agent.</param>
    /// <param name="velocity">The current velocity of the agent.</param>
    /// <param name="evader">The position of the target to pursuit to.</param>
    /// <param name="targetLastPosition">The position of the target during the last time step.</param>
    /// <param name="speed">The speed at which the agent can move.</param>
    /// <param name="deltaTime">The time elapsed between when the target is in its current position and its previous</param>
    /// <returns>The velocity to apply to the agent to perform the pursuit.</returns>
    public static Vector2 Pursuit(Vector2 position, Vector2 velocity, Vector2 evader, Vector2 targetLastPosition, float speed, float deltaTime)
    {
        // Get the vector between the agent and the target.
        Vector2 toEvader = evader - position;
        
        // The time to look ahead is equal to the vector magnitude divided by the sum of the speed of both the agent and the target,
        // with the target's speed calculated by determining how far it has traveled during the elapsed time.
        float lookAheadTime = toEvader.magnitude / (speed + Vector2.Distance(evader, targetLastPosition) * deltaTime);
        
        // Seek the predicted target position based upon adding its position to its velocity multiplied by the look ahead time,
        // with the velocity calculated by subtracting the current and previous positions over the elapsed time.
        return Seek(position, velocity, evader + (evader - targetLastPosition) / deltaTime * lookAheadTime, speed);
    }

    /// <summary>
    /// Evade - Move from a position factoring in its current speed to predict where it is moving.
    /// Based upon the implementation detailed in Programming Game AI by Example page 96.
    /// </summary>
    /// <param name="position">The position of the agent.</param>
    /// <param name="velocity">The current velocity of the agent.</param>
    /// <param name="pursuer">The position of the target to evade from.</param>
    /// <param name="pursuerLastPosition">The position of the target during the last time step.</param>
    /// <param name="speed">The speed at which the agent can move.</param>
    /// <param name="deltaTime">The time elapsed between when the target is in its current position and its previous</param>
    /// <returns>The velocity to apply to the agent to perform the evade.</returns>
    public static Vector2 Evade(Vector2 position, Vector2 velocity, Vector2 pursuer, Vector2 pursuerLastPosition, float speed, float deltaTime)
    {
        // Get the vector between the agent and the target.
        Vector2 toPursuer = pursuer - position;
        
        // The time to look ahead is equal to the vector magnitude divided by the sum of the speed of both the agent and the target,
        // with the target's speed calculated by determining how far it has traveled during the elapsed time.
        float lookAheadTime = toPursuer.magnitude / (speed + Vector2.Distance(pursuer, pursuerLastPosition) * deltaTime);
        
        // Flee the predicted target position based upon adding its position to its velocity multiplied by the look ahead time,
        // with the velocity calculated by subtracting the current and previous positions over the elapsed time.
        return Flee(position, velocity, pursuer + (pursuer - pursuerLastPosition) / deltaTime * lookAheadTime, speed);
    }

    /// <summary>
    /// Wander - Randomly adjust forward angle.
    /// Based upon the implementation detailed in Dr. Goodwin's "Steering Behaviours" slides on slide 19.
    /// </summary>
    /// <param name="currentAngle">The current rotation angle of the agent.</param>
    /// <param name="maxWanderTurn">The maximum degrees which the rotation can be adjusted by.</param>
    /// <returns>The new angle the agent should point towards for its wander.</returns>
    public static float Wander(float currentAngle, float maxWanderTurn)
    {
        // Since each random call gives a value from [0.0, 1.0], this is a binomial distribution so values closer to zero are more likely.
        return currentAngle + (Random.value - Random.value) * maxWanderTurn;
    }

    /// <summary>
    /// Face - Face towards a target position.
    /// Custom implementation, not directly based off of any existing code in either Buckland's book or Dr. Goodwin's slides.
    /// Given that Vector2 does not have any RotateTowards method, this method is the only to use Vector3 values.
    /// </summary>
    /// <param name="position">The position of the agent.</param>
    /// <param name="forward">The forward vector the agent is visually facing.</param>
    /// <param name="target">The position to look towards.</param>
    /// <param name="lookSpeed">The maximum degrees the agent can rotate in a second.</param>
    /// <param name="deltaTime">The elapsed time.</param>
    /// <param name="current">The current rotation prior to calling.</param>
    /// <returns>The quaternion of the updated rotation for the agent visuals.</returns>
    public static Quaternion Face(Vector3 position, Vector3 forward, Vector3 target, float lookSpeed, float deltaTime, Quaternion current)
    {
        Vector3 rotation = Vector3.RotateTowards(forward, target - position, lookSpeed * deltaTime, 0.0f);
        return rotation == Vector3.zero || float.IsNaN(rotation.x) || float.IsNaN(rotation.y) || float.IsNaN(rotation.z) ? current : Quaternion.LookRotation(rotation);
    }
}