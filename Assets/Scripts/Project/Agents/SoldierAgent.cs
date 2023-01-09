using System.Collections;
using System.Collections.Generic;
using System.Linq;
using EasyAI.Agents;
using EasyAI.Managers;
using Project.Managers;
using Project.Pickups;
using Project.Positions;
using Project.Weapons;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Project.Agents
{
    /// <summary>
    /// Agent used to control the soldiers in the project.
    /// </summary>
    public class SoldierAgent : CharacterAgent
    {
        /// <summary>
        /// The behaviour of soldiers is dependent upon their role on the team.
        /// Dead - Nothing as they are dead.
        /// Collector - One on each team who's main goal is to collect the enemy flag and return it.
        /// Attacker - Move between locations on the enemy's side of the map.
        /// Defender - Move between locations on their side of the map and move to return their flag if it has been taken.
        /// </summary>
        public enum SoliderRole : byte
        {
            Dead = 0,
            Collector = 1,
            Attacker = 2,
            Defender = 3
        }
        
        /// <summary>
        /// The indexes of weapons the soldier can use.
        /// </summary>
        public enum WeaponChoices
        {
            MachineGun = 0,
            Shotgun = 1,
            Sniper = 2,
            RocketLauncher = 3,
            Pistol = 4
        }

        /// <summary>
        /// The data a soldier holds.
        /// </summary>
        public class EnemyMemory
        {
            public SoldierAgent Enemy;

            public bool HasFlag;

            public bool Visible;

            public Vector3 Position;

            public float DeltaTime;
        }

        /// <summary>
        /// The data on the current target of a soldier.
        /// </summary>
        public struct TargetData
        {
            public SoldierAgent Enemy;
            
            public Vector3 Position;

            public bool Visible;
        }
        
        /// <summary>
        /// All soldiers on the red team.
        /// </summary>
        private static readonly List<SoldierAgent> TeamRed = new();
        
        /// <summary>
        /// All soldiers on the blue team.
        /// </summary>
        private static readonly List<SoldierAgent> TeamBlue = new();

        [Tooltip("The position of the solder's head.")]
        public Transform headPosition;

        [Tooltip("The position of where to cast rays and spawn projectiles from.")]
        public Transform shootPosition;

        [Tooltip("The position of where to hold the flag when carrying it.")]
        public Transform flagPosition;

        [Tooltip("The position of where weapons are held at by the soldier.")]
        public Transform weaponPosition;

        [SerializeField]
        [Tooltip("All visuals which change color based on the soldier's team.")]
        private MeshRenderer[] colorVisuals;

        [SerializeField]
        [Tooltip("All remaining visuals that do not change color based on the soldier's team.")]
        private MeshRenderer[] otherVisuals;

        /// <summary>
        /// The health of the soldier.
        /// </summary>
        public int Health { get; set; }
        
        /// <summary>
        /// The currently selected weapon of the soldier.
        /// </summary>
        public int WeaponIndex { get; set; }
        
        /// <summary>
        /// The target of the soldier.
        /// </summary>
        public TargetData? Target { get; set; }
        
        /// <summary>
        /// How many kills this soldier has.
        /// </summary>
        public int Kills { get; set; }
        
        /// <summary>
        /// How many deaths this soldier has.
        /// </summary>
        public int Deaths { get; set; }
        
        /// <summary>
        /// How many flag captures this soldier has.
        /// </summary>
        public int Captures { get; set; }
        
        /// <summary>
        /// How many flag returns this soldier has.
        /// </summary>
        public int Returns { get; set; }

        /// <summary>
        /// If this soldier is on the red team or not.
        /// </summary>
        public bool RedTeam { get; private set; }
        
        /// <summary>
        /// The weapons of this soldier.
        /// </summary>
        public Weapon[] Weapons { get; private set; }

        /// <summary>
        /// If the soldier is alive or not.
        /// </summary>
        public bool Alive => Role != SoliderRole.Dead;
        
        /// <summary>
        /// The soldier's current role on the team.
        /// </summary>
        public SoliderRole Role { get; private set; }

        /// <summary>
        /// The colliders that are attached to this soldier.
        /// </summary>
        public Collider[] Colliders { get; private set; }

        /// <summary>
        /// If the soldier should find a new location to move to.
        /// </summary>
        private bool _findNewPoint = true;

        /// <summary>
        /// The coroutine to keep track of when timing if a new point should be searched for.
        /// </summary>
        private Coroutine _pointDelay;

        /// <summary>
        /// The enemies which this soldier currently has detected.
        /// </summary>
        public readonly List<EnemyMemory> EnemiesDetected = new();

        /// <summary>
        /// Which weapons the soldier has a preference to currently use.
        /// </summary>
        private int[] _weaponPriority = new int[(int) WeaponChoices.Pistol];

        /// <summary>
        /// If this soldier is carrying the flag.
        /// </summary>
        private bool CarryingFlag => RedTeam ? FlagPickup.BlueFlag != null && FlagPickup.BlueFlag.carryingPlayer == this : FlagPickup.RedFlag != null && FlagPickup.RedFlag.carryingPlayer == this;

        /// <summary>
        /// If this soldier's flag is at its base.
        /// </summary>
        private bool FlagAtBase => RedTeam ? FlagPickup.RedFlag != null && FlagPickup.RedFlag.transform.position == FlagPickup.RedFlag.SpawnPosition : FlagPickup.BlueFlag != null && FlagPickup.BlueFlag.transform.position == FlagPickup.BlueFlag.SpawnPosition;

        /// <summary>
        /// The location of the enemy flag.
        /// </summary>
        private Vector3 EnemyFlag => RedTeam ? FlagPickup.BlueFlag != null ? FlagPickup.BlueFlag.transform.position : Vector3.zero : FlagPickup.RedFlag != null ? FlagPickup.RedFlag.transform.position : Vector3.zero;

        /// <summary>
        /// The location of the team's flag.
        /// </summary>
        private Vector3 TeamFlag => RedTeam ? FlagPickup.RedFlag != null ? FlagPickup.RedFlag.transform.position : Vector3.zero : FlagPickup.BlueFlag != null ? FlagPickup.BlueFlag.transform.position : Vector3.zero;
        
        /// <summary>
        /// The location of this soldier's base.
        /// </summary>
        private Vector3 Base => RedTeam ? FlagPickup.RedFlag != null ? FlagPickup.RedFlag.SpawnPosition : Vector3.zero : FlagPickup.BlueFlag != null ? FlagPickup.BlueFlag.SpawnPosition : Vector3.zero;
        
        /// <summary>
        /// Override for custom detail rendering on the automatic GUI.
        /// </summary>
        /// <param name="x">X rendering position. In most cases this should remain unchanged.</param>
        /// <param name="y">Y rendering position. Update this with every component added and return it.</param>
        /// <param name="w">Width of components. In most cases this should remain unchanged.</param>
        /// <param name="h">Height of components. In most cases this should remain unchanged.</param>
        /// <param name="p">Padding of components. In most cases this should remain unchanged.</param>
        /// <returns>The updated Y position after all custom rendering has been done.</returns>
        public override float DisplayDetails(float x, float y, float w, float h, float p)
        {
            y = Manager.NextItem(y, h, p);
            Manager.GuiBox(x, y, w, h, p, 13);
            
            // Display overall flags captured for each team.
            Manager.GuiLabel(x, y, w, h, p, $"Team Captures - Red: {SoldierManager.CapturedRed} | Blue: {SoldierManager.CapturedBlue}");
            y = Manager.NextItem(y, h, p);
            
            // Display overall kills for each team.
            Manager.GuiLabel(x, y, w, h, p, $"Team Kills - Red: {SoldierManager.KillsRed} | Blue: {SoldierManager.KillsBlue}");
            y = Manager.NextItem(y, h, p);
            
            Manager.GuiLabel(x, y, w, h, p, "--------------------------------------------------------------------------------------------------------------------------");
            y = Manager.NextItem(y, h, p);

            // Display the position of this soldier relative to all others.
            Manager.GuiLabel(x, y, w, h, p, $"Soldier Performance: {SoldierManager.Sorted.IndexOf(this) + 1} / {SoldierManager.Sorted.Count}");
            y = Manager.NextItem(y, h, p);

            // Display the role of this soldier.
            Manager.GuiLabel(x, y, w, h, p, Role == SoliderRole.Dead ? "Respawning" : $"Role: {Role}");
            y = Manager.NextItem(y, h, p);

            // Display the health of this soldier.
            Manager.GuiLabel(x, y, w, h, p, $"Health: {Health} / {SoldierManager.Health}");
            y = Manager.NextItem(y, h, p);

            // Display the weapon this soldier is using.
            Manager.GuiLabel(x, y, w, h, p, Role == SoliderRole.Dead ? "Weapon: None" : WeaponIndex switch
            {
                (int) WeaponChoices.MachineGun => $"Weapon: Machine Gun | Ammo: {Weapons[WeaponIndex].Ammo} / {Weapons[WeaponIndex].maxAmmo}",
                (int) WeaponChoices.Shotgun => $"Weapon: Shotgun | Ammo: {Weapons[WeaponIndex].Ammo} / {Weapons[WeaponIndex].maxAmmo}",
                (int) WeaponChoices.Sniper => $"Weapon: Sniper | Ammo: {Weapons[WeaponIndex].Ammo} / {Weapons[WeaponIndex].maxAmmo}",
                (int) WeaponChoices.RocketLauncher => $"Weapon: Rocket Launcher | Ammo: {Weapons[WeaponIndex].Ammo} / {Weapons[WeaponIndex].maxAmmo}",
                _ => "Weapon: Pistol"
            });
            y = Manager.NextItem(y, h, p);
            
            // Display the enemy this soldier is fighting.
            Manager.GuiLabel(x, y, w, h, p, Target == null || Target.Value.Enemy == null ? "Fighting: Nobody" : $"Fighting: {Target.Value.Enemy.name}");
            y = Manager.NextItem(y, h, p);

            // Display all enemies this soldier has detected.
            int visible = EnemiesDetected.Count(e => e.Visible);
            Manager.GuiLabel(x, y, w, h, p, $"See: {visible} | Hear: {EnemiesDetected.Count - visible}");
            y = Manager.NextItem(y, h, p);

            // Display how many flag captures this soldier has.
            Manager.GuiLabel(x, y, w, h, p, $"Captures: {Captures} | Most: {SoldierManager.MostCaptures}");
            y = Manager.NextItem(y, h, p);

            // Display how many flag returns this soldier has.
            Manager.GuiLabel(x, y, w, h, p, $"Returns: {Returns} | Most: {SoldierManager.MostReturns}");
            y = Manager.NextItem(y, h, p);

            // Display how many kills this soldier has.
            Manager.GuiLabel(x, y, w, h, p, $"Kills: {Kills} | Most: {SoldierManager.MostKills}");
            y = Manager.NextItem(y, h, p);
            
            // Display how many deaths this soldier has.
            Manager.GuiLabel(x, y, w, h, p, $"Deaths: {Deaths} | Least: {SoldierManager.LeastDeaths}");
            
            return y;
        }
        
        /// <summary>
        /// Override to have the soldier perform its actions.
        /// </summary>
        public override void Perform()
        {
            // Do nothing when dead.
            if (Role == SoliderRole.Dead)
            {
                return;
            }
            
            // Choose a target.
            Target = ChooseTarget();

            // Choose the optimal weapon to use.
            PrioritizeWeapons();
            ChooseWeapon();

            // Chose to move somewhere.
            ChooseDestination();
            
            // Remove detected enemies that have exceeded their maximum memory time.
            Cleanup();
            
            base.Perform();
        }
        
        /// <summary>
        /// Character controller movement.
        /// </summary>
        public override void MovementCalculations()
        {
            // Only move when the controller is enabled to avoid throwing an error as it needs to be disabled when dead.
            if (CharacterController != null && CharacterController.enabled)
            {
                base.MovementCalculations();
            }
        }
        
        /// <summary>
        /// Receive damage from another soldier.
        /// </summary>
        /// <param name="amount">How much damage was taken.</param>
        /// <param name="shooter">What soldier shot.</param>
        public void Damage(int amount, SoldierAgent shooter)
        {
            // If already dead, do nothing.
            if (Role == SoliderRole.Dead)
            {
                return;
            }
            
            // Reduce health.
            Health -= amount;
            
            // Nothing more to do if still alive.
            if (Health <= 0)
            {
                SoldierManager.AddKill(shooter, this);
            }
        }

        /// <summary>
        /// Add an enemy to memory that was heard.
        /// </summary>
        /// <param name="enemy">The enemy which was heard.</param>
        /// <param name="distance">How far away before the sound is considered out of range.</param>
        public void Hear(SoldierAgent enemy, float distance)
        {
            // Do not "hear" an enemy if the shot was out of range.
            if (Vector3.Distance(headPosition.position, enemy.headPosition.position) > distance)
            {
                return;
            }
            
            // See if this item already exists in memory and if it does, simply update values.
            EnemyMemory memory = EnemiesDetected.FirstOrDefault(e => e.Enemy == enemy && !e.Visible);
            if (memory != null)
            {
                memory.DeltaTime = 0;
                memory.Position = enemy.headPosition.position;
                memory.Visible = false;
                memory.HasFlag = false;
                return;
            }
            
            // Otherwise add the instance into memory.
            EnemiesDetected.Add(new()
            {
                DeltaTime = 0,
                Enemy = enemy,
                Position = enemy.headPosition.position,
                Visible = false,
                HasFlag = false
            });
        }

        /// <summary>
        /// Heal this soldier.
        /// </summary>
        public void Heal()
        {
            // Cannot heal if dead.
            if (Role == SoliderRole.Dead)
            {
                return;
            }

            Health = SoldierManager.Health;
        }

        /// <summary>
        /// Get all enemies.
        /// </summary>
        /// <returns>An enumerable of all enemies.</returns>
        public IEnumerable<SoldierAgent> GetEnemies()
        {
            return (RedTeam ? TeamBlue : TeamRed).Where(s => s.Alive);
        }

        /// <summary>
        /// Assign roles to the team.
        /// </summary>
        public void AssignRoles()
        {
            // Get the soldiers on this team, ordered by how close they are to the enemy flag.
            SoldierAgent[] team = GetTeam();
            
            // Loop through every team member.
            for (int i = 0; i < team.Length; i++)
            {
                // Clear any current movement data.
                team[i].StopNavigating();
                team[i]._findNewPoint = true;
                
                // The closest soldier to the enemy flag becomes the collector.
                if (i == 0)
                {
                    team[i].Role = SoliderRole.Collector;
                }
                // The nearest half become attackers.
                else if (i <= team.Length / 2)
                {
                    team[i].Role = SoliderRole.Attacker;
                }
                // The furthest become defenders.
                else
                {
                    team[i].Role = SoliderRole.Defender;
                }
            }
        }

        /// <summary>
        /// Spawn the soldier in.
        /// </summary>
        public void Spawn()
        {
            // Get spawn points on their side of the map.
            SpawnPoint[] points = SoldierManager.SpawnPoints.Where(p => p.redTeam == RedTeam).ToArray();
            
            // Get all open spawn points.
            SpawnPoint[] open = points.Where(p => p.Open).ToArray();
            
            // If there are open spawn points, spawn at one of them, otherwise, default to any spawn point.
            SpawnPoint spawn = open.Length > 0 ? open[Random.Range(0, open.Length)] : points[Random.Range(0, points.Length)];

            // Since there is a character controller attached, it needs to be disabled to move the soldier to the spawn.
            CharacterController.enabled = false;

            // Move to the spawn point.
            Transform spawnTr = spawn.transform;
            transform.position = spawnTr.position;
            Visuals.rotation = spawnTr.rotation;
            
            // Set that the spawn point has been used so other soldiers avoid using it.
            spawn.Use();
            
            // Reenable the character controller.
            // ReSharper disable once Unity.InefficientPropertyAccess
            CharacterController.enabled = true;
            
            // Set a dummy role to indicate the soldier is no longer dead.
            Role = SoliderRole.Collector;
            
            // Get new roles, heal, start with the machine gun, and reset to find a new point.
            AssignRoles();
            Heal();
            SelectWeapon(0);
            ToggleAlive();
            _findNewPoint = true;

            foreach (Weapon weapon in Weapons)
            {
                weapon.Replenish();
            }
            
            SoldierManager.UpdateSorted();
        }

        /// <summary>
        /// Detect which enemies are visible.
        /// </summary>
        /// <returns>All enemies in line of sight.</returns>
        public IEnumerable<SoldierAgent> SeeEnemies()
        {
            return GetEnemies().Where(enemy => !Physics.Linecast(headPosition.position, enemy.headPosition.position, Manager.ObstacleLayers)).ToArray();
        }

        protected override void Start()
        {
            // Perform default setup.
            base.Start();

            // Setup all weapons.
            Weapons = GetComponentsInChildren<Weapon>();
            for (int i = 0; i < Weapons.Length; i++)
            {
                Weapons[i].Soldier = this;
                Weapons[i].Index = i;
            }

            // Assign team.
            RedTeam = TeamRed.Count <= TeamBlue.Count;
            if (RedTeam)
            {
                TeamRed.Add(this);
            }
            else
            {
                TeamBlue.Add(this);
            }

            // Assign name.
            name = (RedTeam ? "Red " : "Blue ") + (RedTeam ? TeamRed.Count : TeamBlue.Count);

            // Get all attached colliders.
            List<Collider> colliders = GetComponents<Collider>().ToList();
            colliders.AddRange(GetComponentsInChildren<Collider>());
            Colliders = colliders.Distinct().ToArray();

            // Assign team colors.
            foreach (MeshRenderer meshRenderer in colorVisuals)
            {
                meshRenderer.material = RedTeam ? SoldierManager.Red : SoldierManager.Blue;
            }
            
            // Spawn in.
            Spawn();
        }

        /// <summary>
        /// Choose where to move to.
        /// </summary>
        private void ChooseDestination()
        {
            // If carrying the flag, attempt to move directly back to base.
            if (CarryingFlag)
            {
                if (Navigate(Base))
                {
                    AddMessage("Have the flag, returning it to base.");
                }

                return;
            }

            switch (Role)
            {
                // If the flag collector, move to collect the enemy flag.
                case SoliderRole.Collector:
                    if (Navigate(EnemyFlag))
                    {
                        AddMessage("Moving to collect enemy flag.");
                    }

                    return;
                
                // If a defender and the flag has been taken, move to it to kill the enemy flag carried and return it.
                case SoliderRole.Defender when !FlagAtBase:
                    if (Navigate(TeamFlag))
                    {
                        AddMessage("Moving to return flag.");
                    }
                    
                    _findNewPoint = true;
                    return;
                
                default:
                    // If the soldier has low health, move to a health pack to heal.
                    if (Health <= SoldierManager.LowHealth)
                    {
                        Vector3? destination = SoldierManager.GetHealth(transform.position);
                        if (destination != null)
                        {
                            if (Navigate(destination.Value))
                            {
                                AddMessage("Moving to pickup health.");
                            }
                            
                            _findNewPoint = true;
                            return;
                        }
                    }

                    // Decisions when the soldier's current target enemy is not visible.
                    if (Target is not { Visible: true })
                    {
                        // If not at full health, move to a health pack to heal.
                        if (Health < SoldierManager.Health)
                        {
                            Vector3? destination = SoldierManager.GetHealth(transform.position);
                            if (destination != null)
                            {
                                if (Navigate(destination.Value))
                                {
                                    AddMessage("Moving to pickup health.");
                                }
                                
                                _findNewPoint = true;
                                return;
                            }
                        }
                        
                        // In order of the most prioritized weapons of the soldier, if a weapon needs more ammo, move to pickup ammo.
                        foreach (int w in _weaponPriority)
                        {
                            if (Weapons[w].maxAmmo < 0 || Weapons[w].Ammo >= Weapons[w].maxAmmo)
                            {
                                continue;
                            }
                            
                            Vector3? destination = SoldierManager.GetWeapon(transform.position, w);
                            if (destination == null)
                            {
                                continue;
                            }

                            if (Navigate(destination.Value))
                            {
                                AddMessage("Moving to pickup ammo for " + (WeaponChoices) w switch
                                {
                                    WeaponChoices.MachineGun => "machine gun.",
                                    WeaponChoices.Shotgun => "shotgun.",
                                    WeaponChoices.Sniper => "sniper.",
                                    WeaponChoices.RocketLauncher => "rocket launcher.",
                                    _=> "pistol."
                                });
                            }
                            
                            _findNewPoint = true;
                            return;
                        }
                    }

                    // If already moving to a position, do not search for a new one.
                    if (Destination != null)
                    {
                        return;
                    }

                    // Find a point to move to, either in the offense or defense side depending on the soldier's role.
                    if (_findNewPoint || (Role == SoliderRole.Attacker && Target is { Visible: true }))
                    {
                        _findNewPoint = false;
                        if (Navigate(SoldierManager.GetPoint(RedTeam, Role == SoliderRole.Defender)))
                        {
                            AddMessage(Role == SoliderRole.Attacker ? "Moving to offensive position." : "Moving to defensive position.");
                        }
                        return;
                    }

                    // Do not search for a new point for a given amount of time upon reaching it.
                    _pointDelay ??= StartCoroutine(PointDelay());
                    return;
            }
        }

        /// <summary>
        /// Prioritize what weapons to use in a given situation.
        /// </summary>
        private void PrioritizeWeapons()
        {
            // If there is no target to choose a weapon based off of, predict what weapon type will be needed.
            if (Target == null)
            {
                // Defenders predict needing to use long range weapons like snipers.
                if (Role == SoliderRole.Defender)
                {
                    AddMessage("No targets, prioritizing sniper.");
                    
                    _weaponPriority = new[]
                    {
                        (int) WeaponChoices.Sniper,
                        (int) WeaponChoices.RocketLauncher,
                        (int) WeaponChoices.MachineGun,
                        (int) WeaponChoices.Shotgun,
                        (int) WeaponChoices.Pistol,
                    };
                    
                    return;
                }
                
                AddMessage("No targets, prioritizing shotgun.");

                // Attackers and the collector predict needing to use short range weapons like shotguns.
                _weaponPriority = new[]
                {
                    (int) WeaponChoices.Shotgun,
                    (int) WeaponChoices.MachineGun,
                    (int) WeaponChoices.RocketLauncher,
                    (int) WeaponChoices.Sniper,
                    (int) WeaponChoices.Pistol,
                };
                return;
            }

            // Determine how far away from the target enemy the soldier is.
            float distance = Vector3.Distance(shootPosition.position, Target.Value.Position);
            
            // Target is far away, use long range weapons.
            if (distance >= SoldierManager.DistanceFar)
            {
                // Defenders use the sniper first.
                if (Role == SoliderRole.Defender)
                {
                    AddMessage("Far target, prioritizing sniper.");
                    
                    _weaponPriority = new[]
                    {
                        (int) WeaponChoices.Sniper,
                        (int) WeaponChoices.RocketLauncher,
                        (int) WeaponChoices.MachineGun,
                        (int) WeaponChoices.Pistol,
                        (int) WeaponChoices.Shotgun
                    };
                
                    return;
                }
                
                AddMessage("Far target, prioritizing rocket launcher.");

                // Attackers and the collector use the rocket launcher first.
                _weaponPriority = new[]
                {
                    (int) WeaponChoices.RocketLauncher,
                    (int) WeaponChoices.MachineGun,
                    (int) WeaponChoices.Sniper,
                    (int) WeaponChoices.Pistol,
                    (int) WeaponChoices.Shotgun
                };

                return;
            }

            // If close range, all roles use close-range weapons first.
            if (distance <= SoldierManager.DistanceClose)
            {
                AddMessage("Close target, prioritizing shotgun.");
                
                _weaponPriority = new[]
                {
                    (int) WeaponChoices.Shotgun,
                    (int) WeaponChoices.MachineGun,
                    (int) WeaponChoices.Pistol,
                    (int) WeaponChoices.RocketLauncher,
                    (int) WeaponChoices.Sniper
                };
                
                return;
            }
            
            AddMessage("Medium target, prioritizing machine gun.");
            
            // Otherwise, it is medium range, with the only difference being defenders using a sniper before a shotgun.
            if (Role == SoliderRole.Defender)
            {
                _weaponPriority = new[]
                {
                    (int) WeaponChoices.MachineGun,
                    (int) WeaponChoices.RocketLauncher,
                    (int) WeaponChoices.Shotgun,
                    (int) WeaponChoices.Sniper,
                    (int) WeaponChoices.Pistol
                };
                
                return;
            }

            _weaponPriority = new[]
            {
                (int) WeaponChoices.MachineGun,
                (int) WeaponChoices.RocketLauncher,
                (int) WeaponChoices.Sniper,
                (int) WeaponChoices.Shotgun,
                (int) WeaponChoices.Pistol
            };
        }

        /// <summary>
        /// Choose the weapon to use.
        /// </summary>
        private void ChooseWeapon()
        {
            // Go through the weapon priority and select the first weapon which has ammo.
            foreach (int w in _weaponPriority)
            {
                if (Weapons[w].Ammo <= 0 && Weapons[w].maxAmmo >= 0)
                {
                    continue;
                }

                SelectWeapon(w);
                return;
            }
        }

        /// <summary>
        /// Respawn the soldier after being killed.
        /// </summary>
        /// <returns>Nothing.</returns>
        public IEnumerator Respawn()
        {
            // Set that the soldier has died.
            Role = SoliderRole.Dead;
            ToggleAlive();
            
            // Reassign team roles.
            AssignRoles();
            
            // Clear data the soldier had.
            EnemiesDetected.Clear();
            Target = null;
            StopNavigating();
            StopLooking();
            MoveVelocity = Vector2.zero;
            
            // Wait to spawn.
            yield return new WaitForSeconds(SoldierManager.Respawn);
            
            // Spawn the soldier.
            Spawn();
        }

        /// <summary>
        /// Get all members of this soldier's team.
        /// </summary>
        /// <returns>All soldiers on this solder's team by closest to the enemy flag.</returns>
        private SoldierAgent[] GetTeam()
        {
            IEnumerable<SoldierAgent> team = (RedTeam ? TeamRed : TeamBlue).Where(s => s.Alive);
            if (RedTeam)
            {
                if (FlagPickup.BlueFlag != null)
                {
                    team = team.OrderBy(s => Vector3.Distance(s.transform.position, FlagPickup.BlueFlag.transform.position));
                }
            }
            else
            {
                if (FlagPickup.RedFlag != null)
                {
                    team = team.OrderBy(s => Vector3.Distance(s.transform.position, FlagPickup.RedFlag.transform.position));
                }
            }

            return team.ToArray();
        }

        /// <summary>
        /// Toggle all meshes, colliders, and weapons based on if the soldier is alive.
        /// </summary>
        private void ToggleAlive()
        {
            foreach (MeshRenderer meshRenderer in colorVisuals)
            {
                meshRenderer.enabled = Alive;
            }
            
            foreach (MeshRenderer meshRenderer in otherVisuals)
            {
                meshRenderer.enabled = Alive;
            }

            foreach (Collider col in Colliders)
            {
                col.enabled = Alive;
            }

            WeaponVisible();
        }

        /// <summary>
        /// Select a given weapon.
        /// </summary>
        /// <param name="i">The weapon index selected.</param>
        private void SelectWeapon(int i)
        {
            int lastWeapon = WeaponIndex;
            
            // Set the new selected weapon.
            WeaponIndex = Mathf.Clamp(i, 0, Weapons.Length - 1);

            if (lastWeapon != WeaponIndex)
            {
                AddMessage((WeaponChoices) WeaponIndex switch
                {
                    WeaponChoices.MachineGun => "Selecting machine gun.",
                    WeaponChoices.Shotgun => "Selecting shotgun.",
                    WeaponChoices.Sniper => "Selecting sniper.",
                    WeaponChoices.RocketLauncher => "Selecting rocket launcher.",
                    _=> "Selecting pistol."
                });
            }
            
            // Limit agent rotation speed based on their weapon.
            lookSpeed = Weapons[WeaponIndex].rotationSpeed;
            
            // Ensure weapons are properly visible.
            WeaponVisible();
        }

        /// <summary>
        /// Ensure only the selected weapon is visible.
        /// </summary>
        private void WeaponVisible()
        {
            for (int i = 0; i < Weapons.Length; i++)
            {
                // Only the selected weapon is visible, and none are visible if the soldier is dead.
                Weapons[i].Visible(Alive && i == WeaponIndex);
            }
        }

        /// <summary>
        /// Remove all detected enemies..
        /// </summary>
        private void Cleanup()
        {
            // Loop through all detected enemies.
            for (int i = 0; i < EnemiesDetected.Count; i++)
            {
                // Increment how long the enemy has been in memory.
                EnemiesDetected[i].DeltaTime += DeltaTime;
                
                // If the detected enemy is too old or they have died, remove it.
                if (EnemiesDetected[i].DeltaTime > SoldierManager.MemoryTime || EnemiesDetected[i].Enemy.Role == SoliderRole.Dead)
                {
                    EnemiesDetected.RemoveAt(i--);
                }
            }
        }

        /// <summary>
        /// Choose the target for this soldier.
        /// </summary>
        /// <returns>The target or null if there is no target.</returns>
        private TargetData? ChooseTarget()
        {
            // If no enemies are detected, return null so the soldier will just look where it is walking.
            if (EnemiesDetected.Count == 0)
            {
                return null;
            }
            
            // For all detected enemies, prioritize who to take aim at.
            // 1. If the enemy is visible.
            // 2. If the enemy has the flag.
            // 3. How recently seen/heard the enemy was.
            // 4. How close the enemy is.
            EnemyMemory target = EnemiesDetected.OrderBy(e => e.Visible).ThenBy(e => e.HasFlag).ThenBy(e => e.DeltaTime).ThenBy(e => Vector3.Distance(transform.position, e.Position)).First();
            
            // Define the target based upon the most ideal enemy to aim at.
            return new TargetData
            {
                Enemy = target.Enemy,
                Position = target.Position,
                Visible = target.Visible
            };
        }

        /// <summary>
        /// Delay for a random amount of time how long to wait before choosing a new position to move to.
        /// </summary>
        /// <returns>Nothing.</returns>
        private IEnumerator PointDelay()
        {
            yield return new WaitForSeconds(Random.Range(0, SoldierManager.MaxWaitTime));
            _findNewPoint = true;
            _pointDelay = null;
        }
    }
}