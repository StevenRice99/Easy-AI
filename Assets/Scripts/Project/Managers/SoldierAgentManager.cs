using System.Collections.Generic;
using System.Linq;
using EasyAI;
using EasyAI.Agents;
using Project.Agents;
using Project.Pickups;
using Project.Positions;
using UnityEngine;

namespace Project.Managers
{
    public class SoldierAgentManager : AgentManager
    {
        /// <summary>
        /// Getter to cast the AgentManager singleton into a SoldierAgentManager.
        /// </summary>
        public static SoldierAgentManager SoldierAgentManagerSingleton => Singleton as SoldierAgentManager;

        [SerializeField]
        [Range(1, 15)]
        [Tooltip("How many soldiers to have on each team.")]
        private int soldiersPerTeam = 1;

        [Min(1)]
        [Tooltip("How much health each soldier has.")]
        public int health = 100;

        [Min(0)]
        [Tooltip("How many seconds soldiers need to wait to respawn.")]
        public float respawn = 10;

        [Min(0)]
        [Tooltip("How many seconds before a pickup can be used again.")]
        public float pickupTimer = 10;

        [Min(0)]
        [Tooltip("How many seconds before an old seen or hear enemy is removed from memory.")]
        public float memoryTime = 5;

        [Min(1)]
        [Tooltip("At what health is a soldier considered at low health.")]
        public int lowHealth = 50;

        [Min(0)]
        [Tooltip("The maximum amount of time a soldier can wait before deciding on a new position to move to.")]
        public float maxWaitTime = 5;

        [Min(0)]
        [Tooltip("How close in units is an enemy considered close.")]
        public float distanceClose = 10;

        [Min(0)]
        [Tooltip("How far in units is an enemy considered far.")]
        public float distanceFar = 20;

        [Range(0, 1)]
        [Tooltip("How loud the audio is.")]
        public float volume;

        [SerializeField]
        [Tooltip("The prefab for soldiers.")]
        private GameObject soldierPrefab;

        [Tooltip("The material to apply to the red soldiers.")]
        public Material red;

        [Tooltip("The material to apply to the blue soldiers.")]
        public Material blue;
        
        /// <summary>
        /// The spawn points for soldiers.
        /// </summary>
        public SpawnPoint[] SpawnPoints { get; private set; }

        /// <summary>
        /// All strategic positions for soldiers to use.
        /// </summary>
        private StrategicPoint[] _strategicPoints;

        /// <summary>
        /// All health and weapon pickups.
        /// </summary>
        private HealthWeaponPickup[] _healthWeaponPickups;
        
        /// <summary>
        /// Soldiers ordered by how well they are performing.
        /// </summary>
        public List<SoldierAgent> Sorted { get; private set; }
        
        /// <summary>
        /// What the most flag captures by a single soldier is.
        /// </summary>
        public int MostCaptures { get; private set; }
        
        /// <summary>
        /// What the most flag returns by a single soldier is.
        /// </summary>
        public int MostReturns { get; private set; }
        
        /// <summary>
        /// What the most kills by a single soldier is.
        /// </summary>
        public int MostKills { get; private set; }
        
        /// <summary>
        /// What the least deaths by a single soldier is.
        /// </summary>
        public int LeastDeaths { get; private set; }
        
        /// <summary>
        /// The flags captured by the red team.
        /// </summary>
        public int ScoreRed { get; set; }
        
        /// <summary>
        /// The flags captured by the blue team.
        /// </summary>
        public int ScoreBlue { get; set; }
        
        /// <summary>
        /// The total kills by the red team.
        /// </summary>
        public int KillsRed { get; set; }
        
        /// <summary>
        /// The total kills by the blue team.
        /// </summary>
        public int KillsBlue { get; set; }

        /// <summary>
        /// If the cameras should lock to the best player or not.
        /// </summary>
        private bool _best = true;

