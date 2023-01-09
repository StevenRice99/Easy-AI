using EasyAI.Managers;
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
            if (Manager.SelectedCamera != null)
            {
                transform.position = Manager.SelectedCamera.transform.position;
            }
        }
    }
}