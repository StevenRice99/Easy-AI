using A2.Managers;
using A2.Pickups;
using A2.States;
using EasyAI;
using EasyAI.Agents;
using EasyAI.Thinking;
using UnityEngine;

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
        public int Hunger { get; set; }
        
        /// <summary>
        /// How long this microbe will live for in seconds.
        /// </summary>
        public float LifeSpan { get; set; }

        /// <summary>
        /// How far away this microbe can detect other microbes and pickups.
        /// </summary>
        public float DetectionRange { get; set; }
        
        /// <summary>
        /// How much of this microbe's life in seconds has passed.
        /// </summary>
        public float ElapsedLifespan { get; set; }
        
        /// <summary>
        /// The microbe that this microbe is moving towards to either eat or mate with.
        /// </summary>
        public Microbe TargetMicrobe { get; set; }
        
        /// <summary>
        /// The microbe that is hunting this microbe.
        /// </summary>
        public Microbe PursuerMicrobe { get; set; }
        
        /// <summary>
        /// The pickup this microbe is moving towards.
        /// </summary>
        public MicrobeBasePickup TargetPickup { get; set; }
        
        /// <summary>
        /// True if this microbe has already mated, false otherwise.
        /// </summary>
        public bool DidMate { get; set; }

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
                    MicrobeManager.MicrobeType.Red => MicrobeManager.MicrobeManagerSingleton.RedMicrobeMaterial,
                    MicrobeManager.MicrobeType.Orange => MicrobeManager.MicrobeManagerSingleton.OrangeMicrobeMaterial,
                    MicrobeManager.MicrobeType.Yellow => MicrobeManager.MicrobeManagerSingleton.YellowMicrobeMaterial,
                    MicrobeManager.MicrobeType.Green => MicrobeManager.MicrobeManagerSingleton.GreenMicrobeMaterial,
                    MicrobeManager.MicrobeType.Blue => MicrobeManager.MicrobeManagerSingleton.BlueMicrobeMaterial,
                    MicrobeManager.MicrobeType.Purple => MicrobeManager.MicrobeManagerSingleton.PurpleMicrobeMaterial,
                    _ => MicrobeManager.MicrobeManagerSingleton.PinkMicrobeMaterial
                };
            }
        }

        /// <summary>
        /// Eat another microbe.
        /// </summary>
        /// <param name="eaten">The microbe to eat.</param>
        public void Eat(Agent eaten)
        {
            Hunger = Mathf.Max(MicrobeManager.MicrobeManagerSingleton.StartingHunger, Hunger - MicrobeManager.MicrobeManagerSingleton.HungerRestoredFromEating);
            PlayAudio(eatAudio);
            AddMessage($"Ate {eaten.name}.");
        }

        /// <summary>
        /// Die.
        /// </summary>
        public void Die()
        {
            AddMessage("Died.");
            Instantiate(MicrobeManager.MicrobeManagerSingleton.DeathParticlesPrefab, transform.position, Quaternion.Euler(270, 0, 0));
            Destroy(gameObject);
        }

        /// <summary>
        /// Set the state visuals for the microbe.
        /// </summary>
        /// <param name="state">The state the microbe is entering.</param>
        public void SetStateVisual(State state)
        {
            if (stateVisualization == null)
            {
                return;
            }
            
            if (state as MicrobeWanderingState)
            {
                stateVisualization.material = MicrobeManager.MicrobeManagerSingleton.SleepingIndicatorMaterial;
                return;
            }
            
            if (state as MicrobeSeekingFoodState)
            {
                stateVisualization.material = MicrobeManager.MicrobeManagerSingleton.FoodIndicatorMaterial;
                return;
            }
            
            if (state as MicrobeSeekingMateState)
            {
                stateVisualization.material = MicrobeManager.MicrobeManagerSingleton.MateIndicatorMaterial;
                return;
            }
            
            if (state as MicrobeSeekingPickupState)
            {
                stateVisualization.material = MicrobeManager.MicrobeManagerSingleton.PickupIndicatorMaterial;
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
            y = AgentManager.NextItem(y, h, p);
            AgentManager.GuiBox(x, y, w, h, p, 3);

            AgentManager.GuiLabel(x, y, w, h, p, $"Hunger: {Hunger} | " + (IsHungry ? "Hungry" : "Not Hungry"));
            y = AgentManager.NextItem(y, h, p);

            AgentManager.GuiLabel(x, y, w, h, p, $"Lifespan: {ElapsedLifespan} / {LifeSpan} | " + (IsAdult ? "Adult" : "Infant"));
            y = AgentManager.NextItem(y, h, p);
            
            AgentManager.GuiLabel(x, y, w, h, p, $"Mating: " + (DidMate ? "Already Mated" : IsAdult && !IsHungry ? TargetMicrobe == null ? "Searching for mate" : $"With {TargetMicrobe.name}" : "No"));
            
            return y;
        }

        protected override void Start()
        {
            base.Start();
            
            SetStateVisual(State);

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
    }
}