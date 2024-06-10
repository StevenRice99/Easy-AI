using System.Linq;
using EasyAI;
using Project.Pickups;
using Project.Sensors;
using UnityEngine;

namespace Project.States
{
    /// <summary>
    /// The global state which soldiers are always in.
    /// </summary>
    [CreateAssetMenu(menuName = "Project/States/Soldier Mind", fileName = "Soldier Mind")]
    public class SoldierMind : EasyState
    {
        /// <summary>
        /// How much to consider low health.
        /// </summary>
        [Tooltip("How much to consider low health.")]
        [SerializeField]
        private int lowHealth;

        /// <summary>
        /// How close in units is an enemy considered close.
        /// </summary>
        [Tooltip("How close in units is an enemy considered close.")]
        [Min(0)]
        [SerializeField]
        private float distanceClose = 10;

        /// <summary>
        /// How far in units is an enemy considered far.
        /// </summary>
        [Tooltip("How far in units is an enemy considered far.")]
        [Min(0)]
        [SerializeField]
        private float distanceFar = 20;
        
        /// <summary>
        /// Called when an agent is in this state.
        /// </summary>
        /// <param name="agent">The agent.</param>
        public override void Execute(EasyAgent agent)
        {
            if (agent is not Soldier soldier)
            {
                return;
            }
            
            // Choose a target.
            TargetNearestEnemy(soldier);

            // Choose the optimal weapon to use.
            PrioritizeWeapons(soldier);

            // Chose to move somewhere.
            ChooseDestination(soldier);
        }

        /// <summary>
        /// Decide who to target.
        /// </summary>
        /// <param name="soldier">The solider.</param>
        private static void TargetNearestEnemy(Soldier soldier)
        {
            // If no enemies are detected, return null so the soldier will just look where it is walking.
            if (soldier.DetectedEnemies.Count == 0)
            {
                soldier.NoTarget();
                return;
            }
            
            // For all detected enemies, prioritize who to take aim at.
            // 1. If the enemy is visible.
            // 2. If the enemy has the flag.
            // 3. How recently seen/heard the enemy was.
            // 4. How close the enemy is.
            Soldier.EnemyMemory target = soldier.DetectedEnemies.OrderBy(e => e.Visible).ThenBy(e => e.HasFlag).ThenBy(e => Vector3.Distance(soldier.transform.position, e.Position)).ThenBy(e => e.DeltaTime).First();
            
            // Define the target based upon the most ideal enemy to aim at.
            soldier.SetTarget(new()
            {
                Enemy = target.Enemy,
                Position = target.Position,
                Visible = target.Visible
            });
        }

        /// <summary>
        /// Prioritize what weapons to use in a given situation.
        /// </summary>
        /// <param name="soldier">The soldier.</param>
        private void PrioritizeWeapons(Soldier soldier)
        {
            if (soldier.CarryingFlag)
            {
                soldier.Log("Carrying flag, using pistol to run fast.");
                soldier.SetWeaponPriority(
                    shotgun: 2,
                    machineGun: 3,
                    rocketLauncher: 4,
                    sniper: 5,
                    pistol: 1
                );
                return;
            }
            
            // If there is no target to choose a weapon based off of, predict what weapon type will be needed.
            if (soldier.Target == null)
            {
                // Defenders predict needing to use long range weapons like snipers.
                if (soldier.Role == Soldier.SoliderRole.Defender)
                {
                    soldier.Log("No targets, prioritizing sniper.");
                    soldier.SetWeaponPriority(
                        sniper:1,
                        rocketLauncher:2,
                        machineGun:3,
                        shotgun:4,
                        pistol:5
                    );
                    return;
                }
                
                // Attackers and the collector predict needing to use short range weapons like shotguns.
                soldier.Log("No targets, prioritizing shotgun.");
                soldier.SetWeaponPriority(
                    shotgun: 1,
                    machineGun: 2,
                    rocketLauncher: 3,
                    sniper: 4,
                    pistol: 5
                );
                return;
            }

            // Determine how far away from the target enemy the soldier is.
            float distance = soldier.DistanceTarget;
            
            // Target is far away, use long range weapons.
            if (distance >= distanceFar)
            {
                // Defenders use the sniper first.
                if (soldier.Role == Soldier.SoliderRole.Defender)
                {
                    soldier.Log("Far target, prioritizing sniper.");
                    soldier.SetWeaponPriority(
                        sniper:1,
                        rocketLauncher:2,
                        machineGun:3,
                        pistol:4,
                        shotgun:5
                    );
                    return;
                }
                
                // Attackers and the collector use the rocket launcher first.
                soldier.Log("Far target, prioritizing rocket launcher.");
                soldier.SetWeaponPriority(
                    rocketLauncher:1,
                    machineGun:2,
                    sniper:3,
                    pistol:4,
                    shotgun:5
                );
                return;
            }

            // If close range, all roles use close-range weapons first.
            if (distance <= distanceClose)
            {
                soldier.Log("Close target, prioritizing shotgun.");
                soldier.SetWeaponPriority(
                    shotgun:1,
                    machineGun:2,
                    pistol:3,
                    rocketLauncher:4,
                    sniper:5
                );
                return;
            }
            
            soldier.Log("Medium target, prioritizing machine gun.");
            
            // Otherwise, it is medium range, with the only difference being defenders using a sniper before a shotgun.
            if (soldier.Role == Soldier.SoliderRole.Defender)
            {
                soldier.SetWeaponPriority(
                    machineGun:1,
                    rocketLauncher:2,
                    shotgun:3,
                    sniper:4,
                    pistol:5
                );
                return;
            }
            
            soldier.SetWeaponPriority(
                machineGun:1,
                rocketLauncher:2,
                sniper:3,
                shotgun:4,
                pistol:5
            );
        }
        
