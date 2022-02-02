﻿using UnityEngine;

/// <summary>
/// Camera for looking at an agent from a set position.
/// </summary>
[RequireComponent(typeof(Camera))]
public class LookAtAgentCamera : MonoBehaviour
{
    [Tooltip("How much to vertically offset the camera for viewing agents.")]
    public float offset = 1;

    [Min(0)]
    [Tooltip("How fast the camera should look to the agent for smooth looking. Set to zero for instant camera looking.")]
    public float lookSpeed = 5;

    private void Start()
    {
        // Snap look right away.
        float look = lookSpeed;
        lookSpeed = 0;
        LateUpdate();
        lookSpeed = look;
    }

    private void LateUpdate()
    {
        // Get the agent to look towards.
        Agent agent = AgentManager.Singleton.SelectedAgent;
        if (agent == null)
        {
            if (AgentManager.Singleton.Agents.Count > 0)
            {
                agent = AgentManager.Singleton.Agents[0];
            }
            else
            {
                return;
            }
        }
            
        // Determine where to look at.
        Vector3 target = agent.Visuals == null ? agent.Position : agent.Visuals.position;
        target = new Vector3(target.x, target.y + offset, target.z);

        // Look instantly.
        if (lookSpeed <= 0)
        {
            transform.LookAt(target);
            return;
        }

        // Look towards it.
        Transform t = transform;
        transform.rotation = Quaternion.Slerp(t.rotation, Quaternion.LookRotation(target - t.position), lookSpeed * Time.deltaTime);
    }
}