using UnityEngine;

namespace WestWorld.Agents
{
    /// <summary>
    /// The Miner, Bob, in the West World game.
    /// </summary>
    public class Miner : WestWorldAgent
    {
        /// <summary>
        /// The money the miner has in total at the bank.
        /// </summary>
        public int MoneyInBank { get; private set; }

        /// <summary>
        /// If the miner cannot carry any more gold.
        /// </summary>
        public bool PocketsFull => _goldCarried >= maxGoldCarried;

        /// <summary>
        /// If the miner is feeling thirsty.
        /// </summary>
        public bool Thirsty => _thirst >= maxThirst;

        /// <summary>
        /// If the miner is feeling tired.
        /// </summary>
        public bool Tired => _fatigue >= maxFatigue;

        /// <summary>
        /// If the miner is feeling rested.
        /// </summary>
        public bool Rested => _fatigue <= 0;

        [Header("Miner Properties")]
        [Tooltip("The maximum amount of gold the miner can carry before their pockets are full.")]
        [SerializeField]
        private int maxGoldCarried = 2;

        [Tooltip("How thirsty the miner can get before they need a drink.")]
        [SerializeField]
        private int maxThirst = 5;

        [Tooltip("How tired the miner can get before they need to rest.")]
        [SerializeField]
        private int maxFatigue = 4;
        
        /// <summary>
        /// The current gold the miner is carrying.
        /// </summary>
        private int _goldCarried;

        /// <summary>
        /// The current thirst of the miner.
        /// </summary>
        private int _thirst;

        /// <summary>
        /// The current fatigue of the miner.
        /// </summary>
        private int _fatigue;

        /// <summary>
        /// Collect more gold to carry.
        /// </summary>
        public void AddToGoldCarried()
        {
            // Can only collect at the gold mine.
            if (Location != WestWorldLocation.GoldMine)
            {
                return;
            }
            
            // Increase the gold carried and cap it.
            _goldCarried += 1;
            if (_goldCarried > maxGoldCarried)
            {
                _goldCarried = maxGoldCarried;
            }
        }

        /// <summary>
        /// Increase the exhaustion of the miner.
        /// </summary>
        public void IncreaseFatigue()
        {
            // Can only get tired when working at the gold mine.
            if (Location != WestWorldLocation.GoldMine)
            {
                return;
            }
            
            // Increase the fatigue and cap it.
            _fatigue += 1;
            if (_fatigue >= maxFatigue)
            {
                _fatigue = maxFatigue;
            }
        }

        /// <summary>
        /// Decrease the exhaustion of the miner.
        /// </summary>
        public void Rest()
        {
            // Can only rest at home.
            if (Location != WestWorldLocation.Home)
            {
                return;
            }

            // Decrease the fatigue and cap it.
            _fatigue -= 1;
            if (_fatigue < 0)
            {
                _fatigue = 0;
            }
        }

        /// <summary>
        /// Deposit all gold into the bank.
        /// </summary>
        public void DepositGold()
        {
            // Can only deposit at the bank.
            if (Location != WestWorldLocation.Bank)
            {
                return;
            }

            // Add all gold to the bank and empty the gold carried.
            MoneyInBank += _goldCarried;
            _goldCarried = 0;
        }

        /// <summary>
        /// Quench thirst.
        /// </summary>
        public void Drink()
        {
            // Can only drink at the saloon.
            if (Location == WestWorldLocation.Saloon)
            {
                _thirst = 0;
            }
        }

        /// <summary>
        /// Increase thirst and cap it.
        /// </summary>
        public void IncreaseThirst()
        {
            _thirst += 1;
            if (_thirst > maxThirst)
            {
                _thirst = maxThirst;
            }
        }
    }
}