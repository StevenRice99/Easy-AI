using System.Linq;
using Project.Agents;
using UnityEngine;

namespace Project.Pickups
{
    /// <summary>
    /// Base class for pickups for the soldiers.
    /// </summary>
    public abstract class PickupBase : MonoBehaviour
    {
        /// <summary>
        /// Implement behaviour for when picked up.
        /// </summary>
        /// <param name="soldier">The soldier.</param>
        /// <param name="ammo">The ammo array of the soldier.</param>
        protected abstract void OnPickedUp(SoldierAgent soldier, int[] ammo);
        
        private void OnTriggerEnter(Collider other)
        {
            DetectPickup(other);
        }

        private void OnTriggerStay(Collider other)
        {
            DetectPickup(other);
        }

        /// <summary>
        /// Detect when picked up.
        /// </summary>
        /// <param name="other">The object collided with.</param>
        private void DetectPickup(Component other)
        {
            // If a soldier, pick it up.
            SoldierAgent soldier = other.gameObject.GetComponent<SoldierAgent>();
            if (soldier != null && soldier.Alive)
            {
                OnPickedUp(soldier, soldier.Weapons.Select(w => w.Ammo).ToArray());
            }
        }
    }
}