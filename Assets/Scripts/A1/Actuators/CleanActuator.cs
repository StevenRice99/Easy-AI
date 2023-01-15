using EasyAI;
using UnityEngine;

namespace A1.Actuators
{
    /// <summary>
    /// Actuator to clean a floor tile.
    /// </summary>
    [DisallowMultipleComponent]
    public class CleanActuator : Actuator
    {
        [SerializeField]
        [Min(0)]
        [Tooltip("The time in seconds it takes to clean a floor tile.")]
        private float timeToClean = 0.25f;

        [SerializeField]
        [Tooltip("Dirt particles system to display when cleaning.")]
        private ParticleSystem dirtParticles;

        /// <summary>
        /// How long the floor tile has been getting cleaned for.
        /// </summary>
        private float _timeSpentCleaning;

        /// <summary>
        /// Clean a floor tile.
        /// </summary>
        /// <param name="agentAction">The action to perform.</param>
        /// <returns>True if the floor tile has finished being cleaned, false otherwise.</returns>
        public override bool Act(object agentAction)
        {
            // Only act if there is a clean action.
            if (agentAction is not CleanAction cleanAction)
            {
                return false;
            }

            // This should never happen, but check just in case.
            if (cleanAction.Floor == null)
            {
                Log("Unable to clean current floor tile.");
                DisableParticles();
                return false;
            }

            // Increment how long the floor has been getting cleaned for.
            _timeSpentCleaning += Agent.DeltaTime;

            // If the tile has not been cleaned long enough, return false as it has not finished getting cleaned.
            if (_timeSpentCleaning < timeToClean)
            {
                Log("Cleaning current floor tile.");
                EnableParticles();
                return false;
            }
            
            // The floor has finished being cleaned so reset the time spent cleaning.
            Log("Finished cleaning current floor tile.");
            _timeSpentCleaning = 0;
            cleanAction.Floor.Clean();
            DisableParticles();
            return true;
        }

        protected override void Start()
        {
            base.Start();
            DisableParticles();
        }

        /// <summary>
        /// Start the cleaning particles.
        /// </summary>
        private void EnableParticles()
        {
            if (dirtParticles != null && !dirtParticles.isPlaying)
            {
                dirtParticles.Play();
            }
        }

        /// <summary>
        /// Stop the cleaning particles.
        /// </summary>
        private void DisableParticles()
        {
            if (dirtParticles != null && dirtParticles.isPlaying)
            {
                dirtParticles.Stop();
            }
        }
    }
}