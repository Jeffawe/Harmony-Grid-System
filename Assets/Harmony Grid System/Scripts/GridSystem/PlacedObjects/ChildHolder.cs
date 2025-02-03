using HarmonyGridSystem.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace HarmonyGridSystem.Objects
{
    [System.Serializable]
    public class SOValues
    {
        public int width;
        public int breadth;
        public int height;
        public bool isMultiple;
        public string nameString;
        public bool isVisual;
        public string PrefabPath;
        public PlacedObjectType placedObjectType;
    }


    public class ChildHolder : MonoBehaviour
    {
        public SOValues sOValues;
        [SerializeField] PlacedObjectType placedObjectType;
        [SerializeField] PlacedObjectSO placedObjectSO;
        [SerializeField] GameObject child;
        private Transform groundCube;

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

        public void SetSOValues(int width, int breadth, int height, string nameString, bool isVisual, string prefabPath, PlacedObjectType placedObjectType)
        {
            sOValues.width = width;
            sOValues.breadth = breadth;
            sOValues.height = height;
            if (sOValues.height > 1) sOValues.isMultiple = true;
            sOValues.nameString = nameString;
            sOValues.isVisual = isVisual;
            sOValues.PrefabPath = prefabPath;
            sOValues.placedObjectType = placedObjectType;
            this.placedObjectType = placedObjectType;
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

        public void CreateCube(Transform gameObject)
        {
            groundCube = gameObject;
        }

        public void TurnOffCubes()
        {
            groundCube.gameObject.SetActive(false);
        }

        void OnDrawGizmos()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawCube(transform.position, Vector3.one * 1f);
        }
    }
}
