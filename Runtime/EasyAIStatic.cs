using UnityEngine;

public static class EasyAIStatic
{
    /// <summary>
    /// Create a transform agent.
    /// </summary>
    public static GameObject CreateTransformAgent()
    {
        GameObject agent = CreateAgent("Transform Agent");
        agent.AddComponent<TransformAgent>();
        return agent;
    }

    /// <summary>
    /// Create a character controller agent.
    /// </summary>
    public static GameObject CreateCharacterAgent()
    {
        GameObject agent = CreateAgent("Character Agent");
        CharacterController c = agent.AddComponent<CharacterController>();
        c.center = new Vector3(0, 1, 0);
        c.minMoveDistance = 0;
        agent.AddComponent<CharacterAgent>();
        return agent;
    }

    /// <summary>
    /// Create a rigidbody agent.
    /// </summary>
    public static GameObject CreateRigidbodyAgent()
    {
        GameObject agent = CreateAgent("Rigidbody Agent");
        CapsuleCollider c = agent.AddComponent<CapsuleCollider>();
        c.center = new Vector3(0, 1, 0);
        c.height = 2;
        Rigidbody rb = agent.AddComponent<Rigidbody>();
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.freezeRotation = true;
        agent.AddComponent<RigidbodyAgent>();
        return agent;
    }

    /// <summary>
    /// Create all types of cameras which only adds in those that are not yet present in the scene.
    /// </summary>
    public static void CreateAllCameras()
    {
        if (Object.FindObjectOfType<FollowAgentCamera>() == null)
        {
            CreateFollowAgentCamera();
        }
        else
        {
            Debug.Log("Already have a follow agent camera in the scene - skipping creating one.");
        }
        
        if (Object.FindObjectOfType<LookAtAgentCamera>() == null)
        {
            CreateLookAtAgentCamera();
        }
        else
        {
            Debug.Log("Already have a look at agent camera in the scene - skipping creating one.");
        }
        
        if (Object.FindObjectOfType<TrackAgentCamera>() == null)
        {
            CreateTrackAgentCamera();
        }
        else
        {
            Debug.Log("Already have a track agent camera in the scene - skipping creating one.");
        }
    }

    /// <summary>
    /// Create a follow agent camera.
    /// </summary>
    public static GameObject CreateFollowAgentCamera()
    {
        GameObject camera = CreateCamera("Follow Camera");
        camera.AddComponent<FollowAgentCamera>();
        return camera;
    }

    /// <summary>
    /// Create a look at agent camera.
    /// </summary>
    public static GameObject CreateLookAtAgentCamera()
    {
        GameObject camera = CreateCamera("Look At Camera");
        camera.AddComponent<LookAtAgentCamera>();
        return camera;
    }

    /// <summary>
    /// Create a track agent camera.
    /// </summary>
    public static GameObject CreateTrackAgentCamera()
    {
        GameObject camera = CreateCamera("Track Camera");
        camera.AddComponent<TrackAgentCamera>();
        camera.transform.localRotation = Quaternion.Euler(90, 0, 0);
        return camera;
    }

    /// <summary>
    /// Base method for setting up the core visuals of an agent.
    /// </summary>
    /// <param name="name">The name to give the agent.</param>
    /// <returns>Game object with the visuals setup for a basic agent.</returns>
    public static GameObject CreateAgent(string name)
    {
        GameObject agent = new GameObject(name);

        GameObject visuals = new GameObject("Visuals");
        visuals.transform.SetParent(agent.transform);
        visuals.transform.localPosition = Vector3.zero;
        visuals.transform.localRotation = Quaternion.identity;
            
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        body.name = "Body";
        body.transform.SetParent(visuals.transform);
        body.transform.localPosition = new Vector3(0, 1, 0);
        body.transform.localRotation = Quaternion.identity;
        Object.DestroyImmediate(body.GetComponent<CapsuleCollider>());
            
        GameObject eyes = GameObject.CreatePrimitive(PrimitiveType.Cube);
        eyes.name = "Eyes";
        eyes.transform.SetParent(body.transform);
        eyes.transform.localPosition = new Vector3(0, 0.4f, 0.25f);
        eyes.transform.localRotation = Quaternion.identity;
        eyes.transform.localScale = new Vector3(1, 0.2f, 0.5f);
        Object.DestroyImmediate(eyes.GetComponent<BoxCollider>());

        return agent;
    }

    /// <summary>
    /// Base method for setting up a camera.
    /// </summary>
    /// <param name="name">The name to give the camera.</param>
    /// <returns>Game object with a camera.</returns>
    public static GameObject CreateCamera(string name)
    {
        GameObject camera = new GameObject(name);
        camera.AddComponent<Camera>();
        return camera;
    }
}
