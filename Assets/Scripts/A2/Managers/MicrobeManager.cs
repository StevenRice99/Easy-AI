using System.Linq;
using A2.Agents;
using A2.Pickups;
using A2.States;
using EasyAI;
using EasyAI.Agents;
using UnityEngine;
using Random = UnityEngine.Random;

namespace A2.Managers
{
    /// <summary>
    /// Agent manager with additional fields for handling microbes for assignment two.
    /// </summary>
    public class MicrobeManager : AgentManager
    {
        /// <summary>
        /// Identifier for the type (color) of microbes.
        /// </summary>
        public enum MicrobeType
        {
            Red = 0,
            Orange,
            Yellow,
            Green,
            Blue,
            Purple,
            Pink
        }
        
        /// <summary>
        /// Used as IDs for events.
        /// </summary>
        public enum MicrobeEvents
        {
            Eaten = 0,
            Impress,
            Mate,
            Hunted
        }
        
        /// <summary>
        /// Access to the singleton directly as a microbe manager.
        /// </summary>
        public static MicrobeManager MicrobeManagerSingleton => Singleton as MicrobeManager;

        [Header("Microbe Parameters")]
        [SerializeField]
        [Tooltip("The hunger to start microbes at.")]
        private int startingHunger = -100;

        [SerializeField]
        [Tooltip("The maximum hunger before a microbe dies of starvation.")]
        private int maxHunger = 200;

        [SerializeField]
        [Min(1)]
        [Tooltip("The hunger restored from eating a microbe.")]
        private int hungerRestoredFromEating = 100;

        [SerializeField]
        [Min(0)]
        [Tooltip("The radius of the floor.")]
        private float floorRadius = 10f;

        [SerializeField]
        [Min(2)]
        [Tooltip("The minimum number of microbes there must be.")]
        private int minMicrobes = 10;

        [SerializeField]
        [Min(2)]
        [Tooltip("The maximum number of microbes there can be.")]
        public int maxMicrobes = 30;

        [SerializeField]
        [Min(0)]
        [Tooltip("The number of pickups present in the level at any time.")]
        private int activePickups = 5;

        [SerializeField]
        [Min(0)]
        [Tooltip("The chance that a new microbe could randomly spawn every tick.")]
        private float randomSpawnChance;

        [SerializeField]
        [Min(float.Epsilon)]
        [Tooltip("The slowest speed a microbe can have.")]
        private float minMicrobeSpeed = 5f;

        [SerializeField]
        [Min(float.Epsilon)]
        [Tooltip("The fastest speed a microbe can have.")]
        private float maxMicrobeSpeed = 10f;

        [SerializeField]
        [Min(float.Epsilon)]
        [Tooltip("The shortest lifespan a microbe can have.")]
        private float minMicrobeLifespan = 20f;

        [SerializeField]
        [Min(float.Epsilon)]
        [Tooltip("The longest lifespan a microbe can have.")]
        private float maxMicrobeLifespan = 30f;

        [SerializeField]
        [Min(1)]
        [Tooltip("The maximum number of offspring microbes can have when mating.")]
        private int maxOffspring = 4;

        [SerializeField]
        [Min(float.Epsilon)]
        [Tooltip("How close microbes must be to interact.")]
        private float microbeInteractRadius = 1;

        [SerializeField]
        [Min(float.Epsilon)]
        [Tooltip("How small to make newborn microbes.")]
        private float minMicrobeSize = 0.25f;

        [SerializeField]
        [Min(float.Epsilon)]
        [Tooltip("How large to make fully grown microbes.")]
        private float maxMicrobeSize = 1;

        [SerializeField]
        [Min(0)]
        [Tooltip("The minimum distance a microbe can detect up to.")]
        private float minMicrobeDetectionRange = 5;
        
        [SerializeField]
        [Min(0)]
        [Tooltip("The chance that a microbe will increase in hunger every tick.")]
        public float hungerChance = 0.05f;
        
