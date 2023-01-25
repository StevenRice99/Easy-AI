using System;
using System.Reflection;
using EasyAI;

namespace WestWorld.Agents
{
    /// <summary>
    /// Extended agent to store useful information about the West World game.
    /// </summary>
    public abstract class WestWorldAgent : TransformAgent
    {
        /// <summary>
        /// Easy-AI doesn't out-of-the-box have a previous state remembering mechanic.
        /// Here is an example way of storing it in the extended agent class.
        /// If previous states are something you think you may use in your project, looking into working them into the
        /// base agent class could be something you try to do.
        /// </summary>
        private Type _previousStateType;
        
        /// <summary>
        /// Different locations in West World for the agents to be at.
        /// Note these locations are only logical, the agents do not actually move in this demonstration.
        /// </summary>
        public enum WestWorldLocation
        {
            Undefined,
            GoldMine,
            Bank,
            Saloon,
            Home
        }
        
        /// <summary>
        /// Message types for the agents to communicate.
        /// </summary>
        public enum WestWorldMessage
        {
            HiHoneyImHome,
            StewReady
        }

        /// <summary>
        /// The current location an agent is at.
        /// </summary>
        public WestWorldLocation Location { get; private set; } = WestWorldLocation.Undefined;

        /// <summary>
        /// Store the last state of an agent if it is needed to go back to.
        /// Again, this is because Easy-AI doesn't out-of-the-box have a previous state remembering mechanic,
        /// and this is something you may want to improve or make automatic when transitioning all scenes.
        /// </summary>
        public void SaveLastState()
        {
            _previousStateType = State.GetType();
        }

        /// <summary>
        /// Go back to the last state.
        /// Again, this is because Easy-AI doesn't out-of-the-box have a previous state remembering mechanic,
        /// and this is something you may want to improve or make automatic when transitioning all scenes.
        /// This is using a very hacky way of doing so using reflection, and thus why it would be a neat idea
        /// to look into building out a better defined and easier to use system to help with your projects.
        /// </summary>
        public void ReturnToLastState()
        {
            MethodInfo method = GetType().GetMethod("SetState")?.MakeGenericMethod(_previousStateType);
            if (method != null)
            {
                method.Invoke(this, null);
            }
        }

        /// <summary>
        /// Move the agent to a new location.
        /// </summary>
        /// <param name="location">The location to move to.</param>
        public void ChangeLocation(WestWorldLocation location)
        {
            Location = location;
        }
    }
}