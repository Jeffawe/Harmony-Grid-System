using HarmonyGridSystem.Grid;
using HarmonyGridSystem.Objects;
using UnityEngine;

namespace HarmonyGridSystem.Builder
{
    public class ObjectCreator : MonoBehaviour
    {
        [SerializeField] GridBuildingSystem3D gridBuilder;
        [SerializeField] PlacedObjectSO floorObject;
        [SerializeField] CreationType creationType;

        [SerializeField] int width;
        [SerializeField] int height;

        // Start is called before the first frame update
        void Start()
        {
            if (creationType == CreationType.Building) CreateFloor(width, height);
        }

        /// <summary>
        /// Places an Object on the Grid (For Building types only)
        /// </summary>
        /// <param name="objectToPlace">The SO of the object to place</param>
        /// <param name="orientation">The Orientation of the Object</param>
        /// <param name="xVector">the x axis of the object</param>
        /// <param name="yVector">the y axis f the object</param>
        /// <param name="maxX">the width of the page</param>
        /// <param name="maxY">th height of the page</param>
        public void PlaceObjectDown(PlacedObjectSO objectToPlace, float orientation, int xVector, int yVector, int maxX, int maxY)
        {
            int newX = ScaleDownValue(xVector, maxX, width);
            int newY = ScaleDownValue(yVector, maxY, height);

            PlaceDownLooseObject(objectToPlace, orientation, newX, newY);
        }

        public void PlaceBuildingDown()
        {

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
                    if (gridBuilder != null) gridBuilder.PlaceGridObject(floorObject, false, InputController.GetMousePosition(), new Vector2Int(x, z));
                }
            }
        }

        private int ScaleDownValue(float value, int previousMaxValue, int newMaxValue)
        {
            float newValue = value / previousMaxValue;
            float finalValue = Mathf.Min(newValue * newMaxValue, newMaxValue);
            return Mathf.RoundToInt(finalValue);
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
