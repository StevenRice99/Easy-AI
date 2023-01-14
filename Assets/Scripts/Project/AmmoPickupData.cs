using UnityEngine;

namespace Project
{
    public struct AmmoPickupData
    {
        public Vector3? Destination;

        public int WeaponId;

        public AmmoPickupData(Vector3? destination, int weaponId)
        {
            Destination = destination;
            WeaponId = weaponId;
        }
    }
}