using EasyAI;
using EasyAI.Thinking;
using UnityEngine;
using UnityEngine.InputSystem;

namespace A4.Minds
{
    public class ClickMoveMind : Mind
    {
        private void Update()
        {
            if (AgentManager.Singleton.SelectedAgent == Agent && Mouse.current.rightButton.wasPressedThisFrame && Physics.Raycast(AgentManager.Singleton.selectedCamera.ScreenPointToRay(new(Mouse.current.position.x.ReadValue(), Mouse.current.position.y.ReadValue(), 0)), out RaycastHit hit, Mathf.Infinity, AgentManager.Singleton.groundLayers | AgentManager.Singleton.obstacleLayers))
            {
                Agent.Navigate(hit.point);
            }
        }
    }
}