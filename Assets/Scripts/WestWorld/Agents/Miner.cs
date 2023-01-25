using UnityEngine;
using WestWorld.States;

namespace WestWorld.Agents
{
    public class Miner : WestWorldAgent
    {
        public int GoldCarried { get; private set; }

        public int MoneyInBank { get; private set; }

        public int Thirst { get; private set; }

        public int Fatigue{ get; private set; }

        [SerializeField]
        private int maxGoldCarried = 2;

        [SerializeField]
        private int maxThirst = 5;

        [SerializeField]
        private int maxFatigue = 4;

        public bool PocketsFull => GoldCarried >= maxGoldCarried;

        public bool Thirsty => Thirst >= maxThirst;

        public bool Tired => Fatigue >= maxFatigue;

        public bool Rested => Fatigue <= 0;

        private HouseKeeper _houseKeeper;

        public void AddToGoldCarried(int gold)
        {
            if (Location != WestWorldLocation.GoldMine)
            {
                return;
            }
            
            GoldCarried += gold;
            if (GoldCarried > maxGoldCarried)
            {
                GoldCarried = maxGoldCarried;
            }
        }

        public void IncreaseFatigue(int fatigue)
        {
            if (Location != WestWorldLocation.GoldMine)
            {
                return;
            }
            
            Fatigue += fatigue;
            if (Fatigue >= maxFatigue)
            {
                Fatigue = maxFatigue;
            }
        }

        public void Rest()
        {
            if (Location != WestWorldLocation.Home)
            {
                return;
            }

            Fatigue -= 1;
            if (Fatigue < 0)
            {
                Fatigue = 0;
            }
        }

        public void DepositGold()
        {
            if (Location != WestWorldLocation.Bank)
            {
                return;
            }

            MoneyInBank += GoldCarried;
            GoldCarried = 0;
        }

        public void Drink()
        {
            if (Location == WestWorldLocation.Saloon)
            {
                Thirst = 0;
            }
        }

        public override void SendMessage(WestWorldMessage message)
        {
            _houseKeeper.ReceiveMessage(message);
        }

        public override void ReceiveMessage(WestWorldMessage message)
        {
            if (IsInState<GoHomeAndSleepTillRested>() && message == WestWorldMessage.StewReady)
            {
                SetState<EatStew>();
            }
        }

        protected override void Start()
        {
            base.Start();

            _houseKeeper = FindObjectOfType<HouseKeeper>();
        }

        private void Update()
        {
            Thirst += 1;
            if (Thirst > maxThirst)
            {
                Thirst = maxThirst;
            }
        }
    }
}