        /// <summary>
        /// Choose where the soldier should move to.
        /// </summary>
        /// <param name="soldier">The soldier.</param>
        private void ChooseDestination(Soldier soldier)
        {
            // If carrying the flag, attempt to move directly back to base.
            if (soldier.CarryingFlag)
            {
                soldier.Move(soldier.BasePosition);
                soldier.Log("Have the flag, returning it to base.");
                return;
            }

            switch (soldier.Role)
            {
                // If the flag collector, move to collect the enemy flag.
                case Soldier.SoliderRole.Collector:
                    soldier.Move(soldier.EnemyFlagPosition);
                    soldier.Log("Moving to collect enemy flag.");
                    return;
                
                // If a defender and the flag has been taken, move to it to kill the enemy flag carried and return it.
                case Soldier.SoliderRole.Defender when !soldier.FlagAtBase:
                    soldier.Move(soldier.TeamFlagPosition);
                    soldier.Log("Moving to return flag.");
                    return;

                case Soldier.SoliderRole.Attacker:
                case Soldier.SoliderRole.Defender:
                default:
                    // If the soldier has low health, move to a health pack to heal.
                    if (soldier.Health <= lowHealth)
                    {
                        HealthAmmoPickup health = soldier.Sense<HealthSensor, HealthAmmoPickup>();
                        if (health != null)
                        {
                            soldier.Move(health.transform.position);
                            soldier.Log("Moving to pickup health.");
                            return;
                        }
                    }

                    // Decisions when the soldier's current target enemy is not visible.
                    if (soldier.Target is not { Visible: true })
                    {
                        // Try to heal.
                        if (soldier.Health <= lowHealth)
                        {
                            HealthAmmoPickup health = soldier.Sense<HealthSensor, HealthAmmoPickup>();
                            if (health != null)
                            {
                                soldier.Move(health.transform.position);
                                soldier.Log("Moving to pickup health.");
                                return;
                            }
                        }

                        HealthAmmoPickup ammo = soldier.Sense<AmmoSensor, HealthAmmoPickup>();
                        if (ammo != null)
                        {
                            soldier.Move(ammo.transform.position);
                            soldier.Log("Moving to pickup ammo for " + (Soldier.WeaponIndexes) ammo.weaponIndex switch
                            {
                                Soldier.WeaponIndexes.MachineGun => "machine gun.",
                                Soldier.WeaponIndexes.Shotgun => "shotgun.",
                                Soldier.WeaponIndexes.Sniper => "sniper.",
                                Soldier.WeaponIndexes.RocketLauncher => "rocket launcher.",
                                _=> "pistol."
                            });
                            return;
                        }
                    }

                    // If already moving, no need to find a new position.
                    if (soldier.Destination != null)
                    {
                        return;
                    }

                    // If an attacker, move to a random offensive position.
                    if (soldier.Role == Soldier.SoliderRole.Attacker)
                    {
                        soldier.Move(soldier.Sense<OffensiveSensor, Vector3>());
                        soldier.Log("Moving to offensive position.");
                        return;
                    }
                    
                    // If a defender, move to a random defensive position.
                    soldier.Move(soldier.Sense<DefensiveSensor, Vector3>());
                    soldier.Log("Moving to defensive position.");
                    return;
            }
        }
    }
}