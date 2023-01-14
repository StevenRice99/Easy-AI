using System.Linq;
using EasyAI;
using UnityEngine;

namespace Project.States
{
    /// <summary>
    /// The global state which soldiers are always in.
    /// </summary>
    [CreateAssetMenu(menuName = "Project/States/Soldier Mind", fileName = "Soldier Mind")]
    public class SoldierMind : State
    {
        /// <summary>
        /// Control the soldier.
        /// </summary>
        /// <param name="agent"></param>
        public override void Execute(Agent agent)
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
            soldier.ChooseDestination();
        }

        /// <summary>
        /// Decide who to target.
        /// </summary>
        /// <param name="soldier">The solider.</param>
        private static void TargetNearestEnemy(Soldier soldier)
        {
            // If no enemies are detected, return null so the soldier will just look where it is walking.
            if (soldier.EnemiesDetected.Count == 0)
            {
                soldier.Target = null;
                return;
            }
            
            // For all detected enemies, prioritize who to take aim at.
            // 1. If the enemy is visible.
            // 2. If the enemy has the flag.
            // 3. How recently seen/heard the enemy was.
            // 4. How close the enemy is.
            Soldier.EnemyMemory target = soldier.EnemiesDetected.OrderBy(e => e.Visible).ThenBy(e => e.HasFlag).ThenBy(e => e.DeltaTime).ThenBy(e => Vector3.Distance(soldier.transform.position, e.Position)).First();
            
            // Define the target based upon the most ideal enemy to aim at.
            soldier.Target = new Soldier.TargetData
            {
                Enemy = target.Enemy,
                Position = target.Position,
                Visible = target.Visible
            };
        }

        /// <summary>
        /// Prioritize what weapons to use in a given situation.
        /// </summary>
        /// <param name="soldier">The soldier</param>
        private static void PrioritizeWeapons(Soldier soldier)
        {
            // If there is no target to choose a weapon based off of, predict what weapon type will be needed.
            if (soldier.Target == null)
            {
                // Defenders predict needing to use long range weapons like snipers.
                if (soldier.Role == Soldier.SoliderRole.Defender)
                {
                    soldier.Log("No targets, prioritizing sniper.");
                    soldier.SetWeaponPriority(new[]
                    {
                        (int) Soldier.WeaponChoices.Sniper,
                        (int) Soldier.WeaponChoices.RocketLauncher,
                        (int) Soldier.WeaponChoices.MachineGun,
                        (int) Soldier.WeaponChoices.Shotgun,
                        (int) Soldier.WeaponChoices.Pistol,
                    });
                    return;
                }
                
                soldier.Log("No targets, prioritizing shotgun.");

                // Attackers and the collector predict needing to use short range weapons like shotguns.
                soldier.SetWeaponPriority(new[]
                {
                    (int) Soldier.WeaponChoices.Shotgun,
                    (int) Soldier.WeaponChoices.MachineGun,
                    (int) Soldier.WeaponChoices.RocketLauncher,
                    (int) Soldier.WeaponChoices.Sniper,
                    (int) Soldier.WeaponChoices.Pistol,
                });
                return;
            }

            // Determine how far away from the target enemy the soldier is.
            float distance = soldier.DistanceTarget;
            
            // Target is far away, use long range weapons.
            if (distance >= SoldierManager.DistanceFar)
            {
                // Defenders use the sniper first.
                if (soldier.Role == Soldier.SoliderRole.Defender)
                {
                    soldier.Log("Far target, prioritizing sniper.");
                    
                    soldier.SetWeaponPriority(new[]
                    {
                        (int) Soldier.WeaponChoices.Sniper,
                        (int) Soldier.WeaponChoices.RocketLauncher,
                        (int) Soldier.WeaponChoices.MachineGun,
                        (int) Soldier.WeaponChoices.Pistol,
                        (int) Soldier.WeaponChoices.Shotgun
                    });
                    return;
                }
                
                soldier.Log("Far target, prioritizing rocket launcher.");

                // Attackers and the collector use the rocket launcher first.
                soldier.SetWeaponPriority(new[]
                {
                    (int) Soldier.WeaponChoices.RocketLauncher,
                    (int) Soldier.WeaponChoices.MachineGun,
                    (int) Soldier.WeaponChoices.Sniper,
                    (int) Soldier.WeaponChoices.Pistol,
                    (int) Soldier.WeaponChoices.Shotgun
                });
                return;
            }

            // If close range, all roles use close-range weapons first.
            if (distance <= SoldierManager.DistanceClose)
            {
                soldier.Log("Close target, prioritizing shotgun.");
                
                soldier.SetWeaponPriority(new[]
                {
                    (int) Soldier.WeaponChoices.Shotgun,
                    (int) Soldier.WeaponChoices.MachineGun,
                    (int) Soldier.WeaponChoices.Pistol,
                    (int) Soldier.WeaponChoices.RocketLauncher,
                    (int) Soldier.WeaponChoices.Sniper
                });
                return;
            }
            
            soldier.Log("Medium target, prioritizing machine gun.");
            
            // Otherwise, it is medium range, with the only difference being defenders using a sniper before a shotgun.
            if (soldier.Role == Soldier.SoliderRole.Defender)
            {
                soldier.SetWeaponPriority(new[]
                {
                    (int) Soldier.WeaponChoices.MachineGun,
                    (int) Soldier.WeaponChoices.RocketLauncher,
                    (int) Soldier.WeaponChoices.Shotgun,
                    (int) Soldier.WeaponChoices.Sniper,
                    (int) Soldier.WeaponChoices.Pistol
                });
                
                return;
            }
            
            soldier.SetWeaponPriority(new[]
            {
                (int) Soldier.WeaponChoices.MachineGun,
                (int) Soldier.WeaponChoices.RocketLauncher,
                (int) Soldier.WeaponChoices.Sniper,
                (int) Soldier.WeaponChoices.Shotgun,
                (int) Soldier.WeaponChoices.Pistol
            });
        }
    }
}