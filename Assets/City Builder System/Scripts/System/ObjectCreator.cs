using HarmonyGridSystem.Grid;
using HarmonyGridSystem.Objects;
using System.Collections.Generic;
using UnityEngine;

namespace HarmonyGridSystem.Builder
{
    [RequireComponent(typeof(CityGridSetter))]
    public class ObjectCreator : MonoBehaviour
    {
        [SerializeField] GridBuildingSystem3D gridBuilder;
        [SerializeField] PlacedObjectSO defaultFloorObject;
        [SerializeField] CreationType creationType;

        [SerializeField] int width;
        [SerializeField] int height;
        [SerializeField] private TextAsset jsonFile;
        [SerializeField] private int cellSize = 100;
        [SerializeField] private LookUpTable lookUpTable;

        private CityGridSetter gridSetter;
        private List<GridObj> gridObjects;

        // Start is called before the first frame update
        void Start()
        {
            gridSetter = GetComponent<CityGridSetter>();
            if (creationType == CreationType.Building) CreateFloor(width, height);

            bool success = gridSetter.ArrangeGridObj(new Vector2Int(width, height), cellSize, jsonFile, lookUpTable, out gridObjects);

            if(success)
            {
                PlaceObjectDown();
            }
        }


        public void PlaceObjectDown()
        {
            for (int i = 0; i < gridObjects.Count; i++)
            {
                PlacedObjectSO so = lookUpTable.GetSO(gridObjects[i].name.ToLower());
                if (so == null) continue;
                
                if(creationType == CreationType.Building)
                {
                    gridBuilder.PlaceGridObject(so, false, InputController.GetMousePosition(), new Vector2Int(gridObjects[i].x, gridObjects[i].y));
                }
                else
                {
                    PlaceDownLooseObject(so, gridObjects[i].direction, gridObjects[i].x, gridObjects[i].y);
                }
            }
        }

        /// <summary>
        /// Creates the ground
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        private void CreateFloor(int width, int height)
        {
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    if (gridBuilder != null) gridBuilder.PlaceGridObject(defaultFloorObject, false, InputController.GetMousePosition(), new Vector2Int(x, z));
                }
            }
        }

        private void PlaceDownLooseObject(PlacedObjectSO objectToPlace, float orientation, int xValue, int yValue)
        {
            Vector3 orientation_vector = CalculateOrientation(orientation);
            Vector3 position = gridBuilder.Grid.GetWorldPosition(xValue, yValue);
            if (gridBuilder != null) gridBuilder.PlaceLooseObject(objectToPlace, false, orientation_vector, position);
            Debug.Log("Table placed at " + xValue + " and " + yValue);
        }

        private Vector3 CalculateOrientation(float orientation)
        {
            switch (orientation)
            {
                case 0:
                    return Vector3.forward;

                case 1:
                    return Vector3.back;

                case 2:
                    return Vector3.left;

                case 3:
                    return Vector3.right;

                default:
                    return Vector3.forward;
            }

        }
    }

    public enum CreationType
    {
        City,
        Building
    }
}