        [Header("Prefabs")]
        [SerializeField]
        [Tooltip("Prefab for the microbes.")]
        private GameObject microbePrefab;

        [SerializeField]
        [Tooltip("Prefab for the fertility pickup.")]
        private GameObject fertilityPickupPrefab;

        [SerializeField]
        [Tooltip("Prefab for the never hungry pickup.")]
        private GameObject neverHungryPickupPrefab;

        [SerializeField]
        [Tooltip("Prefab for the offspring pickup.")]
        private GameObject offspringPickupPrefab;

        [SerializeField]
        [Tooltip("Prefab for the rejuvenate pickup.")]
        private GameObject rejuvenatePickupPrefab;

        [SerializeField]
        [Tooltip("Prefab for the spawn particles object.")]
        private GameObject spawnParticlesPrefab;

        [SerializeField]
        [Tooltip("Prefab for the death particles object.")]
        private GameObject deathParticlesPrefab;

        [SerializeField]
        [Tooltip("Prefab for the mate particles object.")]
        private GameObject mateParticlesPrefab;

        [SerializeField]
        [Tooltip("Prefab for the pickup particles object.")]
        private GameObject pickupParticlesPrefab;

        [Header("Materials")]
        [SerializeField]
        [Tooltip("Material to apply to the floor.")]
        private Material floorMaterial;

        [SerializeField]
        [Tooltip("Material to apply for red microbes.")]
        private Material redMicrobeMaterial;

        [SerializeField]
        [Tooltip("Material to apply for orange microbes.")]
        private Material orangeMicrobeMaterial;

        [SerializeField]
        [Tooltip("Material to apply for yellow microbes.")]
        private Material yellowMicrobeMaterial;

        [SerializeField]
        [Tooltip("Material to apply for green microbes.")]
        private Material greenMicrobeMaterial;

        [SerializeField]
        [Tooltip("Material to apply for blue microbes.")]
        private Material blueMicrobeMaterial;

        [SerializeField]
        [Tooltip("Material to apply for purple microbes.")]
        private Material purpleMicrobeMaterial;

        [SerializeField]
        [Tooltip("Material to apply for pink microbes.")]
        private Material pinkMicrobeMaterial;

        [SerializeField]
        [Tooltip("Material to apply to the microbe state indicator when sleeping.")]
        private Material sleepingIndicatorMaterial;

        [SerializeField]
        [Tooltip("Material to apply to the microbe state indicator when seeking food.")]
        private Material foodIndicatorMaterial;

        [SerializeField]
        [Tooltip("Material to apply to the microbe state indicator when seeking a mate.")]
        private Material mateIndicatorMaterial;

        [SerializeField]
        [Tooltip("Material to apply to the microbe state indicator when seeking a pickup.")]
        private Material pickupIndicatorMaterial;

        /// <summary>
        /// The hunger restored from eating a microbe.
        /// </summary>
        public int HungerRestoredFromEating => hungerRestoredFromEating;

        /// <summary>
        /// How close microbes must be to interact.
        /// </summary>
        public float MicrobeInteractRadius => microbeInteractRadius;

        /// <summary>
        /// How close microbes must be to interact.
        /// </summary>
        public Material RedMicrobeMaterial => redMicrobeMaterial;

        /// <summary>
        /// Material to apply for orange microbes.
        /// </summary>
        public Material OrangeMicrobeMaterial => orangeMicrobeMaterial;

        /// <summary>
        /// Material to apply for yellow microbes.
        /// </summary>
        public Material YellowMicrobeMaterial => yellowMicrobeMaterial;

        /// <summary>
        /// Material to apply for green microbes.
        /// </summary>
        public Material GreenMicrobeMaterial => greenMicrobeMaterial;

        /// <summary>
        /// Material to apply for blue microbes.
        /// </summary>
        public Material BlueMicrobeMaterial => blueMicrobeMaterial;

