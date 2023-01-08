using System.Collections.Generic;
using System.Linq;
using EasyAI;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

namespace A1.Managers
{
    /// <summary>
    /// Extension of AgentManager to handle floor tile generation.
    /// </summary>
    public class CleanerAgentManager : AgentManager
    {
        /// <summary>
        /// Getter to cast the AgentManager singleton into a FloorManager.
        /// </summary>
        public static CleanerAgentManager CleanerAgentManagerSingleton => Singleton as CleanerAgentManager;

        [Header("Cleaner Parameters")]
        [SerializeField]
        [Tooltip("How many floor sections will be generated.")]
        private Vector2 floorSize = new(3, 1);

        [SerializeField]
        [Min(1)]
        [Tooltip("How many units wide will each floor section be generated as.")]
        private int floorScale = 1;

        [SerializeField]
        [Range(0, 1)]
        [Tooltip("The percentage chance that any floor section during generation will be likely to get dirty meaning the odds in increases in dirt level every time are double that of other floor sections.")]
        private float likelyToGetDirtyChance;

        [SerializeField]
        [Min(0)]
        [Tooltip("How many seconds between every time dirt is randomly added to the floor.")]
        private float timeBetweenDirtGeneration = 5;

        [SerializeField]
        [Range(0, 1)]
        [Tooltip("The percentage chance that a floor section will increase in dirt level during dirt generation.")]
        private float chanceDirty;
        
        [Header("Prefabs")]
        [SerializeField]
        [Tooltip("The prefab for the cleaning agent that will be spawned in.")]
        private GameObject cleanerAgentPrefab;

        [Header("Floor Materials")]
        [SerializeField]
        [Tooltip("The material applied to normal floor sections when they are clean.")]
        private Material materialCleanNormal;

        [SerializeField]
        [Tooltip("The material applied to like to get dirty floor sections when they are clean.")]
        private Material materialCleanLikelyToGetDirty;

        [SerializeField]
        [Tooltip("The material applied to a floor section when it is dirty.")]
        private Material materialDirty;

        [SerializeField]
        [Tooltip("The material applied to a floor section when it is very dirty.")]
        private Material materialVeryDirty;

        [SerializeField]
        [Tooltip("The material applied to a floor section when it is extremely dirty.")]
        private Material materialExtremelyDirty;
        
        /// <summary>
        /// All floors.
        /// </summary>
        public readonly List<Floor> Floors = new();

        /// <summary>
        /// The root game object of the cleaner agent.
        /// </summary>
        private GameObject _cleanerAgent;

        /// <summary>
        /// Keep track of how much time has passed since the last time floor tiles were made dirty.
        /// </summary>
        private float _elapsedTime;

        protected override void Start()
        {
            base.Start();
            GenerateFloor();
        }

        /// <summary>
        /// Render buttons to regenerate the floor or change its size.
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
                GenerateFloor();
            }
            
            // Increase the floor width.
            if (floorSize.x < 5)
            {
                y = NextItem(y, h, p);
                if (GuiButton(x, y, w, h, "Increase Size X"))
                {
                    floorSize.x++;
                    GenerateFloor();
                }
            }

            // Decrease the floor width.
            if (floorSize.x > 1)
            {
                y = NextItem(y, h, p);
                if (GuiButton(x, y, w, h, "Decrease Size X"))
                {
                    floorSize.x--;
                    GenerateFloor();
                }
            }
            
            // Increase the floor height.
            if (floorSize.y < 5)
            {
                y = NextItem(y, h, p);
                if (GuiButton(x, y, w, h, "Increase Size Y"))
                {
                    floorSize.y++;
                    GenerateFloor();
                }
            }

            // Decrease the floor height.
            if (floorSize.y > 1)
            {
                y = NextItem(y, h, p);
                if (GuiButton(x, y, w, h, "Decrease Size Y"))
                {
                    floorSize.y--;
                    GenerateFloor();
                }
            }
            
