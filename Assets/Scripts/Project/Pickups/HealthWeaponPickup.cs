using System.Collections;
using Project.Agents;
using Project.Managers;
using UnityEngine;

namespace Project.Pickups
{
    /// <summary>
    /// Pickup for health and weapons.
    /// </summary>
    public class HealthWeaponPickup : PickupBase
    {
        /// <summary>
        /// How fast to spin its visuals in degrees per second.
        /// </summary>
        private const float Speed = 180;
        
        [SerializeField]
        [Tooltip("Set to below 0 to be a health pickup, otherwise the weapon index of the player.")]
        public int weaponIndex = -1;

        [SerializeField]
        [Tooltip("The visuals object to rotate.")]
        private Transform visuals;
        
        /// <summary>
        /// If the pickup is ready to be picked up.
        /// </summary>
        public bool Ready { get; set; } = true;
        
        /// <summary>
        /// All visuals of the pickup.
        /// </summary>
        private MeshRenderer[] _meshRenderers;
        
        /// <summary>
        /// Add health or ammo on pickup.
        /// </summary>
        /// <param name="soldier">The soldier.</param>
        /// <param name="ammo">The ammo array of the soldier.</param>
        protected override void OnPickedUp(SoldierAgent soldier, int[] ammo)
        {
            // If not ready to be pickup up do nothing.
            if (!Ready)
            {
                return;
            }

            // If it was a health pickup, heal if the soldier is not at full health.
            if (weaponIndex < 0)
            {
                if (soldier.Health >= SoldierAgentManager.SoldierAgentManagerSingleton.health)
                {
                    return;
                }
                
                soldier.AddMessage("Picked up health.");
            
                soldier.Heal();
                StartCoroutine(ReadyDelay());

                return;
            }

            // Replenish ammo if needed.
            if (soldier.Weapons.Length <= weaponIndex || soldier.Weapons[weaponIndex].maxAmmo < 0 || ammo[weaponIndex] >= soldier.Weapons[weaponIndex].maxAmmo)
            {
                return;
            }
            
            soldier.AddMessage((SoldierAgent.WeaponChoices) weaponIndex switch
            {
                SoldierAgent.WeaponChoices.MachineGun => "Replenished machine gun.",
                SoldierAgent.WeaponChoices.Shotgun => "Replenished shotgun.",
                SoldierAgent.WeaponChoices.Sniper => "Replenished sniper.",
                SoldierAgent.WeaponChoices.RocketLauncher => "Replenished rocket launcher.",
                _=> "Replenished pistol."
            });
            
            soldier.Weapons[weaponIndex].Replenish();
            StartCoroutine(ReadyDelay());
        }
        
        private void Start()
        {
            // Grab all meshes.
            _meshRenderers = GetComponentsInChildren<MeshRenderer>();
        }

        private void Update()
        {
            // Spin the visuals.
            visuals.Rotate(0, Speed * Time.deltaTime, 0, Space.Self);
        }

        /// <summary>
        /// Make the pickup not available for a given period of time.
        /// </summary>
        /// <returns>Nothing.</returns>
        private IEnumerator ReadyDelay()
        {
            Ready = false;
            ToggleMeshes();
            
            yield return new WaitForSeconds(SoldierAgentManager.SoldierAgentManagerSingleton.pickupTimer);
            
            Ready = true;
            ToggleMeshes();
        }

        /// <summary>
        /// Toggle all meshes on or off.
        /// </summary>
        private void ToggleMeshes()
        {
            foreach (MeshRenderer meshRenderer in _meshRenderers)
            {
                meshRenderer.enabled = Ready;
            }
        }
    }
}