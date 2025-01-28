using HarmonyGridSystem.Objects;
using HarmonyGridSystem.Utils;
using UnityEngine;

namespace HarmonyGridSystem.Grid
{
    [RequireComponent(typeof(GridBuildingSystem3D))]
    public class GridBuildInEditor : MonoBehaviour
    {

        [Header("Building Editor Settings")]
        [ReadOnly]
        public PlacedObjectSO currentSO;

        [Tooltip("Total number of placeable objects")]
        [ReadOnly]
        [SerializeField] private int objectsToPlaceCount;

        [ReadOnly]
        [SerializeField] PlacedObjectType objectType;

        [Tooltip("Index of current object in placedObjects list")]
        [SerializeField] private int currentSOIndex;

        [Tooltip("Grid position (X,Z) for Editor object placement")]
        [SerializeField] private Vector2Int gridObjectPlace;

        [Tooltip("Grid position (X,Z) for Editor Loose object placement")]
        [SerializeField] private Vector3 looseObjectPlace;

        private GridBuildingSystem3D gridBuilder;

        public void CreateGrid()
        {
            gridBuilder = GetComponent<GridBuildingSystem3D>();
            gridBuilder.CreateGrid();
            objectsToPlaceCount = gridBuilder.objectsToPlaceCount;
            currentSOIndex = gridBuilder.currentSOIndex;
        }

        public void ChangeObject()
        {
            if (gridBuilder == null)
            {
                Debug.LogWarning("Create a Grid First");
                return;
            }
            gridBuilder.ChangeObject(currentSOIndex);
            currentSO = gridBuilder.placedObjectSO;
            objectType = gridBuilder.placedObjectSO.placedObjectType;
        }

        public void PlaceObject()
        {
            if (gridBuilder == null)
            {
                Debug.LogWarning("Create a Grid First");
                return;
            }
            gridBuilder.PlaceObject(looseObjectPlace, gridObjectPlace);
        }

        public void ClearObjects()
        {
            gridBuilder = GetComponent<GridBuildingSystem3D>();
            gridBuilder.ClearExistingGrids();
        }

        public void ChangeGridSelect()
        {
            if(gridBuilder == null)
            {
                Debug.LogWarning("Create a Grid First");
                return;
            }
            gridBuilder.HandleGridSelect();
        }
    }
}
