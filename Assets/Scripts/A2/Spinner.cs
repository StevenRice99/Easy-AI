using UnityEngine;

namespace A2
{
    /// <summary>
    /// Simple class for spinning an object.
    /// </summary>
    public class Spinner : MonoBehaviour
    {
        /// <summary>
        /// How fast in degrees per second should the spinner spin.
        /// </summary>
        [SerializeField]
        [Min(0)]
        [Tooltip("How fast in degrees per second should the spinner spin.")]
        private float speed;

        /// <summary>
        /// Update is called every frame, if the MonoBehaviour is enabled.
        /// </summary>
        private void Update()
        {
            transform.rotation = Quaternion.Euler(new(0, transform.rotation.eulerAngles.y + speed * Time.deltaTime, 0));
        }
    }
}