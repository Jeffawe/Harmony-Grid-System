using HarmonyGridSystem.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace HarmonyGridSystem.Objects
{
    public class ChildHolder : MonoBehaviour
    {
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
    }
}
