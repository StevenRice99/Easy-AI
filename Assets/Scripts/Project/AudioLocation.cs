using EasyAI;
using UnityEngine;

namespace Project
{
    /// <summary>
    /// Helper class to simply keep the scene's audio listener with the current camera so audio is positionally correct.
    /// </summary>
    [RequireComponent(typeof(AudioListener))]
    public class AudioLocation : MonoBehaviour
    {
        private void Update()
        {
            if (AgentManager.SelectedCamera != null)
            {
                transform.position = AgentManager.SelectedCamera.transform.position;
            }
        }
    }
}