using System.Collections.Generic;
using EasyAI;
using EasyAI.Agents;
using EasyAI.Interactions;
using EasyAI.Thinking;
using UnityEngine;
using UnityEngine.InputSystem;

namespace A4.States
{
    [CreateAssetMenu(menuName = "A4/States/Click To Move Global State", fileName = "Click To Move Global State")]
    public class ClickToMoveGlobalState : State
    {
        public override ICollection<AgentAction> Enter(Agent agent)
        {
            agent.AddMessage("Right click anywhere on the map to move to it!");
            return null;
        }

        public override ICollection<AgentAction> Execute(Agent agent)
        {
            if (AgentManager.Singleton.SelectedAgent == agent && Mouse.current.rightButton.wasPressedThisFrame && Physics.Raycast(AgentManager.Singleton.selectedCamera.ScreenPointToRay(new(Mouse.current.position.x.ReadValue(), Mouse.current.position.y.ReadValue(), 0)), out RaycastHit hit, Mathf.Infinity, AgentManager.Singleton.groundLayers | AgentManager.Singleton.obstacleLayers))
            {
                agent.Navigate(hit.point);
            }

            return null;
        }
    }
}