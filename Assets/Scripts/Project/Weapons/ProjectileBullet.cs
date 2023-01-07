using System.Collections.Generic;
using System.Linq;
using Project.Agents;
using UnityEngine;

namespace Project.Weapons
{
    /// <summary>
    /// Projectile weapon.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    [RequireComponent(typeof(Rigidbody))]
    public class ProjectileBullet : MonoBehaviour
    {
        /// <summary>
        /// The soldier that shot the weapon.
        /// </summary>
        public SoldierAgent ShotBy { get; set; }

        /// <summary>
        /// The weapon index of the soldier.
        /// </summary>
        public int WeaponIndex { get; set; }
        
        /// <summary>
        /// The velocity to travel at.
        /// </summary>
        public float Velocity { get; set; }
        
        /// <summary>
        /// The maximum damage to deal.
        /// </summary>
        public int Damage { get; set; }

        /// <summary>
        /// How far away to deal splash damage.
        /// </summary>
        public float Distance { get; set; }

        /// <summary>
        /// The rigidbody attached to the projectile.
        /// </summary>
        private Rigidbody _rb;
        
        private void Start()
        {
            // Disable collisions with the soldier that shot it.
            Collider col = GetComponent<Collider>();
            foreach (Collider hitBox in ShotBy.Colliders)
            {
                if (hitBox != null && hitBox.enabled)
                {
                    Physics.IgnoreCollision(col, hitBox, true);
                }
            }
            
            // Add velocity.
            _rb = GetComponent<Rigidbody>();
            _rb.useGravity = false;
            _rb.AddRelativeForce(Vector3.forward * Velocity, ForceMode.VelocityChange);
        }

        private void OnCollisionEnter(Collision collision)
        {
            HandleCollision(collision.transform);
        }
        
        /// <summary>
        /// Handle the collision.
        /// </summary>
        /// <param name="tr">The transform which was hit.</param>
        private void HandleCollision(Transform tr)
        {
            // See if a soldier was hit.
            SoldierAgent attacked;
            do
            {
                attacked = tr.GetComponent<SoldierAgent>();
                tr = tr.parent;
            } while (attacked == null && tr != null);

            // If an enemy was hit, damage them.
            if (attacked != null && attacked.RedTeam != ShotBy.RedTeam)
            {
                attacked.Damage(Damage, ShotBy);
            }
            
            // Calculate splash damage if there is some.
            if (Distance > 0)
            {
                int layerMask = LayerMask.GetMask("Default", "Obstacle", "Ground", "Projectile", "HitBox");

                // Loop through all enemies.
                foreach (SoldierAgent soldier in FindObjectsOfType<SoldierAgent>().Where(p => p != ShotBy && p.RedTeam != ShotBy.RedTeam && p != attacked).ToArray())
                {
                    // Get the points of every collider of an enemy.
                    Collider[] hitBoxes = soldier.GetComponentsInChildren<Collider>().Where(c => c.gameObject.layer == LayerMask.NameToLayer("HitBox")).ToArray();
                    Vector3 position = soldier.transform.position;
                    List<Vector3> points = new() { position, new(position.x, position.y + 0.1f, position.z), soldier.shootPosition.position };
                    points.AddRange(hitBoxes.Select(h => h.bounds).Select(b => b.ClosestPoint(transform.position)));
                
                    // Loop through every point and if one hits, deal damage with falloff.
                    foreach (Vector3 point in points.Where(p => Vector3.Distance(p, transform.position) <= Distance).OrderBy(p => Vector3.Distance(p, transform.position)))
                    {
                        if (!Physics.Linecast(transform.position, point, out RaycastHit hit, layerMask) || !hitBoxes.Contains(hit.collider))
                        {
                            continue;
                        }
                        
                        soldier.Damage(Mathf.Max((int) (Damage * (1 - Vector3.Distance(point, transform.position) / Distance)), 1), ShotBy);
                        break;
                    }
                }
            }

            // Create the impact effect and audio.
            Vector3 p = transform.position;
            ShotBy.Weapons[WeaponIndex].ImpactAudio(p, 1);
            ShotBy.Weapons[WeaponIndex].ImpactVisual(p, Vector3.zero);
            
            // Destroy the projectile.
            Destroy(gameObject);
        }
    }
}