        /// <summary>
        /// Material to apply for purple microbes.
        /// </summary>
        public Material PurpleMicrobeMaterial => purpleMicrobeMaterial;

        /// <summary>
        /// Material to apply for pink microbes.
        /// </summary>
        public Material PinkMicrobeMaterial => pinkMicrobeMaterial;

        /// <summary>
        /// Material to apply to the microbe state indicator when sleeping.
        /// </summary>
        public Material SleepingIndicatorMaterial => sleepingIndicatorMaterial;

        /// <summary>
        /// Material to apply to the microbe state indicator when sleeping.
        /// </summary>
        public Material FoodIndicatorMaterial => foodIndicatorMaterial;

        /// <summary>
        /// Material to apply to the microbe state indicator when seeking a mate.
        /// </summary>
        public Material MateIndicatorMaterial => mateIndicatorMaterial;

        /// <summary>
        /// Material to apply to the microbe state indicator when seeking a pickup.
        /// </summary>
        public Material PickupIndicatorMaterial => pickupIndicatorMaterial;

        /// <summary>
        /// Prefab for the spawn particles object.
        /// </summary>
        public GameObject SpawnParticlesPrefab => spawnParticlesPrefab;

        /// <summary>
        /// Prefab for the death particles object.
        /// </summary>
        public GameObject DeathParticlesPrefab => deathParticlesPrefab;

        /// <summary>
        /// Prefab for the mate particles object.
        /// </summary>
        public GameObject MateParticlesPrefab => mateParticlesPrefab;

        /// <summary>
        /// Prefab for the pickup particles object.
        /// </summary>
        public GameObject PickupParticlesPrefab => pickupParticlesPrefab;

        /// <summary>
        /// The hunger to start microbes at.
        /// </summary>
        public int StartingHunger => startingHunger;

        /// <summary>
        /// The radius of the floor.
        /// </summary>
        public float FloorRadius => floorRadius;

        /// <summary>
        /// Mate two microbes.
        /// </summary>
        /// <param name="parentA">First parent.</param>
        /// <param name="parentB">Second parent.</param>
        /// <returns>The number of offspring spawned.</returns>
        public int Mate(Microbe parentA, Microbe parentB)
        {
            int born;
            
            // Spawn between the two parents.
            Vector3 position = (parentA.transform.position + parentB.transform.position) / 2;
            for (born = 0; born < maxOffspring && Agents.Count < maxMicrobes; born++)
            {
                SpawnMicrobe(
                    // Inherit the color from either parent.
                    Random.value <= 0.5f ? parentA.MicrobeType : parentB.MicrobeType,
                    position,
                    // Inherit the average speed of both parents offset by a slight random value.
                    Mathf.Clamp((parentA.moveSpeed + parentB.moveSpeed) / 2 + Random.value - 0.5f, minMicrobeSpeed, maxMicrobeSpeed),
                    // Inherit the average lifespan of both parents offset by a slight random value.
                    Mathf.Clamp((parentA.LifeSpan + parentB.LifeSpan) / 2 + Random.value - 0.5f, minMicrobeLifespan, maxMicrobeLifespan),
                    // Inherit the average detection range of both parents offset by a slight random value.
                    Mathf.Clamp((parentA.DetectionRange + parentB.DetectionRange) / 2 + Random.value - 0.5f, minMicrobeDetectionRange, floorRadius * 2));
            }

            if (born > 0)
            {
                Instantiate(MateParticlesPrefab, position, Quaternion.Euler(270, 0, 0));
            }

            return born;
        }

