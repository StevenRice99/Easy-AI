using System.Collections.Generic;
using System.Linq;
using EasyAI.Agents;
using EasyAI.Managers;
using Project.Agents;
using Project.Pickups;
using Project.Positions;
using UnityEngine;

namespace Project.Managers
{
    /// <summary>
    /// Manager for soldiers.
    /// </summary>
    public class SoldierManager : Manager
    {
        /// <summary>
        /// How much health each soldier has.
        /// </summary>
        public static int Health => SoldierSingleton.health;

        /// <summary>
        /// How many seconds soldiers need to wait to respawn.
        /// </summary>
        public static float Respawn => SoldierSingleton.respawn;

        /// <summary>
        /// How many seconds before a pickup can be used again.
        /// </summary>
        public static float PickupTimer => SoldierSingleton.pickupTimer;

        /// <summary>
        /// How many seconds before an old seen or hear enemy is removed from memory.
        /// </summary>
        public static float MemoryTime => SoldierSingleton.memoryTime;

        /// <summary>
        /// At what health is a soldier considered at low health.
        /// </summary>
        public static float LowHealth => SoldierSingleton.lowHealth;

        /// <summary>
        /// The maximum amount of time a soldier can wait before deciding on a new position to move to.
        /// </summary>
        public static float MaxWaitTime => SoldierSingleton.maxWaitTime;

        /// <summary>
        /// How close in units is an enemy considered close.
        /// </summary>
        public static float DistanceClose => SoldierSingleton.distanceClose;

        /// <summary>
        /// How far in units is an enemy considered far.
        /// </summary>
        public static float DistanceFar => SoldierSingleton.distanceFar;

        /// <summary>
        /// How loud the audio is.
        /// </summary>
        public static float Volume => SoldierSingleton.volume;

        /// <summary>
        /// The material to apply to the red soldiers.
        /// </summary>
        public static Material Red => SoldierSingleton.red;

        /// <summary>
        /// The material to apply to the blue soldiers.
        /// </summary>
        public static Material Blue => SoldierSingleton.blue;

        /// <summary>
        /// The flags captured by the red team.
        /// </summary>
        public static int CapturedRed => SoldierSingleton._capturedRed;

        /// <summary>
        /// The flags captured by the blue team.
        /// </summary>
        public static int CapturedBlue => SoldierSingleton._capturedBlue;

        /// <summary>
        /// The total kills by the red team.
        /// </summary>
        public static int KillsRed => SoldierSingleton._killsRed;

        /// <summary>
        /// The total kills by the blue team.
        /// </summary>
        public static int KillsBlue => SoldierSingleton._killsBlue;

        /// <summary>
        /// The spawn points for soldiers.
        /// </summary>
        public static IEnumerable<SpawnPoint> SpawnPoints => SoldierSingleton._spawnPoints;

        /// <summary>
        /// Soldiers ordered by how well they are performing.
        /// </summary>
        public static List<SoldierAgent> Sorted => SoldierSingleton._sorted;

        /// <summary>
        /// What the most flag captures by a single soldier is.
        /// </summary>
        public static int MostCaptures => SoldierSingleton._mostCaptures;

        /// <summary>
        /// What the most flag returns by a single soldier is.
        /// </summary>
        public static int MostReturns => SoldierSingleton._mostReturns;

        /// <summary>
        /// What the most kills by a single soldier is.
        /// </summary>
        public static int MostKills => SoldierSingleton._mostKills;

        /// <summary>
        /// What the least deaths by a single soldier is.
        /// </summary>
        public static int LeastDeaths => SoldierSingleton._leastDeaths;
        
        /// <summary>
        /// Cast the Manager singleton into a SoldierManager.
        /// </summary>
        private static SoldierManager SoldierSingleton => Singleton as SoldierManager;

        [Header("Soldier Parameters")]
        [Tooltip("How many soldiers to have on each team.")]
        [Range(1, 15)]
        [SerializeField]
        private int soldiersPerTeam = 3;

        [Tooltip("How much health each soldier has.")]
        [Min(1)]
        [SerializeField]
        private int health = 100;

        [Tooltip("How many seconds soldiers need to wait to respawn.")]
        [Min(0)]
        [SerializeField]
        private float respawn = 10;

        [Tooltip("How many seconds before a pickup can be used again.")]
        [Min(0)]
        [SerializeField]
        private float pickupTimer = 10;

        [Tooltip("How many seconds before an old seen or hear enemy is removed from memory.")]
        [Min(0)]
        [SerializeField]
        private float memoryTime = 5;

        [Tooltip("At what health is a soldier considered at low health.")]
        [Min(1)]
        [SerializeField]
        private int lowHealth = 50;

        [Tooltip("The maximum amount of time a soldier can wait before deciding on a new position to move to.")]
        [Min(0)]
        [SerializeField]
        private float maxWaitTime = 5;

