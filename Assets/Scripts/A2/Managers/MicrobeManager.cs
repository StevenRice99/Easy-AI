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
        private static MicrobeManager MicrobeSingleton => Singleton as MicrobeManager;

        /// <summary>
        /// The maximum number of microbes there can be.
        /// </summary>
        public static int MaxMicrobes => MicrobeSingleton.maxMicrobes;

        /// <summary>
        /// The chance that a microbe will increase in hunger every tick.
        /// </summary>
        public static float HungerChance => MicrobeSingleton.hungerChance;

        /// <summary>
        /// The hunger restored from eating a microbe.
        /// </summary>
        public static int HungerRestoredFromEating => MicrobeSingleton.hungerRestoredFromEating;

        /// <summary>
        /// How close microbes must be to interact.
        /// </summary>
        public static float MicrobeInteractRadius => MicrobeSingleton.microbeInteractRadius;

        /// <summary>
        /// How close microbes must be to interact.
        /// </summary>
        public static Material RedMicrobeMaterial => MicrobeSingleton.redMicrobeMaterial;

        /// <summary>
        /// Material to apply for orange microbes.
        /// </summary>
        public static Material OrangeMicrobeMaterial => MicrobeSingleton.orangeMicrobeMaterial;

        /// <summary>
        /// Material to apply for yellow microbes.
        /// </summary>
        public static Material YellowMicrobeMaterial => MicrobeSingleton.yellowMicrobeMaterial;

        /// <summary>
        /// Material to apply for green microbes.
        /// </summary>
        public static Material GreenMicrobeMaterial => MicrobeSingleton.greenMicrobeMaterial;

        /// <summary>
        /// Material to apply for blue microbes.
        /// </summary>
        public static Material BlueMicrobeMaterial => MicrobeSingleton.blueMicrobeMaterial;

        /// <summary>
        /// Material to apply for purple microbes.
        /// </summary>
        public static Material PurpleMicrobeMaterial => MicrobeSingleton.purpleMicrobeMaterial;

        /// <summary>
        /// Material to apply for pink microbes.
        /// </summary>
        public static Material PinkMicrobeMaterial => MicrobeSingleton.pinkMicrobeMaterial;

        /// <summary>
        /// Material to apply to the microbe state indicator when sleeping.
        /// </summary>
        public static Material SleepingIndicatorMaterial => MicrobeSingleton.sleepingIndicatorMaterial;

        /// <summary>
        /// Material to apply to the microbe state indicator when sleeping.
        /// </summary>
        public static Material FoodIndicatorMaterial => MicrobeSingleton.foodIndicatorMaterial;

        /// <summary>
        /// Material to apply to the microbe state indicator when seeking a mate.
        /// </summary>
        public static Material MateIndicatorMaterial => MicrobeSingleton.mateIndicatorMaterial;

        /// <summary>
        /// Material to apply to the microbe state indicator when seeking a pickup.
        /// </summary>
        public static Material PickupIndicatorMaterial => MicrobeSingleton.pickupIndicatorMaterial;

        /// <summary>
        /// Prefab for the death particles object.
        /// </summary>
        public static GameObject DeathParticlesPrefab => MicrobeSingleton.deathParticlesPrefab;

        /// <summary>
        /// Prefab for the mate particles object.
        /// </summary>
        public static GameObject MateParticlesPrefab => MicrobeSingleton.mateParticlesPrefab;

        /// <summary>
        /// Prefab for the pickup particles object.
        /// </summary>
        public static GameObject PickupParticlesPrefab => MicrobeSingleton.pickupParticlesPrefab;

        /// <summary>
        /// The hunger to start microbes at.
        /// </summary>
        public static int StartingHunger => MicrobeSingleton.startingHunger;

        /// <summary>
        /// The radius of the floor.
        /// </summary>
        public static float FloorRadius => MicrobeSingleton.floorRadius;

        [Header("Microbe Parameters")]
        [Tooltip("The hunger to start microbes at.")]
        [SerializeField]
        private int startingHunger = -100;

        [Tooltip("The maximum hunger before a microbe dies of starvation.")]
        [SerializeField]
        private int maxHunger = 200;

        [Tooltip("The hunger restored from eating a microbe.")]
        [Min(1)]
        [SerializeField]
        private int hungerRestoredFromEating = 100;

        [Tooltip("The radius of the floor.")]
        [Min(0)]
        [SerializeField]
        private float floorRadius = 10f;

        [Tooltip("The minimum number of microbes there must be.")]
        [Min(2)]
        [SerializeField]
        private int minMicrobes = 10;

        [Tooltip("The maximum number of microbes there can be.")]
        [Min(2)]
        [SerializeField]
        private int maxMicrobes = 30;

        [Tooltip("The number of pickups present in the level at any time.")]
        [Min(0)]
        [SerializeField]
        private int activePickups = 5;

        [Tooltip("The chance that a new microbe could randomly spawn every tick.")]
        [Min(0)]
        [SerializeField]
        private float randomSpawnChance;

        [Tooltip("The slowest speed a microbe can have.")]
        [Min(float.Epsilon)]
        [SerializeField]
        private float minMicrobeSpeed = 5f;

        [Tooltip("The fastest speed a microbe can have.")]
        [Min(float.Epsilon)]
        [SerializeField]
        private float maxMicrobeSpeed = 10f;

        [Tooltip("The shortest lifespan a microbe can have.")]
        [Min(float.Epsilon)]
        [SerializeField]
        private float minMicrobeLifespan = 20f;

        [Tooltip("The longest lifespan a microbe can have.")]
        [Min(float.Epsilon)]
        [SerializeField]
        private float maxMicrobeLifespan = 30f;

        [Tooltip("The maximum number of offspring microbes can have when mating.")]
        [Min(1)]
        [SerializeField]
        private int maxOffspring = 4;

        [Tooltip("How close microbes must be to interact.")]
        [Min(float.Epsilon)]
        [SerializeField]
        private float microbeInteractRadius = 1;

        [Tooltip("How small to make newborn microbes.")]
        [Min(float.Epsilon)]
        [SerializeField]
        private float minMicrobeSize = 0.25f;

        [Tooltip("How large to make fully grown microbes.")]
        [Min(float.Epsilon)]
        [SerializeField]
        private float maxMicrobeSize = 1;

        [Tooltip("The minimum distance a microbe can detect up to.")]
        [Min(0)]
        [SerializeField]
        private float minMicrobeDetectionRange = 5;
        
        [Tooltip("The chance that a microbe will increase in hunger every tick.")]
        [Min(0)]
        [SerializeField]
        private float hungerChance = 0.05f;
        
        [Header("Prefabs")]
        [Tooltip("Prefab for the microbes.")]
        [SerializeField]
        private GameObject microbePrefab;

        [Tooltip("Prefab for the fertility pickup.")]
        [SerializeField]
        private GameObject fertilityPickupPrefab;

        [Tooltip("Prefab for the never hungry pickup.")]
        [SerializeField]
        private GameObject neverHungryPickupPrefab;

        [Tooltip("Prefab for the offspring pickup.")]
        [SerializeField]
        private GameObject offspringPickupPrefab;

        [Tooltip("Prefab for the rejuvenate pickup.")]
        [SerializeField]
        private GameObject rejuvenatePickupPrefab;

        [Tooltip("Prefab for the spawn particles object.")]
        [SerializeField]
        private GameObject spawnParticlesPrefab;

        [Tooltip("Prefab for the death particles object.")]
        [SerializeField]
        private GameObject deathParticlesPrefab;

        [Tooltip("Prefab for the mate particles object.")]
        [SerializeField]
        private GameObject mateParticlesPrefab;

        [Tooltip("Prefab for the pickup particles object.")]
        [SerializeField]
        private GameObject pickupParticlesPrefab;

        [Header("Materials")]
        [Tooltip("Material to apply to the floor.")]
        [SerializeField]
        private Material floorMaterial;

        [Tooltip("Material to apply for red microbes.")]
        [SerializeField]
        private Material redMicrobeMaterial;

        [Tooltip("Material to apply for orange microbes.")]
        [SerializeField]
        private Material orangeMicrobeMaterial;

        [Tooltip("Material to apply for yellow microbes.")]
        [SerializeField]
        private Material yellowMicrobeMaterial;

        [Tooltip("Material to apply for green microbes.")]
        [SerializeField]
        private Material greenMicrobeMaterial;

        [Tooltip("Material to apply for blue microbes.")]
        [SerializeField]
        private Material blueMicrobeMaterial;

        [Tooltip("Material to apply for purple microbes.")]
        [SerializeField]
        private Material purpleMicrobeMaterial;

        [Tooltip("Material to apply for pink microbes.")]
        [SerializeField]
        private Material pinkMicrobeMaterial;

        [Tooltip("Material to apply to the microbe state indicator when sleeping.")]
        [SerializeField]
        private Material sleepingIndicatorMaterial;

        [Tooltip("Material to apply to the microbe state indicator when seeking food.")]
        [SerializeField]
        private Material foodIndicatorMaterial;

        [Tooltip("Material to apply to the microbe state indicator when seeking a mate.")]
        [SerializeField]
        private Material mateIndicatorMaterial;

        [Tooltip("Material to apply to the microbe state indicator when seeking a pickup.")]
        [SerializeField]
        private Material pickupIndicatorMaterial;

        /// <summary>
        /// Mate two microbes.
        /// </summary>
        /// <param name="parentA">First parent.</param>
        /// <param name="parentB">Second parent.</param>
        /// <returns>The number of offspring spawned.</returns>
        public static int Mate(Microbe parentA, Microbe parentB)
        {
            int born;
            
            // Spawn between the two parents.
            Vector3 position = (parentA.transform.position + parentB.transform.position) / 2;
            for (born = 0; born < MicrobeSingleton.maxOffspring && MicrobeSingleton.Agents.Count < MicrobeSingleton.maxMicrobes; born++)
            {
                SpawnMicrobe(
                    // Inherit the color from either parent.
                    Random.value <= 0.5f ? parentA.MicrobeType : parentB.MicrobeType,
                    position,
                    // Inherit the average speed of both parents offset by a slight random value.
                    Mathf.Clamp((parentA.moveSpeed + parentB.moveSpeed) / 2 + Random.value - 0.5f, MicrobeSingleton.minMicrobeSpeed, MicrobeSingleton.maxMicrobeSpeed),
                    // Inherit the average lifespan of both parents offset by a slight random value.
                    Mathf.Clamp((parentA.LifeSpan + parentB.LifeSpan) / 2 + Random.value - 0.5f, MicrobeSingleton.minMicrobeLifespan, MicrobeSingleton.maxMicrobeLifespan),
                    // Inherit the average detection range of both parents offset by a slight random value.
                    Mathf.Clamp((parentA.DetectionRange + parentB.DetectionRange) / 2 + Random.value - 0.5f, MicrobeSingleton.minMicrobeDetectionRange, MicrobeSingleton.floorRadius * 2));
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
        public static Microbe FindFood(Microbe seeker)
        {
            Microbe[] microbes = MicrobeSingleton.Agents.Where(a => a is Microbe m && m != seeker && Vector3.Distance(seeker.transform.position, a.transform.position) < seeker.DetectionRange).Cast<Microbe>().ToArray();
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
        public static Microbe FindMate(Microbe seeker)
        {
            Microbe[] microbes = MicrobeSingleton.Agents.Where(a => a is Microbe m && m != seeker && m.IsAdult && m.State.GetType() == typeof(MicrobeSeekingMateState) && Vector3.Distance(seeker.transform.position, a.transform.position) < seeker.DetectionRange).Cast<Microbe>().ToArray();
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
        private static void SpawnMicrobe(MicrobeType microbeType, Vector3 position, float moveSpeed, float lifespan, float detectionRange)
        {
            if (MicrobeSingleton.Agents.Count >= MicrobeSingleton.maxMicrobes)
            {
                return;
            }
            
            // Setup the microbe.
            GameObject go = Instantiate(MicrobeSingleton.microbePrefab, position, Quaternion.identity);
            Microbe microbe = go.GetComponent<Microbe>();
            if (microbe == null)
            {
                return;
            }

            microbe.MicrobeType = microbeType;
            microbe.Hunger = MicrobeSingleton.startingHunger;
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

            Agent[] coloredMicrobes = MicrobeSingleton.Agents.Where(a => a is Microbe m && m.MicrobeType == microbeType && m != microbe).ToArray();
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
            Instantiate(MicrobeSingleton.spawnParticlesPrefab, microbe.transform.position, Quaternion.Euler(270, 0, 0));
        }

        /// <summary>
        /// Spawn a pickup.
        /// </summary>
        private static void SpawnPickup()
        {
            Vector3 position = Random.insideUnitSphere * MicrobeSingleton.floorRadius;
            position = new(position.x, 0, position.z);
            
            GameObject go = Instantiate(Random.Range(0, 4) switch
            {
                3 => MicrobeSingleton.fertilityPickupPrefab,
                2 => MicrobeSingleton.neverHungryPickupPrefab,
                1 => MicrobeSingleton.offspringPickupPrefab,
                _ => MicrobeSingleton.rejuvenatePickupPrefab
            }, position, Quaternion.identity);

            go.transform.localScale = new(0.5f, 1, 0.5f);
        }
    }
}