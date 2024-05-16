using System.Collections;
using UnityEngine;

namespace A2
{
    /// <summary>
    /// Helper class to simply destroy an object after a given amount of time.
    /// </summary>
    [DisallowMultipleComponent]
    public class DestroyAfterTime : MonoBehaviour
    {
        /// <summary>
        /// The time to wait before destroying this object.
        /// </summary>
        [Tooltip("The time to wait before destroying this object.")]
        [Min(float.Epsilon)]
        [SerializeField]
        private float duration = 1f;

        /// <summary>
        /// Start is called on the frame when a script is enabled just before any of the Update methods are called the first time.
        /// </summary>
        private void Start()
        {
            StartCoroutine(DestroyAfterSeconds());
        }

        /// <summary>
        /// Coroutine that waits for a given time before destroying this object.
        /// </summary>
        /// <returns>Nothing.</returns>
        private IEnumerator DestroyAfterSeconds()
        {
            yield return new WaitForSeconds(duration);
            Destroy(gameObject);
        }
    }
}