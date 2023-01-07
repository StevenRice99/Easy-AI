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
            if (AgentManager.Singleton.selectedCamera != null)
            {
                transform.position = AgentManager.Singleton.selectedCamera.transform.position;
            }
        }
    }
}