using UnityEngine;

namespace Project.Weapons
{
    /// <summary>
    /// Projectile weapon.
    /// </summary>
    public class ProjectileWeapon : Weapon
    {
        [SerializeField]
        [Tooltip("How fast the projectile should travel.")]
        private float velocity = 10;

        [SerializeField]
        [Tooltip("Splash damage distance.")]
        private float distance;
        
        [SerializeField]
        [Tooltip("The bullet prefab.")]
        private GameObject bulletPrefab;
        
        /// <summary>
        /// Fire projectile.
        /// </summary>
        /// <param name="positions">Only returns the weapon barrel.</param>
        protected override void Shoot(out Vector3[] positions)
        {
            // No hit scan impacts so just return the barrel.
            positions = new[] { barrel.position };

            // Create the projectile.
            GameObject projectile = Instantiate(bulletPrefab, Soldier.shootPosition.position, barrel.rotation);
            projectile.name = $"{name} Projectile";
            ProjectileBullet projectileBullet = projectile.GetComponent<ProjectileBullet>();
            projectileBullet.WeaponIndex = Index;
            projectileBullet.ShotBy = Soldier;
            projectileBullet.Damage = damage;
            projectileBullet.Distance = distance;
            projectileBullet.Velocity = velocity;
            
            // Ensure the projectile destroys after its max time.
            Destroy(projectile, time);
        }
    }
}