        [Tooltip("How close in units is an enemy considered close.")]
        [Min(0)]
        [SerializeField]
        private float distanceClose = 10;

        [Tooltip("How far in units is an enemy considered far.")]
        [Min(0)]
        [SerializeField]
        private float distanceFar = 20;

        [Tooltip("How loud the audio is.")]
        [Range(0, 1)]
        [SerializeField]
        private float volume;

        [Header("Prefabs")]
        [Tooltip("The prefab for soldiers.")]
        [SerializeField]
        private GameObject soldierPrefab;

        [Header("Materials")]
        [Tooltip("The material to apply to the red soldiers.")]
        [SerializeField]
        private Material red;

        [Tooltip("The material to apply to the blue soldiers.")]
        [SerializeField]
        private Material blue;

        /// <summary>
        /// The flags captured by the red team.
        /// </summary>
        private int _capturedRed;

        /// <summary>
        /// The flags captured by the blue team.
        /// </summary>
        private int _capturedBlue;

        /// <summary>
        /// The total kills by the red team.
        /// </summary>
        private int _killsRed;

        /// <summary>
        /// The total kills by the blue team.
        /// </summary>
        private int _killsBlue;

        /// <summary>
        /// The spawn points for soldiers.
        /// </summary>
        private SpawnPoint[] _spawnPoints;

        /// <summary>
        /// All strategic positions for soldiers to use.
        /// </summary>
        private StrategicPoint[] _strategicPoints;

        /// <summary>
        /// All health and weapon pickups.
        /// </summary>
        private HealthWeaponPickup[] _healthWeaponPickups;

        /// <summary>
        /// What the most flag captures by a single soldier is.
        /// </summary>
        private int _mostCaptures;

        /// <summary>
        /// What the most flag returns by a single soldier is.
        /// </summary>
        private int _mostReturns;

        /// <summary>
        /// What the most kills by a single soldier is.
        /// </summary>
        private int _mostKills;

        /// <summary>
        /// What the least deaths by a single soldier is.
        /// </summary>
        private int _leastDeaths;

        /// <summary>
        /// Soldiers ordered by how well they are performing.
        /// </summary>
        private List<SoldierAgent> _sorted;

        /// <summary>
        /// If the cameras should lock to the best player or not.
        /// </summary>
        private bool _best = true;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="flag"></param>
        public static void CaptureFlag(FlagPickup flag)
        {
            if (flag.IsRedFlag)
            {
                SoldierSingleton._capturedBlue++;
            }
            else
            {
                SoldierSingleton._capturedRed++;
            }
            
            // Add the capture to the player.
            flag.carryingPlayer.AddMessage("Captured the flag.");
            flag.carryingPlayer.Captures++;

            // Finally return the flag and reassign roles.
            SoldierAgent soldier = flag.carryingPlayer;
            flag.ReturnFlag(null);
            soldier.AssignRoles();
            
            UpdateSorted();
        }

        /// <summary>
        /// Add a kill.
        /// </summary>
        /// <param name="shooter">The solider that got the kill.</param>
        /// <param name="killed">The soldier that got killed.</param>
        public static void AddKill(SoldierAgent shooter, SoldierAgent killed)
        {
            // Reset killed player stats.
            killed.Health = 0;
            killed.Deaths++;
            
            // Add a kill for the shooter.
            shooter.Kills++;
            
            // Add messages to each.
            killed.AddMessage($"Killed by {shooter.name}.");
            shooter.AddMessage($"Killed {killed.name}");
            
            // Add team score.
            if (shooter.RedTeam)
            {
                SoldierSingleton._killsRed++;
            }
            else
            {
                SoldierSingleton._killsBlue++;
            }

            // Reassign team roles as a team member has died.
            UpdateSorted();

            // Start the respawn counter.
            killed.StopAllCoroutines();
            killed.StartCoroutine(killed.Respawn());
        }

        /// <summary>
        /// Get a point to move to.
        /// </summary>
        /// <param name="redTeam">If this is for the red or blue team.</param>
        /// <param name="defensive">If this is for a defensive or offensive point.</param>
        /// <returns>A point to move to.</returns>
        public static Vector3 GetPoint(bool redTeam, bool defensive)
        {
            // Get all points for the team and for the given type.
            StrategicPoint[] points = SoldierSingleton._strategicPoints.Where(s => s.redTeam == redTeam && s.defensive == defensive).ToArray();
            
            // Get all open spots.
            StrategicPoint[] open = points.Where(s => s.Open).ToArray();
            
            // Move to an open spot if there is one, otherwise to a random point.
            return open.Length > 0 ? open[Random.Range(0, open.Length)].transform.position : points[Random.Range(0, points.Length)].transform.position;
        }