        /// <summary>
        /// Find the nearest microbe for a microbe to eat.
        /// </summary>
        /// <param name="seeker">The microbe looking for food.</param>
        /// <returns>The nearest microbe to eat or null if there are no microbes to eat.</returns>
        public Microbe FindFood(Microbe seeker)
        {
            Microbe[] microbes = Agents.Where(a => a is Microbe m && m != seeker && Vector3.Distance(seeker.transform.position, a.transform.position) < seeker.DetectionRange).Cast<Microbe>().ToArray();
            if (microbes.Length == 0)
            {
                return null;
            }
            
            // Microbes can eat all types of microbes that they cannot mate with. See readme for a food/mating table.
            microbes = seeker.MicrobeType switch
            {
                MicrobeType.Red => microbes.Where(m => m.MicrobeType != MicrobeType.Red && m.MicrobeType != MicrobeType.Orange && m.MicrobeType != MicrobeType.Pink).ToArray(),
                MicrobeType.Orange => microbes.Where(m => m.MicrobeType != MicrobeType.Orange && m.MicrobeType != MicrobeType.Yellow && m.MicrobeType != MicrobeType.Red).ToArray(),
                MicrobeType.Yellow => microbes.Where(m => m.MicrobeType != MicrobeType.Yellow && m.MicrobeType != MicrobeType.Green && m.MicrobeType != MicrobeType.Orange).ToArray(),
                MicrobeType.Green => microbes.Where(m => m.MicrobeType != MicrobeType.Green && m.MicrobeType != MicrobeType.Blue && m.MicrobeType != MicrobeType.Yellow).ToArray(),
                MicrobeType.Blue => microbes.Where(m => m.MicrobeType != MicrobeType.Blue && m.MicrobeType != MicrobeType.Purple && m.MicrobeType != MicrobeType.Green).ToArray(),
                MicrobeType.Purple => microbes.Where(m => m.MicrobeType != MicrobeType.Purple && m.MicrobeType != MicrobeType.Pink && m.MicrobeType != MicrobeType.Blue).ToArray(),
                _ => microbes.Where(m => m.MicrobeType is not (MicrobeType.Pink or MicrobeType.Red or MicrobeType.Purple)).ToArray()
            };

            return microbes.Length == 0 ? null : microbes.OrderBy(m => Vector3.Distance(seeker.transform.position, m.transform.position)).First();
        }
        /// <summary>
        /// Find the nearest microbe to mate with.
        /// </summary>
        /// <param name="seeker">The microbe looking for a mate.</param>
        /// <returns>The nearest microbe to mate with or null if there are no microbes to eat.</returns>
        public Microbe FindMate(Microbe seeker)
        {
            Microbe[] microbes = Agents.Where(a => a is Microbe m && m != seeker && m.IsAdult && m.State.GetType() == typeof(MicrobeSeekingMateState) && Vector3.Distance(seeker.transform.position, a.transform.position) < seeker.DetectionRange).Cast<Microbe>().ToArray();
            if (microbes.Length == 0)
            {
                return null;
            }
            
            // Microbes can mate with a type/color one up or down from theirs in additional to their own color. See readme for a food/mating table.
            microbes = seeker.MicrobeType switch
            {
                MicrobeType.Red => microbes.Where(m => m.MicrobeType is MicrobeType.Red or MicrobeType.Orange or MicrobeType.Pink).ToArray(),
                MicrobeType.Orange => microbes.Where(m => m.MicrobeType is MicrobeType.Orange or MicrobeType.Yellow or MicrobeType.Red).ToArray(),
                MicrobeType.Yellow => microbes.Where(m => m.MicrobeType is MicrobeType.Yellow or MicrobeType.Green or MicrobeType.Orange).ToArray(),
                MicrobeType.Green => microbes.Where(m => m.MicrobeType is MicrobeType.Green or MicrobeType.Blue or MicrobeType.Yellow).ToArray(),
                MicrobeType.Blue => microbes.Where(m => m.MicrobeType is MicrobeType.Blue or MicrobeType.Purple or MicrobeType.Green).ToArray(),
                MicrobeType.Purple => microbes.Where(m => m.MicrobeType is MicrobeType.Purple or MicrobeType.Pink or MicrobeType.Blue).ToArray(),
                _ => microbes.Where(m => m.MicrobeType is MicrobeType.Pink or MicrobeType.Red or MicrobeType.Purple).ToArray()
            };
            
            return microbes.Length == 0 ? null : microbes.OrderBy(m => Vector3.Distance(seeker.transform.position, m.transform.position)).First();
        }

