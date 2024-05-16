using UnityEngine;

namespace EasyAI.Navigation
{
    /// <summary>
    /// Steering behaviours implemented.
    /// These are static calls using simple parameters, so they are not directly tied to agents but are easily implementable by them.
    /// </summary>
    public static class EasySteering
    {
        /// <summary>
        /// The various move behaviours available for agents.
        /// </summary>
        public enum Behaviour : byte
        {
            Seek,
            Flee,
            Pursue,
            Evade
        }

        /// <summary>
        /// Perform a move.
        /// </summary>
        /// <param name="behaviour">The type of move.</param>
        /// <param name="position">The position of the agent.</param>
        /// <param name="velocity">The current velocity of the agent.</param>
        /// <param name="targetCurrent">The current position of the target.</param>
        /// <param name="targetLast">The last position of the target if needed.</param>
        /// <param name="speed">The speed at which the agent can move.</param>
        /// <param name="deltaTime">The time elapsed between when the target is in its current position and its previous if needed.</param>
        /// <returns>Calculated movement.</returns>
        public static Vector2 Move(Behaviour behaviour, Vector2 position, Vector2 velocity, Vector2 targetCurrent, Vector2 targetLast, float speed, float deltaTime)
        {
            switch (behaviour)
            {
                case Behaviour.Evade:
                    return Evade(position, velocity, targetCurrent, speed, Speed(targetCurrent, targetLast, deltaTime), Velocity(targetCurrent, targetLast, deltaTime));
                case Behaviour.Pursue:
                    return Pursue(position, velocity, targetCurrent, speed, Speed(targetCurrent, targetLast, deltaTime), Velocity(targetCurrent, targetLast, deltaTime));
                case Behaviour.Flee:
                    return Flee(position, velocity, targetCurrent, speed);
                case Behaviour.Seek:
                default:
                    return Seek(position, velocity, targetCurrent, speed);
            }
        }

        /// <summary>
        /// Check if this is an approaching or moving away behaviour
        /// </summary>
        /// <param name="behaviour">The behaviour to check</param>
        /// <returns>True if it is an approaching behaviour, false otherwise</returns>
        public static bool IsApproachingBehaviour(Behaviour behaviour)
        {
            return behaviour is Behaviour.Seek or Behaviour.Pursue;
        }

        /// <summary>
        /// Check if a move is complete.
        /// </summary>
        /// <param name="behaviour">The move type</param>
        /// <param name="position">The position of the agent.</param>
        /// <param name="target">The desired destination position.</param>
        /// <returns>True if the move is complete, false otherwise.</returns>
        public static bool IsMoveComplete(Behaviour behaviour, Vector2 position, Vector2 target)
        {
            return !IsApproachingBehaviour(behaviour)
                ? EasyManager.FleeDistance >= 0 &&
                  Vector2.Distance(position, target) >= EasyManager.FleeDistance
                : EasyManager.SeekDistance >= 0 &&
                  Vector2.Distance(position, target) <= EasyManager.SeekDistance;
        }

        /// <summary>
        /// The color to make a certain move type appear with gizmos.
        /// Note that although not listed here, white displays all paths and yellow displays velocity.
        /// </summary>
        /// <param name="behaviour">The behaviour type.</param>
        /// <returns>The color to display.</returns>
        public static Color GizmosColor(Behaviour behaviour)
        {
            switch (behaviour)
            {
                case Behaviour.Evade:
                    return new(1f, 0.65f, 0f);
                case Behaviour.Pursue:
                    return Color.cyan;
                case Behaviour.Flee:
                    return Color.red;
                case Behaviour.Seek:
                default:
                    return Color.green;
            }
        }

        /// <summary>
        /// Get the speed in units per second.
        /// </summary>
        /// <param name="current">Current position.</param>
        /// <param name="previous">Position at the previous time step.</param>
        /// <param name="deltaTime">The time elapsed between when the target is in its current position and its previous.</param>
        /// <returns>The speed in units per second.</returns>
        private static float Speed(Vector2 current, Vector2 previous, float deltaTime)
        {
            return Vector2.Distance(current, previous) * deltaTime;
        }

        /// <summary>
        /// Get the velocity across axis.
        /// </summary>
        /// <param name="current">Current position.</param>
        /// <param name="previous">Position at the previous time step.</param>
        /// <param name="deltaTime">The time elapsed between when the target is in its current position and its previous.</param>
        /// <returns>The velocity across axis</returns>
        private static Vector2 Velocity(Vector2 current, Vector2 previous, float deltaTime)
        {
            return (current - previous) / deltaTime;
        }
        
        /// <summary>
        /// Seek - Move directly towards a position.
        /// </summary>
        /// <param name="position">The position of the agent.</param>
        /// <param name="velocity">The current velocity of the agent.</param>
        /// <param name="evader">The position of the evader to seek to.</param>
        /// <param name="speed">The speed at which the agent can move.</param>
        /// <returns>The velocity to apply to the agent to perform seek.</returns>
        private static Vector2 Seek(Vector2 position, Vector2 velocity, Vector2 evader, float speed)
        {
            return (evader - position).normalized * speed - velocity;
        }

        /// <summary>
        /// Flee - Move directly away from a position.
        /// </summary>
        /// <param name="position">The position of the agent.</param>
        /// <param name="velocity">The current velocity of the agent.</param>
        /// <param name="pursuer">The position of the pursuer to flee from.</param>
        /// <param name="speed">The speed at which the agent can move.</param>
        /// <returns>The velocity to apply to the agent to perform flee.</returns>
        private static Vector2 Flee(Vector2 position, Vector2 velocity, Vector2 pursuer, float speed)
        {
            // Flee is almost identical to seek except the initial subtraction of positions is reversed.
            return (position - pursuer).normalized * speed - velocity;
        }

        /// <summary>
        /// Pursue - Move towards a position factoring in its current speed to predict where it is moving.
        /// </summary>
        /// <param name="position">The position of the agent.</param>
        /// <param name="velocity">The current velocity of the agent.</param>
        /// <param name="evader">The position of the evader to pursue.</param>
        /// <param name="speed">The speed at which the agent can move.</param>
        /// <param name="evaderSpeed">The movement speed for the evader to pursue.</param>
        /// <param name="evaderVelocity">The movement velocity across axis for the evader to pursue.</param>
        /// <returns>The velocity to apply to the agent to perform pursue.</returns>
        private static Vector2 Pursue(Vector2 position, Vector2 velocity, Vector2 evader, float speed, float evaderSpeed, Vector2 evaderVelocity)
        {
            return Seek(position, velocity, evader + evaderVelocity * ((evader - position).magnitude / (speed + evaderSpeed)), speed);
        }

        /// <summary>
        /// Evade - Move from a position factoring in its current speed to predict where it is moving.
        /// </summary>
        /// <param name="position">The position of the agent.</param>
        /// <param name="velocity">The current velocity of the agent.</param>
        /// <param name="pursuer">The position of the pursuer to evade.</param>
        /// <param name="speed">The speed at which the agent can move.</param>
        /// <param name="pursuerSpeed">The movement speed for the pursuer to evade.</param>
        /// <param name="pursuerVelocity">The movement velocity across axis for the pursuer to evade.</param>
        /// <returns>The velocity to apply to the agent to perform evade.</returns>
        private static Vector2 Evade(Vector2 position, Vector2 velocity, Vector2 pursuer, float speed, float pursuerSpeed, Vector2 pursuerVelocity)
        {
            return Flee(position, velocity, pursuer + pursuerVelocity * ((pursuer - position).magnitude / (speed + pursuerSpeed)), speed);
        }
    }
}