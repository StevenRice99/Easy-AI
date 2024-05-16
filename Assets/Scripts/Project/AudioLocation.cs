using EasyAI;
using UnityEngine;

namespace Project
{
    /// <summary>
    /// Helper class to simply keep the scene's audio listener with the current camera so audio is positionally correct.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(AudioListener))]
    public class AudioLocation : MonoBehaviour
    {
        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        private void Update()
        {
            if (EasyManager.SelectedCamera != null)
            {
                transform.position = EasyManager.SelectedCamera.transform.position;
            }
        }
    }
}