        protected override void Start()
        {
            // Generate the floor.
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            Destroy(floor.GetComponent<Collider>());
            floor.transform.position = new(0, -1, 0);
            floor.transform.localScale = new(floorRadius * 2, 1, floorRadius * 2);
            floor.name = "Floor";
            floor.GetComponent<MeshRenderer>().material = floorMaterial;

            // Spawn initial agents.
            ResetAgents();
            
            // Spawn pickups.
            for (int i = FindObjectsOfType<MicrobeBasePickup>().Length; i < activePickups; i++)
            {
                SpawnPickup();
            }

            base.Start();
        }

        protected override void Update()
        {
            base.Update();

            // Loop through all microbes.
            for (int i = 0; i < Agents.Count; i++)
            {
                // There should never be any that are not microbes but check just in case.
                if (Agents[i] is not Microbe microbe)
                {
                    continue;
                }

                // Increment the lifespan.
                microbe.ElapsedLifespan += Time.deltaTime;

                // If a microbe has not starved, not died of old age, and has not gone out of bounds, update its size to reflect its age.
                if (microbe.Hunger <= maxHunger && microbe.ElapsedLifespan < microbe.LifeSpan && Vector3.Distance(Agents[i].transform.position, Vector3.zero) <= floorRadius)
                {
                    if (Agents[i].Visuals != null)
                    {
                        float scale = microbe.ElapsedLifespan / microbe.LifeSpan * (maxMicrobeSize - minMicrobeSize) + minMicrobeSize;
                        Agents[i].Visuals.localScale = new(scale, scale, scale);
                    }

                    continue;
                }

                // Otherwise, kill the microbe.
                microbe.Die();
                i--;
            }

            // Ensure there are enough microbes in the level.
            while (Agents.Count < minMicrobes)
            {
                SpawnMicrobe();
            }

            // Randomly spawn microbes.
            if (randomSpawnChance > 0)
            {
                for (int i = Agents.Count; i < maxMicrobes; i++)
                {
                    if (Random.value <= randomSpawnChance)
                    {
                        SpawnMicrobe();
                    }
                }
            }

            // Ensure there are enough pickups in the level.
            for (int i = FindObjectsOfType<MicrobeBasePickup>().Length; i < activePickups; i++)
            {
                SpawnPickup();
            }
        }
        
        /// <summary>
        /// Render buttons to regenerate the floor or change its size..
        /// </summary>
        /// <param name="x">X rendering position. In most cases this should remain unchanged.</param>
        /// <param name="y">Y rendering position. Update this with every component added and return it.</param>
        /// <param name="w">Width of components. In most cases this should remain unchanged.</param>
        /// <param name="h">Height of components. In most cases this should remain unchanged.</param>
        /// <param name="p">Padding of components. In most cases this should remain unchanged.</param>
        /// <returns>The updated Y position after all custom rendering has been done.</returns>
        protected override float CustomRendering(float x, float y, float w, float h, float p)
        {
            // Regenerate the floor button.
            if (GuiButton(x, y, w, h, "Reset"))
            {
                ResetAgents();
            }
            
            return NextItem(y, h, p);
        }

        /// <summary>
        /// Reset all agents.
        /// </summary>
        private void ResetAgents()
        {
            for (int i = Agents.Count - 1; i >= 0; i--)
            {
                Destroy(Agents[i].gameObject);
            }
            
            for (int i = 0; i < minMicrobes; i++)
            {
                SpawnMicrobe();
            }
        }

