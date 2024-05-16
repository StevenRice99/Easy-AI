using System.Linq;
using UnityEngine;

namespace EasyAI.Navigation.Utility
{
    /// <summary>
    /// These manually placed will be connected by an agent manager but have no other logic themselves.
    /// </summary>
    public class EasyNode : MonoBehaviour
    {
        /// <summary>
        /// Remove the nodes at runtime.
        /// </summary>
        public void Remove()
        {
            enabled = false;
            if (transform.childCount == 0 && !GetComponents<MonoBehaviour>().Any(m => m != this && m.enabled))
            {
                Destroy(gameObject);
                return;
            }

            Destroy(this);
        }
    }
}