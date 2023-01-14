using EasyAI;
using UnityEngine;

namespace Project.Sensors
{
    [DisallowMultipleComponent]
    public class NearestAmmoPickupSensor : Sensor
    {
        protected override object Sense()
        {
            int weaponIndex = 0;
            Vector3? destination = null;

            if (Agent is not Soldier soldier)
            {
                return new AmmoPickupData(destination, weaponIndex);
            }

            int priority = int.MaxValue;
            
            for (int i = 0; i < soldier.WeaponPriority.Length; i++)
            {
                if (soldier.Weapons[i].maxAmmo < 0 || soldier.Weapons[i].Ammo >= soldier.Weapons[i].maxAmmo)
                {
                    continue;
                }

                if (destination != null && priority <= soldier.WeaponPriority[i])
                {
                    continue;
                }

                weaponIndex = i;
                priority = soldier.WeaponPriority[i];
                destination = SoldierManager.NearestAmmoPickup(soldier, i);
            }

            return new AmmoPickupData(destination, weaponIndex);
        }
    }
}