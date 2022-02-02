namespace Samples
{
    /// <summary>
    /// An example mind which simply tells its agent to move to and look at the target it gets from its percept.
    /// </summary>
    public class SampleMind : Mind
    {
        /// <summary>
        /// Tell the agent to move to a position.
        /// </summary>
        /// <param name="percepts">The percepts which the agent's sensors sensed.</param>
        /// <returns>Nothing as this agent is very simple with no actions outside of moving.</returns>
        public override Action[] Think(Percept[] percepts)
        {
            // Even though this sample agent has only a single percept, in larger agents you will likely need to loop
            // through your percepts.
            foreach (Percept percept in percepts)
            {
                // Attempt to cast the percept into a SamplePercept.
                if (!(percept is SamplePercept samplePercept))
                {
                    continue;
                }

                // Move to and look at the given position.
                MoveToLookAtTarget(samplePercept.Position);
                return null;
            }

            return null;
        }
    }
}