using UnityEngine;

namespace A2
{
    /// <summary>
    /// Simple class for spinning an object.
    /// </summary>
    public class Spinner : MonoBehaviour
    {
        [SerializeField]
        [Min(0)]
        [Tooltip("How fast in degrees per second should the spinner spin.")]
        private float speed;

        private void Update()
        {
            transform.rotation = Quaternion.Euler(new(0, transform.rotation.eulerAngles.y + speed * Time.deltaTime, 0));
        }
    }
}