        /// <summary>
        /// Get a health pack to move to.
        /// </summary>
        /// <param name="soldierPosition">The position of the solder.</param>
        /// <returns>The health pack to move to or null if none are ready.</returns>
        public static Vector3? GetHealth(Vector3 soldierPosition)
        {
            // A health pickup is just a weapon pickup with an index of -1, so simply return that.
            return GetWeapon(soldierPosition, -1);
        }

        /// <summary>
        /// Get an ammo pickup to move to.
        /// </summary>
        /// <param name="soldierPosition">The position of the solder.</param>
        /// <param name="weaponIndex">The weapon type to look for.</param>
        /// <returns>The ammo pickup to move to or null if none are ready.</returns>
        public static Vector3? GetWeapon(Vector3 soldierPosition, int weaponIndex)
        {
            // Get all pickups for the given type that can be picked up.
            HealthWeaponPickup[] ready = SoldierSingleton._healthWeaponPickups.Where(p => p.weaponIndex == weaponIndex && p.Ready).ToArray();
            
            // Get the nearest one if there are any, otherwise return null.
            return ready.Length > 0 ? ready.OrderBy(p => Vector3.Distance(soldierPosition, p.transform.position)).First().transform.position : null;
        }

        /// <summary>
        /// Update all top scoring values.
        /// </summary>
        public static void UpdateSorted()
        {
            SoldierSingleton._sorted = SoldierSingleton._sorted.OrderByDescending(s => s.Captures).ThenByDescending(s => s.Kills).ThenBy(s => s.Deaths).ThenByDescending(s => s.Returns).ThenByDescending(s => s.Role == SoldierAgent.SoliderRole.Collector).ToList();
            SoldierSingleton._mostCaptures = SoldierSingleton._sorted.OrderByDescending(s => s.Captures).First().Captures;
            SoldierSingleton._mostReturns = SoldierSingleton._sorted.OrderByDescending(s => s.Returns).First().Returns;
            SoldierSingleton._mostKills = SoldierSingleton._sorted.OrderByDescending(s => s.Kills).First().Kills;
            SoldierSingleton._leastDeaths = SoldierSingleton._sorted.OrderBy(s => s.Deaths).First().Deaths;
        }

        /// <summary>
        /// Reset the level.
        /// </summary>
        private static void NewGame()
        {
            // Return the red flag.
            if (FlagPickup.RedFlag != null)
            {
                FlagPickup.RedFlag.ReturnFlag(null);
            }
            
            // Return the blue flag.
            if (FlagPickup.BlueFlag != null)
            {
                FlagPickup.BlueFlag.ReturnFlag(null);
            }

            // Enable every spawn point.
            foreach (SpawnPoint spawnPoint in SoldierSingleton._spawnPoints)
            {
                spawnPoint.Used = false;
            }
            
            // Reset every soldier.
            foreach (SoldierAgent soldier in SoldierSingleton._sorted)
            {
                soldier.Spawn();
                soldier.Kills = 0;
                soldier.Deaths = 0;
                soldier.Captures = 0;
                soldier.Returns = 0;
            }
            
            // Reset every pickup.
            foreach (HealthWeaponPickup pickup in SoldierSingleton._healthWeaponPickups)
            {
                pickup.StopAllCoroutines();
                pickup.Ready = true;
            }

            // Reset all values.
            SoldierSingleton._killsRed = 0;
            SoldierSingleton._killsBlue = 0;
            SoldierSingleton._capturedRed = 0;
            SoldierSingleton._capturedBlue = 0;
            SoldierSingleton._mostCaptures = 0;
            SoldierSingleton._mostReturns = 0;
            SoldierSingleton._mostKills = 0;
            SoldierSingleton._leastDeaths = 0;
        }
        
        protected override void Start()
        {
            // Perform base agent manager setup.
            base.Start();

            // Get all points in the level.
            _spawnPoints = FindObjectsOfType<SpawnPoint>();
            _strategicPoints = FindObjectsOfType<StrategicPoint>();
            _healthWeaponPickups = FindObjectsOfType<HealthWeaponPickup>();

            // Spawn all soldiers.
            for (int i = 0; i < soldiersPerTeam * 2; i++)
            {
                Instantiate(soldierPrefab);
            }
            
            _sorted = FindObjectsOfType<SoldierAgent>().ToList();
        }

