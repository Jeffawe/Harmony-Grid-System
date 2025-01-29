using HarmonyGridSystem.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace HarmonyGridSystem.Objects
{
    [System.Serializable]
    public class SOValues
    {
        public int width;
        public int height;
        public string nameString;
        public string VisualPath;
        public string PrefabPath;
        public PlacedObjectType placedObjectType;

        public SOValues (int width, int height, string nameString, string visualPath, string prefabPath, PlacedObjectType placedObjectType)
        {
            this.width = width;
            this.height = height;
            this.nameString = nameString;
            VisualPath = visualPath;
            PrefabPath = prefabPath;
            this.placedObjectType = placedObjectType;
        }
    }


    public class ChildHolder : MonoBehaviour
    {
        public SOValues sOValues;
        [SerializeField] PlacedObjectType placedObjectType;
        [SerializeField] PlacedObjectSO placedObjectSO;
        [SerializeField] GameObject child;
        private List<GameObject> groundCubes;

        public PlacedObjectType _placedObjectType => placedObjectType;
        public PlacedObjectSO _placedObjectSO => placedObjectSO;
        public GameObject OriginalMesh => child;

        public void AddToChild<T>() where T : Component
        {
            T component = child.GetComponent<T>();

            if (component == null)
            {
                child.AddComponent<T>();
            }
        }

        public void SetSOValues(int width, int height, string nameString, string visualPath, string prefabPath, PlacedObjectType placedObjectType)
        {
            sOValues.width = width;
            sOValues.height = height;
            sOValues.nameString = nameString;
            sOValues.VisualPath = visualPath;
            sOValues.PrefabPath = prefabPath;
            sOValues.placedObjectType = placedObjectType;
        }

        public Vector3 GetChildLocation()
        {
            Debug.Log(child.transform.localPosition);
            return child.transform.localPosition;
        }

        public void SetPlacedObjectType(PlacedObjectType _placedObjectType)
        {
            placedObjectType = _placedObjectType;
        }

        public void SetSO(PlacedObjectSO _placedObjectSO)
        {
            placedObjectSO = _placedObjectSO;
        }

        public void SetChild(GameObject gameObject)
        {
            child = gameObject;
        }

        public void CreateCube(List<GameObject> gameObjectList)
        {
            groundCubes = gameObjectList;
        }

        public void TurnOffCubes()
        {
            if (groundCubes.Count <= 0) return;
            foreach (var item in groundCubes)
            {
                item.SetActive(false);
            }
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, 0.5f);
        }
    }
}
