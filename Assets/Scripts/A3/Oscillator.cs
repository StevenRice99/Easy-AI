using UnityEngine;

namespace A3
{
    public class Oscillator : MonoBehaviour
    {
        [SerializeField]
        [Tooltip("How fast in meters per second should the oscillator move along the X axis.")]
        private float speed;
    
        [SerializeField]
        [Tooltip("The bounds in meters to limit the movement on the X axis.")]
        private Vector2 bounds;

        private bool _positive = true;

        private void Update()
        {
            Vector3 position = transform.position;

            float x;
            if (_positive)
            {
                x = position.x + speed * Time.deltaTime;
                if (x >= bounds.y)
                {
                    x = bounds.y;
                    _positive = false;
                }
            }
            else
            {
                x = position.x - speed * Time.deltaTime;
                if (x <= bounds.x)
                {
                    x = bounds.x;
                    _positive = true;
                }
            }
        
            transform.position = new(x, position.y, position.z);
        }
    }
}