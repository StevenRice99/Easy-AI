using System;
using System.Reflection;
using EasyAI;

namespace WestWorld.Agents
{
    public abstract class WestWorldAgent : TransformAgent
    {
        private Type previousStateType;
        
        public enum WestWorldLocation
        {
            Undefined,
            GoldMine,
            Bank,
            Saloon,
            Home
        }
        
        public enum WestWorldMessage
        {
            HiHoneyImHome,
            StewReady
        }

        public WestWorldLocation Location { get; private set; } = WestWorldLocation.Undefined;

        public void SaveLastState()
        {
            previousStateType = State.GetType();
        }

        public void ReturnToLastState()
        {
            MethodInfo method = GetType().GetMethod("SetState")?.MakeGenericMethod(previousStateType);
            method.Invoke(this, null);
        }

        public void ChangeLocation(WestWorldLocation location)
        {
            Location = location;
        }

        public abstract void SendMessage(WestWorldMessage message);

        public abstract void ReceiveMessage(WestWorldMessage message);
    }
}