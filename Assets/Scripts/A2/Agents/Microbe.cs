using A2.Managers;
using A2.Pickups;
using A2.States;
using EasyAI.Agents;
using EasyAI.Managers;
using UnityEngine;
using Random = UnityEngine.Random;

namespace A2.Agents
{
    /// <summary>
    /// Microbe extends agent rather than being a separate component.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class Microbe : TransformAgent
    {
        [SerializeField]
        [Tooltip("The mesh renderer for the mesh that changes color depending on what state the agent is in.")]
        private MeshRenderer stateVisualization;

        [SerializeField]
        [Tooltip("Audio to play when spawning.")]
        private AudioClip spawnAudio;

        [SerializeField]
        [Tooltip("Audio to play when eating another microbe.")]
        private AudioClip eatAudio;

        [SerializeField]
        [Tooltip("Audio to play when mating.")]
        private AudioClip mateAudio;

        [SerializeField]
        [Tooltip("Audio to play when picking up a pickup.")]
        private AudioClip pickupAudio;
        
        /// <summary>
        /// The hunger of this microbe.
        /// </summary>
        public int Hunger { get; private set; }
        
        /// <summary>
        /// How long this microbe will live for in seconds.
        /// </summary>
        public float LifeSpan { get; private set; }

        /// <summary>
        /// How far away this microbe can detect other microbes and pickups.
        /// </summary>
        public float DetectionRange { get; private set; }
        
        /// <summary>
        /// How much of this microbe's life in seconds has passed.
        /// </summary>
        public float ElapsedLifespan { get; private set; }
        
        /// <summary>
        /// The microbe that this microbe is moving towards to either eat or mate with.
        /// </summary>
        public Microbe TargetMicrobe { get; private set; }
        
        /// <summary>
        /// The microbe that is hunting this microbe.
        /// </summary>
        public Microbe PursuerMicrobe { get; private set; }
        
        /// <summary>
        /// The pickup this microbe is moving towards.
        /// </summary>
        public MicrobeBasePickup TargetPickup { get; private set; }
        
        /// <summary>
        /// True if this microbe has already mated, false otherwise.
        /// </summary>
        public bool DidMate { get; private set; }

        /// <summary>
        /// The microbe is hungry when its hunger level is above zero.
        /// </summary>
        public bool IsHungry => Hunger > 0;

        /// <summary>
        /// A microbe is considered an adult if it has reached the halfway point of its life.
        /// </summary>
        public bool IsAdult => ElapsedLifespan >= LifeSpan / 2;

        /// <summary>
        /// The type (color) of this microbe.
        /// </summary>
        private MicrobeManager.MicrobeType _microbeType;

        /// <summary>
        /// The audio source to play audio from.
        /// </summary>
        private AudioSource _audioSource;

        /// <summary>
        /// The type (color) of this microbe.
        /// </summary>
        public MicrobeManager.MicrobeType MicrobeType
        {
            get => _microbeType;
            set
            {
                _microbeType = value;

                MeshRenderer meshRenderer = GetComponentInChildren<MeshRenderer>();
                if (meshRenderer == null)
                {
                    return;
                }

                meshRenderer.material = _microbeType switch
                {
                    MicrobeManager.MicrobeType.Red => MicrobeManager.RedMicrobeMaterial,
                    MicrobeManager.MicrobeType.Orange => MicrobeManager.OrangeMicrobeMaterial,
                    MicrobeManager.MicrobeType.Yellow => MicrobeManager.YellowMicrobeMaterial,
                    MicrobeManager.MicrobeType.Green => MicrobeManager.GreenMicrobeMaterial,
                    MicrobeManager.MicrobeType.Blue => MicrobeManager.BlueMicrobeMaterial,
                    MicrobeManager.MicrobeType.Purple => MicrobeManager.PurpleMicrobeMaterial,
                    _ => MicrobeManager.PinkMicrobeMaterial
                };
            }
        }

        /// <summary>
        /// Eat another microbe.
        /// </summary>
        /// <param name="eaten">The microbe to eat.</param>
        public void Eat(Agent eaten)
        {
            Hunger = Mathf.Max(MicrobeManager.StartingHunger, Hunger - MicrobeManager.HungerRestoredFromEating);
            PlayAudio(eatAudio);
            AddMessage($"Ate {eaten.name}.");
        }

        /// <summary>
        /// Die.
        /// </summary>
        public void Die()
        {
            AddMessage("Died.");
            Instantiate(MicrobeManager.DeathParticlesPrefab, transform.position, Quaternion.Euler(270, 0, 0));
            Destroy(gameObject);
        }

        /// <summary>
        /// Set the hunger of the microbe.
        /// </summary>
        /// <param name="hunger">The hunger to give the microbe.</param>
        public void SetHunger(int hunger)
        {
            Hunger = hunger;
        }

        /// <summary>
        /// Set the life span of the microbe.
        /// </summary>
        /// <param name="lifeSpan">The life span to give the microbe.</param>
        public void SetLifeSpan(float lifeSpan)
        {
            LifeSpan = lifeSpan;
        }

        /// <summary>
        /// Set the detection range of the microbe.
        /// </summary>
        /// <param name="detectionRange">The detection range to give the microbe.</param>
        public void SetDetectionRange(float detectionRange)
        {
            DetectionRange = detectionRange;
        }

        /// <summary>
        /// Set the elapsed lifespan of the microbe.
        /// </summary>
        /// <param name="elapsedTime">The elapsed lifespan to give the microbe.</param>
        public void SetElapsedLifespan(float elapsedTime)
        {
            ElapsedLifespan = elapsedTime;
        }

        /// <summary>
        /// Increase the age of the microbe.
        /// </summary>
        public void Age()
        {
            ElapsedLifespan += Time.deltaTime;
        }

        /// <summary>
        /// Set the target microbe for hunting or mating.
        /// </summary>
        /// <param name="microbe"></param>
        public void SetTargetMicrobe(Microbe microbe)
        {
            TargetMicrobe = microbe;
        }

        /// <summary>
        /// Remove the target microbe for hunting or mating.
        /// </summary>
        public void RemoveTargetMicrobe()
        {
            TargetMicrobe = null;
        }

        /// <summary>
        /// Set the pursuer microbe that is hunting this microbe.
        /// </summary>
        public void SetPursuerMicrobe(Microbe microbe)
        {
            PursuerMicrobe = microbe;
        }

        /// <summary>
        /// Set the pickup that the microbe is moving for.
        /// </summary>
        /// <param name="pickup">The pickup to assign.</param>
        public void SetTargetPickup(MicrobeBasePickup pickup)
        {
            TargetPickup = pickup;
        }
        
        /// <summary>
        /// Remove the pickup that the microbe is moving for.
        /// </summary>
        public void RemoveTargetPickup()
        {
            TargetPickup = null;
        }

        /// <summary>
        /// Set that the microbe mated.
        /// </summary>
        public void Mate()
        {
            DidMate = true;
        }

        /// <summary>
        /// Reset that the microbe has mated so it can mate again.
        /// </summary>
        public void CanMate()
        {
            DidMate = false;
        }
        
        /// <summary>
        /// Called by the AgentManager to have the agent sense, think, and act.
        /// </summary>
        public override void Perform()
        {
            // Determine if the microbe's hunger should increase.
            if (Random.value <= MicrobeManager.HungerChance * DeltaTime)
            {
                Hunger++;
            }
            
            base.Perform();
        }

        /// <summary>
        /// Set the state visuals for the microbe.
        /// </summary>
        private void SetStateVisual()
        {
            if (stateVisualization == null)
            {
                return;
            }
            
            if (State as MicrobeRoamingState)
            {
                stateVisualization.material = MicrobeManager.SleepingIndicatorMaterial;
                return;
            }
            
            if (State as MicrobeSeekingFoodState)
            {
                stateVisualization.material = MicrobeManager.FoodIndicatorMaterial;
                return;
            }
            
            if (State as MicrobeSeekingMateState)
            {
                stateVisualization.material = MicrobeManager.MateIndicatorMaterial;
                return;
            }
            
            if (State as MicrobeSeekingPickupState)
            {
                stateVisualization.material = MicrobeManager.PickupIndicatorMaterial;
            }
        }

        /// <summary>
        /// Play audio for mating.
        /// </summary>
        public void PlayMateAudio()
        {
            PlayAudio(mateAudio);
        }

        /// <summary>
        /// Play audio for picking up a pickup.
        /// </summary>
        public void PlayPickupAudio()
        {
            PlayAudio(pickupAudio);
        }
        
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
            Manager.GuiBox(x, y, w, h, p, 3);

            Manager.GuiLabel(x, y, w, h, p, $"Hunger: {Hunger} | " + (IsHungry ? "Hungry" : "Not Hungry"));
            y = Manager.NextItem(y, h, p);

            Manager.GuiLabel(x, y, w, h, p, $"Lifespan: {ElapsedLifespan} / {LifeSpan} | " + (IsAdult ? "Adult" : "Infant"));
            y = Manager.NextItem(y, h, p);
            
            Manager.GuiLabel(x, y, w, h, p, $"Mating: " + (DidMate ? "Already Mated" : IsAdult && !IsHungry ? TargetMicrobe == null ? "Searching for mate" : $"With {TargetMicrobe.name}" : "No"));
            
            return y;
        }

        protected override void Start()
        {
            base.Start();
            
            SetStateVisual();

            _audioSource = GetComponent<AudioSource>();
            
            PlayAudio(spawnAudio);

            Visuals.rotation = Quaternion.Euler(new(0, Random.Range(0f, 360f), 0));
        }

        /// <summary>
        /// Play an audio clip.
        /// </summary>
        /// <param name="clip">The audio clip to play.</param>
        private void PlayAudio(AudioClip clip)
        {
            try
            {
                _audioSource.clip = clip;
                _audioSource.Play();
            }
            catch
            {
                // Ignored.
            }
        }

        private void Update()
        {
            SetStateVisual();
        }
    }
}