        /// <summary>
        /// Spawn a microbe completely randomly.
        /// </summary>
        private void SpawnMicrobe()
        {
            SpawnMicrobe((MicrobeType) Random.Range((int) MicrobeType.Red, (int) MicrobeType.Pink + 1));
        }

        /// <summary>
        /// Spawn a microbe with a given type/color but everything else random.
        /// </summary>
        /// <param name="microbeType">The type for the microbe.</param>
        private void SpawnMicrobe(MicrobeType microbeType)
        {
            Vector3 position = Random.insideUnitSphere * floorRadius;
            position = new(position.x, 0, position.z);
            
            SpawnMicrobe(microbeType, position);
        }

        /// <summary>
        /// Spawn a microbe with a given type/color at a set position but everything else random.
        /// </summary>
        /// <param name="microbeType">The type for the microbe.</param>
        /// <param name="position">The position of the microbe.</param>
        private void SpawnMicrobe(MicrobeType microbeType, Vector3 position)
        {
            SpawnMicrobe(microbeType, position, Random.Range(minMicrobeSpeed, maxMicrobeSpeed), Random.Range(minMicrobeLifespan, maxMicrobeLifespan), Random.Range(minMicrobeDetectionRange, floorRadius * 2));
        }

        /// <summary>
        /// Spawn a microbe.
        /// </summary>
        /// <param name="microbeType">The type for the microbe.</param>
        /// <param name="position">The position of the microbe.</param>
        /// <param name="moveSpeed">The speed the microbe will move at.</param>
        /// <param name="lifespan">How long the microbe can live.</param>
        /// <param name="detectionRange">How far away microbes can detect others and pickups from.</param>
        private void SpawnMicrobe(MicrobeType microbeType, Vector3 position, float moveSpeed, float lifespan, float detectionRange)
        {
            if (Agents.Count >= maxMicrobes)
            {
                return;
            }
            
            // Setup the microbe.
            GameObject go = Instantiate(microbePrefab, position, Quaternion.identity);
            Microbe microbe = go.GetComponent<Microbe>();
            if (microbe == null)
            {
                return;
            }

            microbe.MicrobeType = microbeType;
            microbe.Hunger = startingHunger;
            microbe.LifeSpan = lifespan;
            microbe.DetectionRange = detectionRange;
            microbe.moveSpeed = moveSpeed;

            // Setup the microbe name.
            string n = microbeType switch
            {
                MicrobeType.Red => "Red",
                MicrobeType.Orange => "Orange",
                MicrobeType.Yellow => "Yellow",
                MicrobeType.Green => "Green",
                MicrobeType.Blue => "Blue",
                MicrobeType.Purple => "Purple",
                _ => "Pink"
            };

            Agent[] coloredMicrobes = Agents.Where(a => a is Microbe m && m.MicrobeType == microbeType && m != microbe).ToArray();
            if (coloredMicrobes.Length == 0)
            {
                microbe.name = $"{n} 1";
            }

            for (int i = 1;; i++)
            {
                if (coloredMicrobes.Any(m => m.name == $"{n} {i}"))
                {
                    continue;
                }

                n = $"{n} {i}";
                microbe.name = n;
                break;
            }
            
            SortAgents();
            AddGlobalMessage($"Spawned microbe {n}.");
            Instantiate(SpawnParticlesPrefab, microbe.transform.position, Quaternion.Euler(270, 0, 0));
        }

        /// <summary>
        /// Spawn a pickup.
        /// </summary>
        private void SpawnPickup()
        {
            Vector3 position = Random.insideUnitSphere * floorRadius;
            position = new(position.x, 0, position.z);
            
            GameObject go = Instantiate(Random.Range(0, 4) switch
            {
                3 => fertilityPickupPrefab,
                2 => neverHungryPickupPrefab,
                1 => offspringPickupPrefab,
                _ => rejuvenatePickupPrefab
            }, position, Quaternion.identity);

            go.transform.localScale = new(0.5f, 1, 0.5f);
        }
    }
}