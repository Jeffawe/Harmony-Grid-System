using HarmonyGridSystem.Grid;
using HarmonyGridSystem.Objects;
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace HarmonyGridSystem.Utils
{
    public class GridObjectSetter : MonoBehaviour
    {
        [Serializable]
        public class Object
        {
            public GameObject fullGameObject;
            public GameObject essentialPart;

            public GameObject EssentialPart
            {
                get
                {
                    if (essentialPart != null || essentialPart != fullGameObject) return essentialPart;
                    return null;
                }
            }
        }

        [Header("General Attributes")]
        [Tooltip("The type of the gameobjects being set")]
        [SerializeField] PlacedObjectType gameObjectType;

        [Tooltip("Where to Place the Pivot")]
        [SerializeField] PivotPoint pivotPoint;

        [Tooltip("Prefab for the ground cube element")]
        [SerializeField] GameObject cubePrefab;

        [Tooltip("The Grid Cell Size")]
        [SerializeField] int gridCellSize;

        [Tooltip("The offset of the y axis for each of the elements")]
        [SerializeField] float yOffset;

        [Tooltip("If a visual duplicate should be created")]
        [SerializeField] bool createVisual;

        [Header("Wall Object Specific")]
        [Tooltip("The layer index for the Edge Objects")]
        [SerializeField] int edgeLayer;

        [Header("Loose Object Specific")]
        [Tooltip("The layer index for the Loose Objects")]
        [SerializeField] int looseObjectLayer;

        [Header("Floor Object Optional")]
        [Tooltip("The size of the edge place holders of the floor. Makes it easier to place the walls for the floor Don't set the X layer to above 1 and the y layer to above 2")]
        [SerializeField] Vector3 edgeSize = new Vector3(0.5f, 2, 0);

        [Space(10)]
        [Tooltip("GameObject to be set.")]
        [SerializeField] List<Object> gameObjects;

        #region Private Variables
        float width;
        float depth;
        bool removeCube;
        Mesh mesh;
        int occupiedGridCellsX;
        int occupiedGridCellsZ;
        int totalOccupiedGrids;
        List<EdgeType> listOfEdges;
        List<FloorEdgePosition> edgePositionsList;
        int selectedGridSize;
        #endregion


        string PrefabPath = "Prefabs/Buildings";
        string VisualPath = "Prefabs/Visuals";


        private void SetUpObject()
        {
            removeCube = true;
            edgePositionsList = new List<FloorEdgePosition>();
            listOfEdges = new List<EdgeType>
            {
                EdgeType.Up,
                EdgeType.Left,
                EdgeType.Down,
                EdgeType.Right
            };

            if (gameObjects.Count == 0) return;

            if (gameObjectType == PlacedObjectType.GridObject || gameObjectType == PlacedObjectType.WallObject)
            {
                GetBestGridSize();
            }
            else
            {
                CreateObject(gameObjects, gameObjectType);
            }
        }

        private void CreateObject(List<Object> gameObjects, PlacedObjectType gameObjectType)
        {
            foreach (var item in gameObjects)
            {
                GameObject newObject = Instantiate(item.fullGameObject, Vector3.zero, item.fullGameObject.transform.rotation);
                GameObject mainPart = (item.EssentialPart == null) ? null : Instantiate(item.EssentialPart, Vector3.zero, item.EssentialPart.transform.rotation);

                if (gameObjectType == PlacedObjectType.GridObject) SetUpGridObject<PlacedObject>(newObject, mainPart, true, false);
                else if (gameObjectType == PlacedObjectType.FloorObject) SetUpFloorObject(newObject, item.essentialPart);
                else if (gameObjectType == PlacedObjectType.WallObject) SetUpGridObject<FloorEdgeObject>(newObject, item.EssentialPart, false, true);
                else SetUpGridObject<PlacedObject>(newObject, item.EssentialPart, false, false, true);
            }
        }

        public void SetUpGridObject<T>(GameObject newObject, GameObject mainPart, bool haveCubes, bool isWall, bool isLooseObject = false) where T : MonoBehaviour
        {
            //Gets the mesh and calculated the x and z axis of it
            mesh = (mainPart == null) ? newObject.GetComponent<MeshFilter>().sharedMesh : mainPart.GetComponent<MeshFilter>().sharedMesh;

            width = Mathf.CeilToInt(mesh.bounds.size.x);
            depth = Mathf.CeilToInt(mesh.bounds.size.z);

            //Calculated the amount of grid spaces it will occupy
            occupiedGridCellsX = (Mathf.RoundToInt(width / gridCellSize) == 0) ? 1 : Mathf.RoundToInt(width / gridCellSize);
            occupiedGridCellsZ = (Mathf.RoundToInt(depth / gridCellSize) == 0) ? 1 : Mathf.RoundToInt(depth / gridCellSize);

            totalOccupiedGrids = occupiedGridCellsX * occupiedGridCellsZ;

            //Gets the Pivot Position
            Vector3 pivotpoint = SetPivotPoint(pivotPoint, width, depth);

            //Creates a new parent object and offsets it
            Transform Parent = new GameObject().transform;
            Vector3 offset = new Vector3(Parent.transform.localScale.x / 2, 0, Parent.transform.localScale.z / 2);
            Parent.gameObject.name = $"{newObject.name}";
            ChildHolder childHolder = Parent.gameObject.AddComponent<ChildHolder>();
            childHolder.SetChild(newObject);
            childHolder.SetPlacedObjectType(gameObjectType);
            List<GameObject> list = new List<GameObject>();

            if (haveCubes)
            {
                //Creates the Ground Grids for the Mesh
                for (int x = 0; x < occupiedGridCellsX; x++)
                {
                    for (int z = 0; z < occupiedGridCellsZ; z++)
                    {
                        Vector3 cubePosition = new Vector3(x, 0, z) * gridCellSize;
                        GameObject cube = Instantiate(cubePrefab);

                        cube.transform.localScale = new Vector3(gridCellSize, 0.1f, gridCellSize);
                        cube.transform.position = newObject.transform.position + cubePosition;
                        cube.transform.forward = newObject.transform.forward;
                        cube.transform.SetParent(Parent);
                        list.Add(cube);
                    }
                }
            }

            float positionX;
            float positionZ;
            if (isWall)
            {
                positionX = 0;
                positionZ = 0;
            }
            else
            {
                //Calculates the position of the Mesh so it can be in the middle
                //It does this in relation to the cube grids
                //This formula is done to account for the fact that it does not start from zero
                positionX = gridCellSize * (occupiedGridCellsX - 1);
                positionZ = gridCellSize * (occupiedGridCellsZ - 1);
            }


            newObject.transform.position = newObject.transform.position + new Vector3(positionX / 2, 0 + yOffset, positionZ / 2);
            Parent.transform.position = newObject.transform.position + pivotpoint - offset;

            //Sets the Parent in order to change the Pivot point
            newObject.transform.SetParent(Parent);
            if (pivotPoint == PivotPoint.Center)
            {
                Parent.transform.position = Vector3.zero;
                newObject.transform.localPosition = Vector3.zero;
            }

            if (isLooseObject) SetLayerRecursively(Parent.gameObject, looseObjectLayer);

            GameObject Parent2 = null;
            if (createVisual) Parent2 = Instantiate(Parent).gameObject;

            if (gameObjectType == PlacedObjectType.FloorObject)
            {
                FloorPlacedObject floorPlacedObject = Parent.gameObject.AddComponent<FloorPlacedObject>();
                floorPlacedObject.SetEdgePositions(edgePositionsList[0], edgePositionsList[2], edgePositionsList[1], edgePositionsList[3]);
            }
            else
            {
                Parent.gameObject.AddComponent<T>();
            }

            Parent.gameObject.AddComponent<PlacedObjectAnimation>();

            if (removeCube && haveCubes) Parent.gameObject.GetComponent<ChildHolder>().TurnOffCubes();

            //GameObject child = Parent2.GetComponent<ChildHolder>().GetChild();
            //child.GetComponent<Renderer>().material.SetColor("_Color", Color.cyan);
            //Saves the Mesh in the Assets
            Utilities.SaveMesh(newObject, newObject.name);

            //Creates a Scriptable Object and Prefab for the Mesh
            PlacedObjectSO building = Utilities.CreateNewScriptableObject<PlacedObjectSO>(newObject.name);

            CreateAndSetSO(building, newObject, Parent, Parent2);

            while (string.IsNullOrEmpty(building.PrefabPath))
            {
                CreateAndSetSO(building, newObject, Parent, Parent2);
            }

            // Optional: Refresh the Unity Editor to see the changes immediately
            EditorUtility.SetDirty(building);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private void SetUpFloorObject(GameObject newObject, GameObject essentialPart)
        {
            Mesh mesh = newObject.GetComponent<MeshFilter>().sharedMesh;
            float currentRot = 0;
            float currentX = -mesh.bounds.size.x;
            float currentZ = mesh.bounds.size.z + 1.5f;
            //Add the Edge Position
            for (int i = 0; i < 4; i++)
            {
                GameObject EdgePosition = GameObject.CreatePrimitive(PrimitiveType.Cube);
                EdgePosition.GetComponent<MeshRenderer>().enabled = false;
                EdgePosition.transform.SetParent(newObject.transform);
                EdgePosition.transform.rotation = Quaternion.Euler(new Vector3(0, currentRot, 0));
                EdgePosition.layer = edgeLayer;

                currentX += mesh.bounds.size.x / 2;
                currentZ -= mesh.bounds.size.z / 2;
                float x = (currentRot == 0 || currentRot == 180) ? currentX : 0;
                float y = 0f;
                float z = (currentRot == 90 || currentRot == 270) ? currentZ : 0;
                EdgePosition.transform.localPosition = new Vector3(x, y, z);
                EdgePosition.transform.localScale = new Vector3(edgeSize.x, edgeSize.y, mesh.bounds.size.z);
                FloorEdgePosition floorEdgePosition = EdgePosition.AddComponent<FloorEdgePosition>();
                floorEdgePosition.SetEdge(listOfEdges[i]);
                edgePositionsList.Add(floorEdgePosition);

                currentRot += 90;
            }

            newObject.transform.localScale = new Vector3(gridCellSize / Mathf.RoundToInt(mesh.bounds.size.x), newObject.transform.localScale.y, gridCellSize / Mathf.RoundToInt(mesh.bounds.size.z));

            SetUpGridObject<FloorPlacedObject>(newObject, essentialPart, false, false);
        }

        public void ModifyObjectScale(GameObject newObject)
        {
            Mesh mesh = newObject.GetComponent<MeshFilter>().sharedMesh;

            if (gameObjectType == PlacedObjectType.GridObject)
            {
                newObject.transform.localScale = new Vector3(gridCellSize / Mathf.CeilToInt(mesh.bounds.size.x), newObject.transform.localScale.y, gridCellSize / Mathf.CeilToInt(mesh.bounds.size.z));
            }
            else if (gameObjectType == PlacedObjectType.WallObject)
            {
                newObject.transform.localScale = new Vector3(gridCellSize / Mathf.RoundToInt(mesh.bounds.size.x), newObject.transform.localScale.y, newObject.transform.localScale.z);
            }
        }

        public void GetBestGridSize()
        {
            List<float> widths = new List<float>();
            List<float> depths = new List<float>();

            foreach (var item in gameObjects)
            {
                GameObject newObject = Instantiate(item.fullGameObject);
                Mesh mesh = newObject.GetComponent<MeshFilter>().sharedMesh;
                widths.Add(mesh.bounds.size.x);
                depths.Add(mesh.bounds.size.z);
                Destroy(newObject);
            }

            float widthH = GetGreatestFloat(widths);
            float widthL = GetLowestFloat(widths);

            float depthH = GetGreatestFloat(depths);
            float depthL = GetLowestFloat(depths);

            float highest = (widthL > depthL) ? widthL : depthL;

            if (gameObjectType == PlacedObjectType.GridObject)
            {
                Debug.Log("Greatest Width is " + widthH + " and lowest width is " + widthL);
                Debug.Log("Greatest Depth is " + depthH + " and lowest depth is " + depthL);
                Debug.Log("I advise that your grid size is " + Mathf.RoundToInt(highest));
                selectedGridSize = Mathf.RoundToInt(highest);

            }
            else if (gameObjectType == PlacedObjectType.WallObject)
            {
                Debug.Log("Greatest Width is " + widthH + " and lowest width is " + widthL);
                Debug.Log("I advice that your grid be " + Mathf.RoundToInt(widthL));
                if (Mathf.RoundToInt(widthL) != Mathf.RoundToInt(widthH)) Debug.Log("All Walls are not the same width. This will cause issues");
                selectedGridSize = Mathf.RoundToInt(widthL);
            }

            if (selectedGridSize == gridCellSize) TakeSelectedGridSize();
        }

        public void TakeSelectedGridSize()
        {
            gridCellSize = selectedGridSize;
            CreateObject(gameObjects, gameObjectType);
        }

        public void SetLayerRecursively(GameObject obj, int layer)
        {
            if (obj.transform == null)
            {
                return;
            }

            // Set the layer of the current object
            obj.layer = layer;

            foreach (Transform child in obj.transform)
            {
                child.gameObject.layer = layer;

                Transform _HasChildren = child.GetComponentInChildren<Transform>();
                if (_HasChildren != null)
                    SetLayerRecursively(child.gameObject, layer);
            }
        }

        public void ContinueWithGridSize()
        {
            foreach (var item in gameObjects)
            {
                ModifyObjectScale(item.fullGameObject);
            }

            CreateObject(gameObjects, gameObjectType);
        }

        private Vector3 SetPivotPoint(PivotPoint pivotPoint, float width = 0, float depth = 0)
        {
            Vector3 BottomRight = new Vector3(-gridCellSize / 2, 0, -gridCellSize / 2);
            Vector3 BottomLeft = new Vector3(-gridCellSize / 2, 0, gridCellSize * occupiedGridCellsZ - (gridCellSize / 2));
            Vector3 TopLeft = new Vector3(gridCellSize * occupiedGridCellsX - (gridCellSize / 2), 0, gridCellSize * occupiedGridCellsZ - (gridCellSize / 2));
            Vector3 TopRight = new Vector3(gridCellSize * occupiedGridCellsX - (gridCellSize / 2), 0, -gridCellSize / 2);

            /*
            BottomRight = new Vector3(-width / 2, 0, -depth / 2);
            BottomLeft = new Vector3(-width / 2, 0, depth * occupiedGridCellsZ - (depth / 2));
            TopLeft = new Vector3(width * occupiedGridCellsX - (width / 2), 0, depth * occupiedGridCellsZ - (depth / 2));
            TopRight = new Vector3(width * occupiedGridCellsX - (width / 2), 0, -depth / 2);
            */

            switch (pivotPoint)
            {
                case PivotPoint.Center:
                    return Vector3.zero;

                case PivotPoint.BottomLeft:
                    return BottomLeft;

                case PivotPoint.BottomRight:
                    return BottomRight;

                case PivotPoint.TopLeft:
                    return TopLeft;

                case PivotPoint.TopRight:
                    return TopRight;

                default:
                    return Vector3.zero;
            }
        }

        private void CreateAndSetSO(PlacedObjectSO building, GameObject newObject, Transform Parent, GameObject Parent2)
        {
            building.width = occupiedGridCellsX;
            building.height = occupiedGridCellsZ;
            building.nameString = newObject.name;
            building.PrefabPath = PrefabPath;
            building.placedObjectType = gameObjectType;
            Utilities.CreatePrefab(Parent.gameObject, PrefabPath, newObject.name, true);

            if (Parent2 == null) building.hasVisual = false;
            else
            {
                if (createVisual) Utilities.CreatePrefab(Parent2, VisualPath, $"{newObject.name}_visual", true);

                building.hasVisual = createVisual;
            }

        }

        public float GetGreatestFloat(List<float> floats)
        {
            float greatest = 0;
            foreach (var item in floats)
            {
                greatest = (item > greatest) ? item : greatest;
            }

            return greatest;
        }

        public float GetLowestFloat(List<float> floats)
        {
            float lowest = 2000;
            foreach (var item in floats)
            {
                lowest = (item < lowest) ? item : lowest;
            }

            return lowest;
        }
    }
}