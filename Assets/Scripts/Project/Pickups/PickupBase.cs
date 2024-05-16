using System.Linq;
using UnityEngine;

namespace Project.Pickups
{
    /// <summary>
    /// Base class for pickups for the soldiers.
    /// </summary>
    [DisallowMultipleComponent]
    public abstract class PickupBase : MonoBehaviour
    {
        /// <summary>
        /// Implement behaviour for when picked up.
        /// </summary>
        /// <param name="soldier">The soldier.</param>
        /// <param name="ammo">The ammo array of the soldier.</param>
        protected abstract void OnPickedUp(Soldier soldier, int[] ammo);
        
        /// <summary>
        /// When a GameObject collides with another GameObject, Unity calls OnTriggerEnter.
        /// </summary>
        /// <param name="other">The trigger that was entered.</param>
        private void OnTriggerEnter(Collider other)
        {
            DetectPickup(other);
        }

        /// <summary>
        /// OnTriggerStay is called once per physics update for every Collider other that is touching the trigger.
        /// </summary>
        /// <param name="other">The trigger that this is in.</param>
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
            Soldier soldier = other.gameObject.GetComponent<Soldier>();
            if (soldier != null && soldier.Alive)
            {
                OnPickedUp(soldier, soldier.Weapons.Select(w => w.Ammo).ToArray());
            }
        }
    }
}