using HarmonyGridSystem.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace HarmonyGridSystem.Objects
{
    public class PlacedObject : MonoBehaviour
    {
        private PlacedObjectSO placedObjectSO;
        private PlacedObjectSO.Dir dir;
        private Vector2Int origin;
        [SerializeField] private int gridWidth;
        [SerializeField] private int gridHeight;

        public int Width => gridWidth;
        public int Height => gridHeight;
        public PlacedObjectSO PlacedObjectSOValue => placedObjectSO;

        public void Initialize(int width, int height)
        {
            gridWidth = width;
            gridHeight = height;
        }

        public PlacedObjectType placedObjectType
        {
            get
            {
                return placedObjectSO.placedObjectType;
            }
        }

        public static PlacedObject Create(PlacedObjectSO placedObjectSO, Vector3 worldPos, PlacedObjectSO.Dir currentDir, Vector2Int origin)
        {
            Transform placedObjectTransform = Instantiate(placedObjectSO.Prefab, worldPos, Quaternion.Euler(0, placedObjectSO.GetRotationAngle(currentDir), 0));
            placedObjectSO.Prefab.GetComponent<ChildHolder>()?.SetSO(placedObjectSO);
            PlacedObject placedObject = placedObjectTransform.GetComponent<PlacedObject>();
            placedObject.placedObjectSO = placedObjectSO;
            placedObject.origin = origin;
            placedObject.dir = currentDir;

            return placedObject;
        }

        public List<Vector2Int> GetGridPositionList()
        {
            return placedObjectSO.GetGridPosition(origin, dir);
        }

        virtual public void DestroyGameObject()
        {
            Destroy(gameObject);
        }
    }
}