        protected override void Update()
        {
            // Perform base agent manager updates.
            base.Update();

            // Loop through every agent.
            foreach (Agent agent in Agents)
            {
                // Only perform on alive soldiers.
                if (agent is not SoldierAgent { Alive: true } soldier)
                {
                    continue;
                }
                
                // Detect seen enemies and add them to memory.
                foreach (SoldierAgent enemy in soldier.SeeEnemies())
                {
                    // If there is no existing memory, add it to memory.
                    SoldierAgent.EnemyMemory memory = soldier.EnemiesDetected.FirstOrDefault(e => e.Enemy == enemy);
                    if (memory != null)
                    {
                        memory.DeltaTime = 0;
                        memory.Enemy = enemy;
                        memory.Position = enemy.headPosition.position;
                        memory.Visible = true;
                        memory.HasFlag = FlagPickup.RedFlag != null && FlagPickup.RedFlag.carryingPlayer == enemy || FlagPickup.BlueFlag != null && FlagPickup.BlueFlag.carryingPlayer == enemy;
                    }
                    // Otherwise, update the existing memory.
                    else
                    {
                        soldier.EnemiesDetected.Add(new()
                        {
                            DeltaTime = 0,
                            Enemy = enemy,
                            Position = enemy.headPosition.position,
                            Visible = true,
                            HasFlag = FlagPickup.RedFlag != null && FlagPickup.RedFlag.carryingPlayer == enemy || FlagPickup.BlueFlag != null && FlagPickup.BlueFlag.carryingPlayer == enemy
                        });
                    }

                    // If this enemy is not the soldier's current target, continue.
                    if (soldier.Target == null || soldier.Target.Value.Enemy != enemy)
                    {
                        continue;
                    }

                    // Update the target for the soldier.
                    soldier.Target = new SoldierAgent.TargetData
                    {
                        Enemy = enemy,
                        Position = enemy.headPosition.position,
                        Visible = true
                    };
                }
            }

            int layerMask = LayerMask.GetMask("Default", "Obstacle", "Ground", "Projectile", "HitBox");

            // Loop through all agents again.
            foreach (Agent agent in Agents)
            {
                // Only perform for alive soldiers.
                if (agent is not SoldierAgent { Alive: true } soldier)
                {
                    agent.StopLooking();
                    continue;
                }

                // If the soldier has no target, reset its look angle.
                if (soldier.Target == null)
                {
                    soldier.StopLooking();
                    soldier.headPosition.localRotation = Quaternion.identity;
                    soldier.weaponPosition.localRotation = Quaternion.identity;
                    continue;
                }

                // Otherwise, look towards the target.
                Vector3 position = soldier.Target.Value.Position;
                soldier.Look(position);
                soldier.headPosition.LookAt(position);
                soldier.headPosition.localRotation = Quaternion.Euler(soldier.headPosition.localRotation.eulerAngles.x, 0, 0);
                soldier.weaponPosition.LookAt(position);
                soldier.weaponPosition.localRotation = Quaternion.Euler(soldier.weaponPosition.localRotation.eulerAngles.x, 0, 0);

                // Continue if nothing is in line of sight of the soldier.
                if (!soldier.Weapons[soldier.WeaponIndex].CanShoot || !Physics.Raycast(soldier.shootPosition.position, soldier.shootPosition.forward, out RaycastHit hit, float.MaxValue, layerMask))
                {
                    continue;
                }

                // If something was hit, see if it was another soldier.
                Transform tr = hit.collider.transform;
                do
                {
                    SoldierAgent attacked = tr.GetComponent<SoldierAgent>();
                    if (attacked != null)
                    {
                        // If it was a soldier on the other team, shoot at them.
                        if (attacked.RedTeam != soldier.RedTeam)
                        {
                            soldier.Weapons[soldier.WeaponIndex].Shoot();
                        }

                        break;
                    }

                    tr = tr.parent;
                } while (tr != null);
            }

            if (!_best)
            {
                return;
            }
            
            // If set to follow the best soldier, set it as the selected agent.
            SoldierAgent bestAlive = _sorted.FirstOrDefault(s => s.Alive);
            SetSelectedAgent(bestAlive != null ? bestAlive : _sorted[0]);
        }
        
        /// <summary>
        /// Render buttons to reset the level or follow the best agent.
        /// </summary>
        /// <param name="x">X rendering position. In most cases this should remain unchanged.</param>
        /// <param name="y">Y rendering position. Update this with every component added and return it.</param>
        /// <param name="w">Width of components. In most cases this should remain unchanged.</param>
        /// <param name="h">Height of components. In most cases this should remain unchanged.</param>
        /// <param name="p">Padding of components. In most cases this should remain unchanged.</param>
        /// <returns>The updated Y position after all custom rendering has been done.</returns>
        protected override float CustomRendering(float x, float y, float w, float h, float p)
        {
            // Reset the game.
            if (GuiButton(x, y, w, h, "Reset"))
            {
                NewGame();
            }
            
            // Toggle between manually selecting soldiers and following the best one.
            y = NextItem(y, h, p);
            if (GuiButton(x, y, w, h, _best ? "Following Best" : "Manual Selection"))
            {
                _best = !_best;
            }
            
            return NextItem(y, h, p);
        }
    }
}