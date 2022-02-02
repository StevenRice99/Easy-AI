using UnityEngine;

namespace Samples
{
    /// <summary>
    /// The AgentManager itself is able to be extended although it is not necessary.
    /// If you have a static level you built, the default AgentManager will be fine in most cases.
    /// However, situations with procedural generation or changing environments may merit extending the manager to do so.
    /// </summary>
    public class SampleAgentManager : AgentManager
    {
        /// <summary>
        /// Used for spawning different kinds of agents for this sample.
        /// </summary>
        private enum AgentType : byte
        {
            Transform,
            Character,
            Rigidbody
        }
    
        protected override void Start()
        {
            // Spawn all three types of agents.
            CreateSampleArea(new Vector3(-15, 0, 0), AgentType.Transform);
            CreateSampleArea(Vector3.zero, AgentType.Character);
            CreateSampleArea(new Vector3(15, 0, 0), AgentType.Rigidbody);
            
            // Ensure the AgentManager sets itself of properly.
            base.Start();
        }

        /// <summary>
        /// Create a simple room with an agent and some rigidbody obstacles for the sample scene.
        /// </summary>
        /// <param name="position">The position to generate the room at.</param>
        /// <param name="agentType">The type of agent to create.</param>
        private static void CreateSampleArea(Vector3 position, AgentType agentType)
        {
            // Create the main floor.
            GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Quad);
            floor.name = $"{agentType} Agent Area";
            floor.transform.position = position;
            floor.transform.rotation = Quaternion.Euler(90, 0, 0);
            floor.transform.localScale = new Vector3(10, 10, 1);
        
            // Create the top wall.
            GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wall.name = "Wall Top";
            wall.transform.position = new Vector3(position.x, position.y + 2.5f, position.z + 5);
            wall.transform.localScale = new Vector3(10, 5, 1);
            wall.transform.SetParent(floor.transform);
        
            // Create the bottom wall.
            wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wall.name = "Wall Bottom";
            wall.transform.position = new Vector3(position.x, position.y + 2.5f, position.z - 5);
            wall.transform.localRotation =  Quaternion.Euler(0, 180, 0);
            wall.transform.localScale = new Vector3(10, 5, 1);
            wall.transform.SetParent(floor.transform);
        
            // Create the right wall.
            wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wall.name = "Wall Right";
            wall.transform.position = new Vector3(position.x + 5, position.y + 2.5f, position.z);
            wall.transform.localRotation =  Quaternion.Euler(0, 90, 0);
            wall.transform.localScale = new Vector3(10, 5, 1);
            wall.transform.SetParent(floor.transform);
        
            // Create the left wall.
            wall = GameObject.CreatePrimitive(PrimitiveType.Quad);
            wall.name = "Wall Left";
            wall.transform.position = new Vector3(position.x - 5, position.y + 2.5f, position.z);
            wall.transform.localRotation =  Quaternion.Euler(0, 270, 0);
            wall.transform.localScale = new Vector3(10, 5, 1);
            wall.transform.SetParent(floor.transform);

            // Create the agent.
            GameObject agent = agentType switch
            {
                AgentType.Transform => EasyAIStatic.CreateTransformAgent(),
                AgentType.Character => EasyAIStatic.CreateCharacterAgent(),
                _ => EasyAIStatic.CreateRigidbodyAgent()
            };
            agent.transform.position = position;
            agent.transform.SetParent(floor.transform);
            agent.AddComponent<SampleMind>();
            SampleSensor sensor = agent.AddComponent<SampleSensor>();
            sensor.origin = position;
            sensor.size = 5;
            sensor.target = position;
            
            // Add rigidbody obstacles.
            GameObject obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.position = new Vector3(position.x + 2.5f, position.y + 2, position.z + 2.5f);
            Rigidbody rb = obstacle.AddComponent<Rigidbody>();
            rb.mass = 0.01f;
            
            obstacle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obstacle.transform.position = new Vector3(position.x + 2.5f, position.y + 2, position.z - 2.5f);
            rb = obstacle.AddComponent<Rigidbody>();
            rb.mass = 0.01f;
            
            obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
            obstacle.transform.position = new Vector3(position.x - 2.5f, position.y + 2, position.z - 2.5f);
            rb = obstacle.AddComponent<Rigidbody>();
            rb.mass = 0.01f;
            
            obstacle = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obstacle.transform.position = new Vector3(position.x - 2.5f, position.y + 2, position.z + 2.5f);
            rb = obstacle.AddComponent<Rigidbody>();
            rb.mass = 0.01f;
        }
    }
}