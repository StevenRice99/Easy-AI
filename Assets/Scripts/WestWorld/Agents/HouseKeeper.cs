using WestWorld.States;

namespace WestWorld.Agents
{
    public class HouseKeeper : WestWorldAgent
    {
        private Miner _miner;
        
        public override void SendMessage(WestWorldMessage message)
        {
            _miner.ReceiveMessage(message);
        }

        public override void ReceiveMessage(WestWorldMessage message)
        {
            SaveLastState();
            if (message == WestWorldMessage.HiHoneyImHome)
            {
                Log("Hi honey. Let me make you some of mah fine country stew.");
                SetState<CookStew>();
            }
            else
            {
                _miner.ReceiveMessage(WestWorldMessage.StewReady);
            }
        }

        protected override void Start()
        {
            base.Start();

            _miner = FindObjectOfType<Miner>();
        }
    }
}