        /// <summary>
        /// Get a point to move to.
        /// </summary>
        /// <param name="redTeam">If this is for the red or blue team.</param>
        /// <param name="defensive">If this is for a defensive or offensive point.</param>
        /// <returns>A point to move to.</returns>
        public Vector3 GetPoint(bool redTeam, bool defensive)
        {
            // Get all points for the team and for the given type.
            StrategicPoint[] points = _strategicPoints.Where(s => s.redTeam == redTeam && s.defensive == defensive).ToArray();
            
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
        public Vector3? GetHealth(Vector3 soldierPosition)
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
        public Vector3? GetWeapon(Vector3 soldierPosition, int weaponIndex)
        {
            // Get all pickups for the given type that can be picked up.
            HealthWeaponPickup[] ready = _healthWeaponPickups.Where(p => p.weaponIndex == weaponIndex && p.Ready).ToArray();
            
            // Get the nearest one if there are any, otherwise return null.
            return ready.Length > 0 ? ready.OrderBy(p => Vector3.Distance(soldierPosition, p.transform.position)).First().transform.position : null;
        }

        /// <summary>
        /// Update all top scoring values.
        /// </summary>
        public void UpdateSorted()
        {
            Sorted = Sorted.OrderByDescending(s => s.Captures).ThenByDescending(s => s.Kills).ThenBy(s => s.Deaths).ThenByDescending(s => s.Returns).ThenByDescending(s => s.Role == SoldierAgent.SoliderRole.Collector).ToList();
            MostCaptures = Sorted.OrderByDescending(s => s.Captures).First().Captures;
            MostReturns = Sorted.OrderByDescending(s => s.Returns).First().Returns;
            MostKills = Sorted.OrderByDescending(s => s.Kills).First().Kills;
            LeastDeaths = Sorted.OrderBy(s => s.Deaths).First().Deaths;
        }
        
        protected override void Start()
        {
            // Perform base agent manager setup.
            base.Start();

            // Get all points in the level.
            SpawnPoints = FindObjectsOfType<SpawnPoint>();
            _strategicPoints = FindObjectsOfType<StrategicPoint>();
            _healthWeaponPickups = FindObjectsOfType<HealthWeaponPickup>();

            // Spawn all soldiers.
            for (int i = 0; i < soldiersPerTeam * 2; i++)
            {
                Instantiate(soldierPrefab);
            }
            
            Sorted = FindObjectsOfType<SoldierAgent>().ToList();
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
                    agent.StopLookAtTarget();
                    continue;
                }

                // If the soldier has no target, reset its look angle.
                if (soldier.Target == null)
                {
                    soldier.StopLookAtTarget();
                    soldier.headPosition.localRotation = Quaternion.identity;
                    soldier.weaponPosition.localRotation = Quaternion.identity;
                    continue;
                }

                // Otherwise, look towards the target.
                Vector3 position = soldier.Target.Value.Position;
                soldier.LookAtTarget(position);
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
            SoldierAgent bestAlive = Sorted.FirstOrDefault(s => s.Alive);
            SetSelectedAgent(bestAlive != null ? bestAlive : Sorted[0]);
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
                Reset();
            }
            
            // Toggle between manually selecting soldiers and following the best one.
            y = NextItem(y, h, p);
            if (GuiButton(x, y, w, h, _best ? "Following Best" : "Manual Selection"))
            {
                _best = !_best;
            }
            
            return NextItem(y, h, p);
        }

        /// <summary>
        /// Reset the level.
        /// </summary>
        private void Reset()
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
            foreach (SpawnPoint spawnPoint in SpawnPoints)
            {
                spawnPoint.Used = false;
            }
            
            // Reset every soldier.
            foreach (SoldierAgent soldier in Sorted)
            {
                soldier.Spawn();
                soldier.Kills = 0;
                soldier.Deaths = 0;
                soldier.Captures = 0;
                soldier.Returns = 0;
            }
            
            // Reset every pickup.
            foreach (HealthWeaponPickup pickup in _healthWeaponPickups)
            {
                pickup.StopAllCoroutines();
                pickup.Ready = true;
            }

            // Reset all values.
            KillsRed = 0;
            KillsBlue = 0;
            ScoreRed = 0;
            ScoreBlue = 0;
            MostCaptures = 0;
            MostReturns = 0;
            MostKills = 0;
            LeastDeaths = 0;
        }
    }
}