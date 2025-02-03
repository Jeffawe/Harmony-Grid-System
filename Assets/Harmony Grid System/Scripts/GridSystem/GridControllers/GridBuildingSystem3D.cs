using HarmonyGridSystem.Utils;
using HarmonyGridSystem.Objects;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace HarmonyGridSystem.Grid
{
    /// <summary>
    /// Manages the 3D grid building system, handling object placement, deletion, and grid management.
    /// Must be attached to a GameObject with a GridManager component.
    /// </summary>
    [RequireComponent(typeof(GridManager))]
    public class GridBuildingSystem3D : MonoBehaviour
    {
        #region Singleton Pattern
        private static GridBuildingSystem3D _instance;
        public static GridBuildingSystem3D Instance
        {
            get => _instance;
            private set
            {
                if (_instance != null && _instance != value)
                {
                    Destroy(value.gameObject);
                    return;
                }
                _instance = value;
            }
        }
        #endregion

        #region Properties and Fields
        /// <summary>
        /// Reference to the GridManager component
        /// </summary>
        [Tooltip("Automatically assigned GridManager component")]
        public GridManager gridManager { get; private set; }

        /// <summary>
        /// Layer mask for detecting ground objects that can register mouse clicks
        /// </summary>
        [Tooltip("The Layer Mask for the Ground Object the Mouse can hit to register mouse clicks")]
        [SerializeField] private LayerMask mouseMask;

        /// <summary>
        /// Layer mask used for edge detection
        /// </summary>
        [Tooltip("The Layer Mask used to look for Grid Edges")]
        [SerializeField] private LayerMask edgeMask;

        [Tooltip("The layer integer loose objects are on")]
        public LayerMask looseObjectLayer;

        /// <summary>
        /// Ghost object prefabs for preview animations
        /// </summary>
        [Header("Ghost Objects")]
        [Tooltip("Preview object for placed grid items")]
        [SerializeField] private GameObject PlacedGridGhost;
        [Tooltip("Preview object for loose items")]
        [SerializeField] private GameObject LooseGridGhost;
        [Tooltip("Preview object for wall items")]
        [SerializeField] private GameObject WallGridGhost;

        [Tooltip("Debug options for grid visualization")]
        [SerializeField] private DebugOptions gridVisualizationOptions;

        /// <summary>
        /// The current index of the current object to be placed in the list
        /// </summary>
        public int currentSOIndex { get; private set; }

        /// <summary>
        /// The number of objects available to be placed
        /// </summary>
        public int objectsToPlaceCount { get; private set; }

        /// <summary>
        /// The Current Object to be Placed
        /// </summary>
        public PlacedObjectSO placedObjectSO { get; private set; }


        // Cached variables for better performance
        private readonly Dictionary<PlacedObjectType, bool> filterType = new Dictionary<PlacedObjectType, bool>();
        private List<Grid3D<GridObject>> gridList;
        private Grid3D<GridObject> grid;
        private float looseObjectEulerY;
        private PlacedObjectSO.Dir currentDir = PlacedObjectSO.Dir.Down;
        private int currentGridIndex;
        private bool isEditMode;
        private float floorYoffset;

        // Quick access property
        public Grid3D<GridObject> Grid => grid;
        #endregion

        #region Events
        /// <summary>
        /// Triggered when the active grid level changes
        /// </summary>
        public event EventHandler OnActiveGridLevelChanged;

        /// <summary>
        /// Triggered when the selected object changes
        /// </summary>
        public event EventHandler OnSelectedChanged;

        /// <summary>
        /// Triggered when an object is placed on the grid
        /// </summary>
        public event EventHandler OnObjectPlaced;
        #endregion

        #region Unity Lifecycle Methods
        private void Awake()
        {
            Instance = this;
            gridManager = GetComponent<GridManager>();
            CreateGrid();
        }

        private void Update()
        {
            HandleInput();
        }
        #endregion

        #region Input Handling
        /// <summary>
        /// Centralized input handling for better organization and maintenance
        /// </summary>
        private void HandleInput()
        {
            if (InputController.ChangeCurrentGrid())
                HandleGridSelect();

            if (InputController.GetLeftMouseButton() && placedObjectSO != null)
                PlaceObject(InputController.GetMousePosition(), default);

            if (InputController.EditObject())
                HandleEditObject(InputController.GetMousePosition());

            if (InputController.ChangeObject())
                ChangeObject();

            if (InputController.DeselectObject())
                DeselectObjectType();

            if (InputController.GetRightMouseButton())
                HandleDeleteObject(out _, InputController.GetMousePosition());

            if (InputController.RotateObject())
                RotateGridObject();


        }
        #endregion

        #region Grid Creation
        /// <summary>
        /// Creates and initializes the grid system with multiple layers
        /// </summary>
        public void CreateGrid()
        {
            if (!Application.isPlaying)
            {
                gridManager = GetComponent<GridManager>();
            }

            ClearExistingGrids();
            InitializeNewGrids();
            InitializeGridState();
            InitializeFilterDictionary();
        }

        /// <summary>
        /// Clears existing grids and their contents
        /// </summary>
        public void ClearExistingGrids()
        {
            if (gridList?.Count > 0)
            {
                foreach (var gridItem in gridList)
                {
                    // Iterate through all grid objects in 3D space
                    for (int x = 0; x < grid.GetWidth(); x++)
                    {
                        for (int y = 0; y < grid.GetBreath(); y++)
                        {
                            DestroyImmediate(grid.GetGridObject(x, y)?.GetPlacedObject()?.gameObject);
                            grid.GetGridObject(x, y)?.ClearPlacedObject();
                        }
                    }
                    gridItem.ClearGrid();
                }

                gridList.Clear();
            }
        }

        /// <summary>
        /// Initializes new grid layers based on GridManager settings
        /// </summary>
        private void InitializeNewGrids()
        {
            gridList = new List<Grid3D<GridObject>>();

            for (int i = 0; i < gridManager.GridYCount; i++)
            {
                Vector3 origin = new Vector3(
                    transform.position.x,
                    gridManager.GridYSize * i,
                    transform.position.z
                );

                var newGrid = new Grid3D<GridObject>(
                    gridManager.GridWidth,
                    gridManager.GridHeight,
                    gridManager.CellSize,
                    origin,
                    gridVisualizationOptions,
                    (Grid3D<GridObject> grid, int x, int z) => new GridObject(x, z, grid)
                );

                gridList.Add(newGrid);
            }
        }


        /// <summary>
        /// Initializes the initial grid state
        /// </summary>
        private void InitializeGridState()
        {
            currentGridIndex = 0;
            currentSOIndex = 0;
            grid = gridList[0];
            objectsToPlaceCount = gridManager.placedObjects.entries.Count;
        }

        /// <summary>
        /// Initializes the filter dictionary for object types
        /// </summary>
        private void InitializeFilterDictionary()
        {
            filterType.Clear();
            filterType[PlacedObjectType.FloorObject] = false;
            filterType[PlacedObjectType.GridObject] = false;
            filterType[PlacedObjectType.WallObject] = false;
            filterType[PlacedObjectType.LooseObject] = true;
        }
        #endregion

        #region Object Placement
        /// <summary>
        /// Change the current Object that it to place
        /// </summary>
        /// <param name="currentIndex">A reference to the current index of the object that it sto be placed</param>
        public void ChangeObject(int currentIndex = int.MaxValue)
        {
            if (grid == null) Debug.LogError("Grid not Created");

            if (currentIndex != int.MaxValue) currentSOIndex = currentIndex;

            if (!Application.isPlaying)
            {
                if (currentSOIndex >= gridManager.placedObjects.entries.Count) currentSOIndex = gridManager.placedObjects.entries.Count - 1;
                placedObjectSO = gridManager.placedObjects.entries[currentSOIndex].objectSO;
                RefreshSelectedObjectType();
            }
            else
            {
                if (gridManager.placedObjects.entries.Count <= 0) return;
                if (currentSOIndex >= gridManager.placedObjects.entries.Count) currentSOIndex = 0;
                placedObjectSO = gridManager.placedObjects.entries[currentSOIndex].objectSO;
                RefreshSelectedObjectType();
            }
        }

        /// <summary>
        /// Places the Object at a specific position
        /// </summary>
        /// <param name="gridPos">The position to place other grid objects</param>
        /// <param name="mousePos">The Vector3 Position to place the object at. This can either be the mouse position or the position for a loose object</param>
        public void PlaceObject(Vector3 mousePos, Vector2Int gridPos)
        {
            if (placedObjectSO == null)
            {
                Debug.LogWarning("No Object To Place");
                return;
            }

            if (!Application.isPlaying)
            {
                if (placedObjectSO.placedObjectType == PlacedObjectType.WallObject)
                {
                    PlaceWallObject(placedObjectSO, false, mousePos, EdgeType.Up, gridPos);
                }
                else if (placedObjectSO.placedObjectType == PlacedObjectType.LooseObject)
                {
                    PlaceLooseObject(placedObjectSO, false, mousePos, default, mousePos);
                }
                else if (placedObjectSO.placedObjectType == PlacedObjectType.ZoneObject)
                {
                    PlaceZoneObject(placedObjectSO, false, mousePos, false, gridPos);
                }
                else
                {
                    PlaceGridObject(placedObjectSO, false, mousePos, gridPos);
                }
            }
            else
            {
                if (placedObjectSO.placedObjectType == PlacedObjectType.WallObject)
                {
                    PlaceWallObject(placedObjectSO, true, mousePos);
                }
                else if (placedObjectSO.placedObjectType == PlacedObjectType.LooseObject)
                {
                    PlaceLooseObject(placedObjectSO, true, mousePos);
                }
                else if (placedObjectSO.placedObjectType == PlacedObjectType.ZoneObject)
                {
                    PlaceZoneObject(placedObjectSO, true, mousePos, true);
                }
                else
                {
                    PlaceGridObject(placedObjectSO, true, mousePos);
                }
            }
        }

        /// <summary>
        /// Function used to Place a Wall Object
        /// </summary>
        /// <param name="placedObjectSO">The Scriptable Object with Data on the Object to be Placed</param>
        /// <param name="useMousePosition">If a mouse position or a position given should be used to place the object</param>
        /// <param name="mousePos">The position of the Mouse</param>
        /// <param name="orientation">The orientation of the object being placed</param>
        /// <param name="position">The grid position of the object if not using a mouse position</param>
        public void PlaceWallObject(PlacedObjectSO placedObjectSO, bool useMousePosition, Vector3 mousePos, EdgeType orientation = EdgeType.Up, Vector2Int position = default)
        {
            if (!useMousePosition)
            {
                PlacedObject placedObject = grid.GetGridObject(position.x, position.y)?.GetPlacedObject();
                if (placedObject.gameObject.TryGetComponent(out FloorPlacedObject floorPlacedObject))
                {
                    // Place Object on Edge
                    floorPlacedObject.PlaceEdge(orientation, placedObjectSO);
                }
            }
            else
            {
                Ray ray = Camera.main.ScreenPointToRay(mousePos);
                if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, edgeMask))
                {
                    // Raycast Hit Edge Object
                    if (raycastHit.collider.TryGetComponent(out FloorEdgePosition floorEdgePosition))
                    {
                        CheckParent(raycastHit.collider.transform, floorEdgePosition);
                    }
                }
            }
        }

        /// <summary>
        /// Recursively checks for a script in the parent Object
        /// </summary>
        /// <param name="Object"></param>
        /// <param name="Edge"></param>
        public void CheckParent(Transform Object, FloorEdgePosition Edge)
        {
            CheckParent<FloorPlacedObject>(
                Object,
                Edge,
                (floorPlacedObject, data) =>
                {
                    var edge = (FloorEdgePosition)data;
                    if (placedObjectSO != null)
                    {
                        floorPlacedObject.PlaceEdge(edge.edge, placedObjectSO);
                    }
                }
            );
        }

        /// <summary>
        /// Generic recursive parent checker that accepts a component type and callback action
        /// </summary>
        /// <typeparam name="T">The component type to search for in parent objects</typeparam>
        /// <param name="currentObject">The starting Transform to check from</param>
        /// <param name="data">Optional data to pass to the callback function</param>
        /// <param name="onComponentFound">Callback action to execute when component is found</param>
        public void CheckParent<T>(Transform currentObject, object data, Action<T, object> onComponentFound) where T : Component
        {
            if (currentObject.parent != null && currentObject.parent.TryGetComponent(out T component))
            {
                // Found parent component of type T
                onComponentFound?.Invoke(component, data);
            }
            else
            {
                if (currentObject.parent != null)
                {
                    CheckParent<T>(currentObject.parent, data, onComponentFound);
                }
            }
        }

        /// <summary>
        /// Places an object in a PlacementZone based on the provided parameters.
        /// </summary>
        /// <param name="placedObjectSO">The Scriptable Object containing data about the object to be placed.</param>
        /// <param name="useMousePosition">If true, uses the mouse position to determine placement. If false, uses the provided grid position.</param>
        /// <param name="mousePos">The position of the mouse (used if useMousePosition is true).</param>
        /// <param name="isEditor">If this is called In Editor or runtime.</param>
        /// <param name="gridPosition">The grid position of the object (used if useMousePosition is false).</param>
        public void PlaceZoneObject(PlacedObjectSO placedObjectSO, bool useMousePosition, Vector3 mousePos, bool isEditor, Vector2Int gridPosition = default)
        {
            // Validate the Scriptable Object
            if (placedObjectSO == null)
            {
                Debug.LogError("PlacedObjectSO is null. Cannot place object.");
                return;
            }

            // Find the PlacementZone based on the placement method
            PlacementZone placementZone = null;
            if (useMousePosition)
            {
                // Use mouse position to find the PlacementZone
                Ray ray = Camera.main.ScreenPointToRay(mousePos);
                if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f))
                {
                    // Try getting component directly
                    if (raycastHit.collider.TryGetComponent(out placementZone))
                    {
                        Debug.Log("PlacementZone found at mouse position.");
                    }
                    // If not found, check children
                    else if (raycastHit.collider.GetComponentInChildren<PlacementZone>() is PlacementZone childZone)
                    {
                        placementZone = childZone;
                        Debug.Log("PlacementZone found in children.");
                    }
                    // If still not found, check parents
                    else if (raycastHit.collider.GetComponentInParent<PlacementZone>() is PlacementZone parentZone)
                    {
                        placementZone = parentZone;
                        Debug.Log("PlacementZone found in parent.");
                    }
                }
            }
            else
            {
                // Use grid position to find the PlacementZone
                placementZone = FindPlacementZoneAtGridPosition(gridPosition);
                if (placementZone != null)
                {
                    Debug.Log("PlacementZone found at grid position.");
                }
            }

            // If no PlacementZone is found, log a warning and exit
            if (placementZone == null)
            {
                Debug.LogWarning("No valid PlacementZone found.");
                return;
            }


            // Instantiate the object to be placed
            GameObject objectToPlace = Instantiate(placedObjectSO.Prefab.gameObject);
            if (objectToPlace == null)
            {
                Debug.LogError("Failed to instantiate object from PlacedObjectSO.");
                return;
            }


            // Check if the object can be placed in the zone
            if (!placementZone.CanPlaceObject(objectToPlace))
            {
                Debug.LogWarning("Object cannot be placed in the selected PlacementZone.");
                if (isEditor) DestroyImmediate(objectToPlace); else Destroy(objectToPlace);
                return;
            }

            // Place the object in the zone
            placementZone.PlaceObject(objectToPlace);

        }

        /// <summary>
        /// Finds a PlacementZone at the specified grid position.
        /// </summary>
        /// <param name="gridPosition">The grid position to search for a PlacementZone.</param>
        /// <returns>The PlacementZone at the grid position, or null if none is found.</returns>
        private PlacementZone FindPlacementZoneAtGridPosition(Vector2Int gridPosition)
        {
            return null;
        }

        /// <summary>
        /// Function used to Place a Loose Object
        /// </summary>
        /// <param name="placedObjectSO">The Scriptable Object with Data on the Object to be Placed</param>
        /// <param name="useMousePosition">If a mouse position or a position given should be used to place the object</param>
        /// <param name="mousePos">The position of the Mouse</param>
        /// <param name="orientation">The orientation of the object being placed</param>
        /// <param name="position">The position of the object if not using a mouse position</param>
        public void PlaceLooseObject(PlacedObjectSO placedObjectSO, bool useMousePosition, Vector3 mousePos, Vector3 orientation = new Vector3(), Vector3 position = default)
        {
            if (!useMousePosition)
            {
                GameObject originalObject = placedObjectSO.Prefab.GetComponent<ChildHolder>().OriginalMesh;
                placedObjectSO.Prefab.GetComponent<ChildHolder>()?.SetSO(placedObjectSO);
                // Calculate the rotation needed to align the Y-axis with the target direction
                Quaternion targetRotation = Quaternion.LookRotation(orientation, Vector3.up);

                // Apply the rotation to the object
                originalObject.transform.rotation = targetRotation * originalObject.transform.rotation;

                Transform looseObjectTransform = Instantiate(placedObjectSO.Prefab, position, Quaternion.Euler(0, looseObjectEulerY, 0));
                looseObjectTransform.gameObject.layer = looseObjectLayer;
                if (Application.isPlaying) DeselectObjectType();
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit raycastHit))
            {
                if (InputController.GetLeftMouseButton())
                {
                    float yAxis = (currentGridIndex <= 0) ? 0 : gridManager.GridYSize * currentGridIndex;
                    Vector3 _position = new Vector3(raycastHit.point.x, yAxis, raycastHit.point.z);
                    Transform looseObjectTransform = Instantiate(placedObjectSO.Prefab, _position, Quaternion.Euler(0, looseObjectEulerY, 0));
                    placedObjectSO.Prefab.GetComponent<ChildHolder>()?.SetSO(placedObjectSO);
                    //Saving System
                    //looseObjectTransformList.Add(looseObjectTransform);
                }

                if (Application.isPlaying) DeselectObjectType();
            }
        }

        /// <summary>
        /// Places a Grid Objecton the Grid
        /// </summary>
        /// <param name="placedObjectSO">The Scriptable Object with Data on the Object to be Placed</param>
        /// <param name="useMousePosition">if the mouse positions should be use</param>
        /// <param name="mousePos">The mouse position to use</param>
        /// <param name="positions">If mouse positions shouldn't be used, the X and Z positions to be used</param>
        public void PlaceGridObject(PlacedObjectSO placedObjectSO, bool useMousePosition, Vector3 mousePos, Vector2Int positions = default)
        {
            int x;
            int z;

            if (useMousePosition)
            {
                grid.GetXZ(Utilities.GetMouseWorldPosition(mouseMask, mousePos), out int x1, out int z1);
                x = x1;
                z = z1;
            }
            else
            {
                x = positions.x;
                z = positions.y;
            }

            Vector2Int placedObjectOrigin = new Vector2Int(x, z);
            placedObjectOrigin = grid.ValidateGridPosition(placedObjectOrigin);

            List<Vector2Int> gridPositions = placedObjectSO.GetGridPosition(placedObjectOrigin, currentDir);

            if (IsGridEmpty(gridPositions, placedObjectSO.placedObjectType))
            {
                Vector2Int rotationOffset = placedObjectSO.GetRotationOffset(currentDir);
                floorYoffset = (placedObjectSO.placedObjectType == PlacedObjectType.FloorObject) ? floorYoffset = transform.localPosition.y + gridManager.FloorYOffset : transform.localPosition.y;

                Vector3 placedWorldPos = grid.GetWorldPosition(placedObjectOrigin.x, placedObjectOrigin.y) + new Vector3(rotationOffset.x, floorYoffset / gridManager.CellSize, rotationOffset.y) * gridManager.CellSize;
                PlacedObject placedObject = PlacedObject.Create(placedObjectSO, placedWorldPos, currentDir, placedObjectOrigin);

                foreach (var position in gridPositions) grid.GetGridObject(position.x, position.y).SetPlacedObject(placedObject);
                OnObjectPlaced?.Invoke(this, EventArgs.Empty);

                if (Application.isPlaying) DeselectObjectType();
            }
            else
            {
                if (Application.isPlaying) UtilsClass.CreateWorldTextPopup("Cannot build here!", Utilities.GetMouseWorldPosition(mouseMask, mousePos));
                Debug.LogWarning("Cannot Build Here");
            }
        }

        /// <summary>
        /// Checks if the Grid Object is empty
        /// </summary>
        /// <param name="gridPositions">The List of positions to check</param>
        /// <returns>true if it is empty</returns>
        public bool IsGridEmpty(List<Vector2Int> gridPositions, PlacedObjectType placedObjectType)
        {
            foreach (Vector2Int position in gridPositions)
            {
                var gridObject = grid.GetGridObject(position.x, position.y);

                // If the grid cell is occupied or cannot accept the object type, return false
                if (!gridObject.CanBuild(placedObjectType))
                {
                    return false;
                }

                // Check constraints
                if (!CheckPlacementConstraints(position, placedObjectSO))
                {
                    return false;
                }
            }


            return true;
        }

        /// <summary>
        /// Checks if the placement of an object at a given position respects the placement constraints
        /// relative to its adjacent grid positions (left, right, up, down).
        /// </summary>
        /// <param name="position">The position where the object is being placed.</param>
        /// <param name="placedObjectSO">The ScriptableObject containing the details of the object being placed.</param>
        /// <returns>True if the placement respects all constraints; false if any constraint is violated.</returns>

        private bool CheckPlacementConstraints(Vector2Int position, PlacedObjectSO placedObjectSO)
        {
            Vector2Int[] adjacentPositions = new Vector2Int[]
            {
                new Vector2Int(position.x + 1, position.y), // Right
                new Vector2Int(position.x - 1, position.y), // Left
                new Vector2Int(position.x, position.y + 1), // Up
                new Vector2Int(position.x, position.y - 1)  // Down
            };

            foreach (var adjacentPosition in adjacentPositions)
            {
                if (!grid.B_ValidateGridPosition(adjacentPosition)) continue; // Skip if out of bounds

                var adjacentObject = grid.GetGridObject(adjacentPosition.x, adjacentPosition.y)?.GetPlacedObject();
                if (adjacentObject == null) continue; // If empty, no constraint issues

                // Check if this object violates the constraints of the new object
                if (!CheckPlacementConstraintLocal(placedObjectSO, adjacentObject.PlacedObjectSOValue))
                {
                    return false; // Constraint violated
                }
            }

            return true;
        }

        /// <summary>
        /// Checks if the placement of an object respects the constraints relative to an adjacent object.
        /// </summary>
        /// <param name="objectToPlace">The object that is being placed.</param>
        /// <param name="adjacentObject">The existing adjacent object to check constraints against.</param>
        /// <returns>True if the placement is valid, false otherwise.</returns>
        private bool CheckPlacementConstraintLocal(PlacedObjectSO objectToPlace, PlacedObjectSO adjacentObject)
        {
            if (objectToPlace == null || adjacentObject == null) return false;

            if (!objectToPlace.HasConstraints) return false;

            if (!adjacentObject.HasConstraints) return false;

            // Check ALL rules from BOTH objects
            foreach (var rule in objectToPlace.constraintRules)
            {
                if (!rule.ValidatePlacement(objectToPlace, adjacentObject))
                    return false; // Fail if any rule is not met
            }

            foreach (var rule in adjacentObject.constraintRules)
            {
                if (!rule.ValidatePlacement(adjacentObject, objectToPlace))
                    return false;
            }

            return true; // Placement is valid if all rules pass
        }


        #endregion

        #region Delete Objects
        /// <summary>
        /// Deletes the Object currently clicked on
        /// </summary>
        /// <param name="_placedObjectSO">The removed object</param>
        /// <param name="mousePos">The position of the mouse</param>
        private void HandleDeleteObject(out PlacedObjectSO _placedObjectSO, Vector3 mousePos)
        {
            _placedObjectSO = null;
            if (Camera.main == null)
            {
                Debug.LogError("Main camera not found.");
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f))
            {
                ChildHolder childHolder = SearchParent<ChildHolder>(raycastHit.collider.transform);

                if (childHolder != null)
                {
                    _placedObjectSO = childHolder._placedObjectSO;
                    PlacedObjectType placedType = childHolder._placedObjectType;
                    if (!FilterPlacedObjectTypes(placedType)) return;
                    switch (childHolder._placedObjectType)
                    {
                        case PlacedObjectType.FloorObject:
                        case PlacedObjectType.GridObject:
                            RemoveGridObject(mousePos);
                            break;

                        case PlacedObjectType.WallObject:
                            RemoveWallObject(mousePos);
                            break;

                        case PlacedObjectType.LooseObject:
                            RemoveLooseObject(mousePos);
                            break;
                    }
                }
            }
        }

        public void RemoveWallObject(Vector3 mousePos)
        {
            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, edgeMask))
            {
                // Raycast Hit Edge Object
                if (raycastHit.collider.TryGetComponent(out FloorEdgeObject floorEdgeObject))
                {
                    CheckParentRemove(raycastHit.collider.transform, floorEdgeObject);
                }
            }
        }

        public void CheckParentRemove(Transform Object, FloorEdgeObject _object)
        {
            if (Object.parent.TryGetComponent(out FloorEdgeObject floorEdgeObject))
            {
                // Found parent FloorPlacedObject
                if (placedObjectSO != null)
                {
                    FloorPlacedObject floorPlacedObject = _object.GetFloor();

                    // Remove Object on Edge
                    if (floorPlacedObject != null) floorPlacedObject.RemoveEdge(_object.CurrentEdge, _object.gameObject.transform);
                }
            }
            else
            {
                if (Object.parent != null)
                {
                    CheckParentRemove(Object.parent, _object);
                }
            }
        }

        /// <summary>
        /// Search through parents for a script
        /// </summary>
        /// <typeparam name="T">The script</typeparam>
        /// <param name="Object">Object to start from</param>
        /// <returns></returns>
        public T SearchParent<T>(Transform Object) where T : MonoBehaviour
        {
            if (Object == null) return null;

            if (Object.TryGetComponent(out T script))
            {
                return script;
            }
            else if (Object.parent != null)
            {
                return SearchParent<T>(Object.parent);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Removes the Current Object clicked on
        /// </summary>
        /// <param name="mousePos">The position of the mouse</param>
        public void RemoveGridObject(Vector3 mousePos)
        {
            GridObject gridObject = grid.GetGridObject(Utilities.GetMouseWorldPosition(mouseMask, mousePos));

            if (gridObject != null)
            {
                //Get Placed Object
                PlacedObject placedObject2 = gridObject.GetPlacedObject();

                if (placedObject2)
                {
                    List<Vector2Int> gridPositions = placedObject2.GetGridPositionList();
                    //Clear their Transform
                    foreach (var position in gridPositions)
                    {
                        grid.GetGridObject(position.x, position.y).ClearPlacedObject();
                    }

                    placedObject2.DestroyGameObject();
                }
                else
                {
                    UtilsClass.CreateWorldTextPopup("Nothing here to Delete!", Utilities.GetMouseWorldPosition(mouseMask, mousePos));
                }
            }
        }

        /// <summary>
        /// Removes a Loose Object
        /// </summary>
        /// <param name="mousePos">The position of the mouse</param>
        private void RemoveLooseObject(Vector3 mousePos)
        {
            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit raycastHit))
            {
                if (raycastHit.collider.gameObject.layer == looseObjectLayer)
                {
                    DestroyHierarchy(raycastHit.collider.gameObject.transform);
                }
                else
                {
                    UtilsClass.CreateWorldTextPopup("No Loose Object Here!", Utilities.GetMouseWorldPosition(mouseMask, mousePos));
                }
            }
        }

        /// <summary>
        /// Recursively destorys an object and all its parents
        /// </summary>
        /// <param name="obj">The object</param>
        public void DestroyHierarchy(Transform obj)
        {
            if (obj == null)
            {
                return;
            }

            // Destroy the current object
            Destroy(obj.gameObject);

            // Recursively destroy the parent
            DestroyHierarchy(obj.parent);
        }
        #endregion

        private void HandleEditObject(Vector3 mousePos)
        {
            HandleDeleteObject(out PlacedObjectSO _editSO, mousePos);

            if (!FilterPlacedObjectTypes(_editSO.placedObjectType)) return;
            placedObjectSO = _editSO;
            RefreshSelectedObjectType();
        }

        /// <summary>
        /// Changes the Grid Layer that placement is done on.
        /// </summary>
        public void HandleGridSelect()
        {
            if (placedObjectSO && placedObjectSO.placedObjectType == PlacedObjectType.GridObject) return;
            int nextSelectedGridIndex = (gridList.IndexOf(grid) + 1) % gridList.Count;
            currentGridIndex = nextSelectedGridIndex;
            grid = gridList[nextSelectedGridIndex];
            OnActiveGridLevelChanged?.Invoke(this, EventArgs.Empty);
        }

        /// <summary>
        /// Checks if that current Filter Object is activated
        /// </summary>
        /// <param name="currentPlacedObjecType"></param>
        /// <returns></returns>
        public bool FilterPlacedObjectTypes(PlacedObjectType currentPlacedObjecType)
        {
            bool useful = filterType[currentPlacedObjecType];
            return useful;
        }

        public void RotateGridObject()
        {
            if (placedObjectSO == null) return;
            if (placedObjectSO.placedObjectType == PlacedObjectType.LooseObject)
            {
                looseObjectEulerY += 90f;
            }
            else
            {
                currentDir = placedObjectSO.GetNextDir(currentDir);
            };
        }

        /// <summary>
        /// Gets the current mouse position snapped to the grid
        /// </summary>
        public Vector3 GetMouseWorldSnappedPosition()
        {
            Vector3 mousePosition = Utilities.GetMouseWorldPosition(mouseMask, InputController.GetMousePosition());
            grid.GetXZ(mousePosition, out int x, out int z);

            if (placedObjectSO != null)
            {
                Vector2Int rotationOffset = placedObjectSO.GetRotationOffset(currentDir);
                Vector3 placedObjectWorldPosition = grid.GetWorldPosition(x, z) + new Vector3(rotationOffset.x, 0, rotationOffset.y) * gridManager.CellSize;
                return placedObjectWorldPosition;
            }
            else
            {
                return mousePosition;
            }
        }

        /// <summary>
        /// Deselects the Current Object that was selected
        /// </summary>
        private void DeselectObjectType()
        {
            placedObjectSO = null;
            RefreshSelectedObjectType();
        }

        private void RefreshSelectedObjectType()
        {
            OnSelectedChanged?.Invoke(this, EventArgs.Empty);
            //SetEnabledScript();
        }

        /// <summary>
        /// Gets the rotation for the currently selected object
        /// </summary>
        public Quaternion GetPlacedObjectRotation()
        {
            if (placedObjectSO != null)
            {
                return Quaternion.Euler(0, placedObjectSO.GetRotationAngle(currentDir), 0);
            }
            else
            {
                return Quaternion.identity;
            }
        }

        public PlacedObjectSO GetPlacedObjectTypeSO()
        {
            return placedObjectSO;
        }

        public float GetLooseObjectEulerY()
        {
            return looseObjectEulerY;
        }

        public int GetGridIndex()
        {
            return currentGridIndex;
        }

        /// <summary>
        /// Gets the FloorEdge the mouse is clicking on
        /// </summary>
        /// <returns>The Floor Edge Position the mouse is clicking on</returns>
        public FloorEdgePosition GetMouseFloorEdgePosition()
        {
            Ray ray = Camera.main.ScreenPointToRay(InputController.GetMousePosition());
            if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, edgeMask))
            {
                // Raycast Hit Edge Object
                if (raycastHit.collider.TryGetComponent(out FloorEdgePosition floorEdgePosition))
                {
                    return floorEdgePosition;
                }
            }

            return null;
        }
    }
}



