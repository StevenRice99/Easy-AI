using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace EasyAI.Navigation.Nodes
{
    [CreateAssetMenu(menuName = "Easy-AI/Lookup Table", fileName = "Lookup Table", order = 0)]
    public class LookupTable : ScriptableObject
    {
        public NavigationLookup[] Read => data?.ToArray();
        
        [Tooltip("Navigation data.")]
        [SerializeField]
        private NavigationLookup[] data;

        public void Write(IEnumerable<NavigationLookup> write)
        {
            data = write.ToArray();
        }
    }
}