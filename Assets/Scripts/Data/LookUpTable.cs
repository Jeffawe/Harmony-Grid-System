using UnityEngine;
using System.Collections.Generic;

namespace HarmonyGridSystem.Objects
{
    [CreateAssetMenu(fileName = "LookupTable", menuName = "Harmony Grid System/LookupTable")]
    public class LookUpTable : ScriptableObject
    {
        [System.Serializable]
        public class Entry
        {
            public string key;
            public PlacedObjectSO objectSO;
        }

        public List<Entry> entries = new List<Entry>();

        public PlacedObjectSO GetSO(string name)
        {
            foreach (var item in entries)
            {
                item.key = item.key.ToLower();
                if (item.key == name)
                {
                    return item.objectSO;
                }
            }

            return null;
        }
    }
}
