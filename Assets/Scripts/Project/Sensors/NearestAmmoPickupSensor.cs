using EasyAI;
using Project.Pickups;
using UnityEngine;

namespace Project.Sensors
{
    [DisallowMultipleComponent]
    public class NearestAmmoPickupSensor : Sensor
    {
        public override object Sense()
        {

            if (Agent is not Soldier soldier)
            {
                return null;
            }
            
            HealthWeaponPickup selected = null;

            int priority = int.MaxValue;
            
            for (int i = 0; i < soldier.WeaponPriority.Length; i++)
            {
                if (soldier.Weapons[i].MaxAmmo < 0 || soldier.Weapons[i].Ammo >= soldier.Weapons[i].MaxAmmo)
                {
                    continue;
                }

                if (selected != null && priority <= soldier.WeaponPriority[i])
                {
                    continue;
                }
                
                selected = SoldierManager.NearestAmmoPickup(soldier, i);
                if (selected != null)
                {
                    priority = soldier.WeaponPriority[i];
                }
            }

            return selected;
        }
    }
}