            return NextItem(y, h, p);
        }

        /// <summary>
        /// Generate the floor.
        /// </summary>
        private void GenerateFloor()
        {
            // Destroy the previous agent.
            if (_cleanerAgent != null)
            {
                Destroy(_cleanerAgent.gameObject);
            }
            
            // Destroy all previous floors.
            foreach (Floor floor in Floors)
            {
                Destroy(floor.gameObject);
            }
            Floors.Clear();
            
            // Generate the floor tiles.
            Vector2 offsets = new Vector2((floorSize.x - 1) / 2f, (floorSize.y - 1) / 2f) * floorScale;
            for (int x = 0; x < floorSize.x; x++)
            {
                for (int y = 0; y < floorSize.y; y++)
                {
                    GenerateFloorTile(new(x, y), offsets);
                }
            }

            // Add the cleaner agent.
            _cleanerAgent = Instantiate(cleanerAgentPrefab, Vector3.zero, quaternion.identity);
            _cleanerAgent.name = "Cleaner Agent";

            // Reset elapsed time.
            _elapsedTime = 0;
        }

        /// <summary>
        /// Generate a floor tile.
        /// </summary>
        /// <param name="position">Its position relative to the rest of the floor tiles.</param>
        /// <param name="offsets">How much to offset the floor tile so all floors are centered around the origin.</param>
        private void GenerateFloorTile(Vector2 position, Vector2 offsets)
        {
            // Create a quad, then position, rotate, size, and name it.
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Quad);
            go.transform.position = new(position.x * floorScale - offsets.x, 0, position.y * floorScale - offsets.y);
            go.transform.rotation = Quaternion.Euler(90, 0, 0);
            go.transform.localScale = new(floorScale, floorScale, 1);
            go.name = $"Floor {position.x} {position.y}";
            
            // Its collider is not needed.
            Destroy(go.GetComponent<Collider>());
            
            // Add and setup its floor component.
            Floor floor = go.AddComponent<Floor>();
            bool likelyToGetDirty = Random.value < likelyToGetDirtyChance;
            floor.Setup(likelyToGetDirty, likelyToGetDirty ? materialCleanLikelyToGetDirty : materialCleanNormal, materialDirty, materialVeryDirty, materialExtremelyDirty);
            Floors.Add(floor);
        }

        protected override void Update()
        {
            base.Update();
            UpdateFloor();
        }

        private void UpdateFloor()
        {
            // Increment how much time has passed and return if it has not been long enough since the last dirt generation.
            _elapsedTime += Time.deltaTime;
            if (_elapsedTime < timeBetweenDirtGeneration)
            {
                return;
            }

            // Reset elapsed time.
            _elapsedTime = 0;
            
            // If all floor tiles are already at max dirt level return as there is nothing more which can be updated.
            if (Floors.Count(f => f.State != Floor.DirtLevel.ExtremelyDirty) == 0)
            {
                return;
            }

            // Get the chance that any tile will become dirty.
            float currentDirtyChance = Mathf.Max(chanceDirty, float.Epsilon);
            
            // We will loop until at least a single tile has been made dirty.
            bool addedDirty = false;
            do
            {
                // Loop through all floor tiles.
                foreach (Floor floor in Floors.Where(f => f.State != Floor.DirtLevel.ExtremelyDirty))
                {
                    // Double the chance to get dirty of the current floor tile is likely to get dirty.
                    float dirtChance = floor.LikelyToGetDirty ? currentDirtyChance * 2 : currentDirtyChance;

                    // Attempt to make each tile dirty three times meaning there is a chance a tile can gain multiple dirt levels at once.
                    for (int i = 0; i < 3; i++)
                    {
                        if (Random.value <= dirtChance)
                        {
                            floor.Dirty();
                            addedDirty = true;
                        }
                    }
                }

                // Double the chances of tiles getting dirty for the next loop so we are not infinitely looping.
                currentDirtyChance *= 2;
            }
            while (!addedDirty);
        }
    }
}