using UnityEngine;

namespace Warehouse
{
    public class Part : MonoBehaviour
    {
        [field: Tooltip("The ID for this part.")]
        [field: SerializeField]
        public int ID { get; private set; }
    }
}