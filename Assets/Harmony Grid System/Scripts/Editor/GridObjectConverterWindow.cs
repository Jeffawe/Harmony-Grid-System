using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using HarmonyGridSystem.Grid;
using HarmonyGridSystem.Objects;

namespace HarmonyGridSystem.Utils
{
    public class GridObjectConverterWindow : EditorWindow
    {
        private const string SETTINGS_PATH = "Assets/Editor/GridObjectConverterSettings.asset";
        private const string WINDOW_TITLE = "Grid Converter";
        private const float MIN_WINDOW_WIDTH = 400f;
        private const float MIN_WINDOW_HEIGHT = 500f;

        [MenuItem("Tools/Harmony Grid System/Convert Objects", priority = 100)]
        public static void ShowWindow()
        {
            var window = GetWindow<GridObjectConverterWindow>();
            window.titleContent = new GUIContent(WINDOW_TITLE);
            window.minSize = new Vector2(MIN_WINDOW_WIDTH, MIN_WINDOW_HEIGHT);
        }

        #region Private Variables
        private SerializedObject _serializedObject;
        private GridObjectConverterSettings _settings;
        private Vector2 _scrollPosition;
        #endregion

        #region Unity Callbacks
        private void OnEnable()
        {
            LoadOrCreateSettings();
            _serializedObject = new SerializedObject(_settings);
            ValidateSelectedObjects();
        }

        private void OnGUI()
        {
            _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
            _serializedObject.Update();

            DrawHeader();
            DrawMainSettings();
            DrawTypeSpecificSettings();
            DrawValidationSection();
            DrawConversionControls();

            _serializedObject.ApplyModifiedProperties();
            EditorGUILayout.EndScrollView();
        }

        private void OnSelectionChange() => ValidateSelectedObjects();
        #endregion

        #region Core Functionality
        private void ConvertSelectedObjects()
        {
            if (!ValidateConversion()) return;

            try
            {
                EditorUtility.DisplayProgressBar("Converting Objects", "Starting conversion...", 0);
                float progressPerObject = 1f / Selection.gameObjects.Length;

                for (int i = 0; i < Selection.gameObjects.Length; i++)
                {
                    var go = Selection.gameObjects[i];
                    EditorUtility.DisplayProgressBar("Converting Objects",
                        $"Processing {go.name} ({i + 1}/{Selection.gameObjects.Length})",
                        i * progressPerObject);

                    using (var tempParent = new TemporarySceneParent())
                    {
                        var instance = PrefabUtility.InstantiatePrefab(go) as GameObject;
                        if (instance == null) continue;

                        instance.transform.SetParent(tempParent.Transform);
                        ProcessObject(instance, go.name);
                    }
                }
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }
        }

        private void ProcessObject(GameObject target, string originalName)
        {
            try
            {
                switch (_settings.objectType)
                {
                    case PlacedObjectType.GridObject:
                        CreateGridObject(target, originalName);
                        break;
                    case PlacedObjectType.FloorObject:
                        CreateFloorObject(target, originalName);
                        break;
                    case PlacedObjectType.WallObject:
                        CreateWallObject(target, originalName);
                        break;
                    case PlacedObjectType.LooseObject:
                        CreateLooseObject(target, originalName);
                        break;
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to process {originalName}: {e}");
            }
        }
        #endregion

        #region Object Creation Methods
        private void CreateGridObject(GameObject target, string name)
        {
            var parent = CreateBaseObject(target, name, new List<System.Type> { typeof(PlacedObject) }, out PlacedObjectSO placedObjectSO);

            GeneratePrefab(parent.gameObject, placedObjectSO);
        }

        private void CreateFloorObject(GameObject target, string name)
        {
            var parent = CreateBaseObject(target, name, new List<System.Type> { typeof(FloorPlacedObject) }, out PlacedObjectSO placedObjectSO);
            var bounds = GetRenderBounds(target);

            // Edge markers
            CreateEdgePositions(parent, bounds);
            GeneratePrefab(parent.gameObject, placedObjectSO);
        }

        private void CreateWallObject(GameObject target, string name)
        {
            var parent = CreateBaseObject(target, name, new List<System.Type> { typeof(FloorEdgeObject) }, out PlacedObjectSO placedObjectSO, true);

            GeneratePrefab(parent.gameObject, placedObjectSO);
        }

        private void CreateLooseObject(GameObject target, string name)
        {
            var parent = CreateBaseObject(target, name, new List<System.Type> { typeof(PlacedObject) }, out PlacedObjectSO placedObjectSO);
            SetLayerRecursively(parent.gameObject, _settings.looseObjectLayer);

            GeneratePrefab(parent.gameObject, placedObjectSO);
        }

        private Transform CreateBaseObject(GameObject target, string name, List<System.Type> components, out PlacedObjectSO placedObject, bool isWall = false)
        {
            var parent = new GameObject($"{name}_{_settings.objectType}");
            parent.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            target.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var bounds = GetRenderBounds(target);
            int width = Mathf.CeilToInt(bounds.size.x);
            int depth = Mathf.CeilToInt(bounds.size.z);
            int gridCellSize = _settings.gridCellSize;
            int occupiedGridCellsX = (Mathf.RoundToInt(width / gridCellSize) == 0) ? 1 : Mathf.RoundToInt(width / gridCellSize);
            int occupiedGridCellsZ = (Mathf.RoundToInt(depth / gridCellSize) == 0) ? 1 : Mathf.RoundToInt(depth / gridCellSize);

            //Vector3 offset = new Vector3(parent.transform.localScale.x / 2, 0, parent.transform.localScale.z / 2);

            // Calculate grid-based position
            float positionX = isWall ? 0 : gridCellSize * (occupiedGridCellsX - 1);
            float positionZ = isWall ? 0 : gridCellSize * (occupiedGridCellsZ - 1);

            // Calculate pivot point offset
            Vector3 pivotPoint = SetPivotPoint(_settings.pivotPoint, occupiedGridCellsX, occupiedGridCellsZ);

            // Setup child holder
            ChildHolder childHolder = parent.AddComponent<ChildHolder>();
            childHolder.SetChild(target);
            childHolder.SetPlacedObjectType(_settings.objectType);

            // Position the parent object at the pivot point
            parent.transform.position = target.transform.position - pivotPoint;
            target.transform.position = parent.transform.position + pivotPoint + new Vector3(0, _settings.yOffset, 0);
            //target.transform.position = target.transform.position + new Vector3(positionX / 2, 0 + _settings.yOffset, positionZ / 2);
            //parent.transform.position = target.transform.position + pivotPoint - offset;
            target.transform.SetParent(parent.transform, true);
            parent.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);

            bool isVisual = false;
            if (_settings.createVisualPrefab)
            {
                var visual = Instantiate(target, parent.transform, true);
                visual.name = "Visual";
                visual.transform.position = parent.transform.position + pivotPoint + new Vector3(0, _settings.yOffset, 0);
                if (_settings.visualMaterial) visual.GetComponent<Renderer>().sharedMaterial = _settings.visualMaterial;
                isVisual = true;
            }

            if (_settings.createGridVisualization && _settings.objectType != PlacedObjectType.WallObject)
            {
                CreateGridCubes(parent.transform, target.transform, bounds, occupiedGridCellsX, occupiedGridCellsZ, out Transform transforms);
                childHolder.CreateCube(transforms);
            }

            placedObject = null;
            if (_settings.generateScriptableObject)
            {
                placedObject = GenerateSO(parent, new Vector2Int(occupiedGridCellsX, occupiedGridCellsZ), isVisual);
                childHolder.SetSO(placedObject);
            }
            else
            {
                childHolder.SetSOValues(occupiedGridCellsX, occupiedGridCellsZ, target.name, isVisual, _settings.prefabSavePath, _settings.objectType);
            }

            components.ForEach(c => parent.AddComponent(c));

            return parent.transform;
        }


        #endregion

        #region Helper Methods
        private Bounds GetRenderBounds(GameObject target)
        {
            var renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return new Bounds();

            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            return bounds;
        }

        private Bounds GetMeshBounds(GameObject target)
        {
            Mesh meshBounds = target.GetComponent<MeshFilter>().sharedMesh;
            return meshBounds.bounds;

        }

        private PlacedObjectSO GenerateSO(GameObject target, Vector2Int occupiedGridCells, bool isVisual)
        {
            PlacedObjectSO building = Utilities.CreateNewScriptableObject<PlacedObjectSO>(target.name);

            building.width = occupiedGridCells.x;
            building.height = occupiedGridCells.y;
            building.nameString = target.name;
            building.PrefabPath = _settings.prefabSavePath;
            building.placedObjectType = _settings.objectType;
            building.hasVisual = isVisual;
            Utilities.CreatePrefab(target, _settings.prefabSavePath, target.name, true);

            return building;
        }

        private void GeneratePrefab(GameObject parent, PlacedObjectSO objectSO)
        {
            if (_settings.generatePrefab)
            {
                Utilities.CreatePrefab(parent, _settings.prefabSavePath, parent.name, true);
                if (objectSO) objectSO.prefab = parent.transform;
            }

            if (objectSO) EditorUtility.SetDirty(objectSO);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private Vector3 SetPivotPoint(PivotPoint pivotPoint, int occupiedGridCellsX, int occupiedGridCellsZ)
        {
            int gridCellSize = _settings.gridCellSize;
            float halfGridCellSizeX = (occupiedGridCellsX * gridCellSize) / 2f;
            float halfGridCellSizeZ = (occupiedGridCellsZ * gridCellSize) / 2f;

            float pivotX;
            float pivotZ;

            switch (pivotPoint)
            {
                case PivotPoint.Center:
                    pivotX = 0;
                    pivotZ = 0;
                    break;

                case PivotPoint.TopRight:
                    pivotX = -halfGridCellSizeX;
                    pivotZ = -halfGridCellSizeZ;
                    break;

                case PivotPoint.TopLeft:
                    pivotX = +halfGridCellSizeX;
                    pivotZ = -halfGridCellSizeZ;
                    break;

                case PivotPoint.BottomRight:
                    pivotX = -halfGridCellSizeX;
                    pivotZ = +halfGridCellSizeZ;
                    break;

                case PivotPoint.BottomLeft:
                    pivotX = +halfGridCellSizeX;
                    pivotZ = +halfGridCellSizeZ;
                    break;

                default:
                    pivotX = 0f;
                    pivotZ = 0f;
                    break;
            }

            return new Vector3(pivotX, 0f, pivotZ);
        }

        private void CreateGridCubes(Transform parent, Transform target, Bounds bounds, int xCells, int zCells, out Transform transforms)
        {
            // Create a parent object for the grid cubes
            var gridParent = new GameObject("GridCubes").transform;
            gridParent.SetParent(parent);
            gridParent.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            gridParent.localScale = Vector3.one;
            transforms = gridParent.transform;

            // Calculate the starting position of the grid cubes
            Vector3 startPosition = target.position - new Vector3(
                bounds.size.x / 2f, // Half the object's width
                0f,
                bounds.size.z / 2f  // Half the object's depth
            );

            // Create grid cubes for each cell
            for (int x = 0; x < xCells; x++)
            {
                for (int z = 0; z < zCells; z++)
                {
                    // Calculate the position of the current grid cube
                    Vector3 cubePosition = startPosition + new Vector3(
                        x * _settings.gridCellSize + _settings.gridCellSize / 2f, // Center of the cell
                        0f,
                        z * _settings.gridCellSize + _settings.gridCellSize / 2f  // Center of the cell
                    );

                    // Instantiate the grid cube
                    var cube = Instantiate(_settings.cubePrefab);
                    if (cube == null) cube = GameObject.CreatePrimitive(PrimitiveType.Cube);

                    // Set the cube's scale to match the grid cell size
                    cube.transform.localScale = new Vector3(
                        _settings.gridCellSize,
                        0.1f, // Height of the grid cube (adjust as needed)
                        _settings.gridCellSize
                    );

                    // Position the cube
                    cube.transform.position = cubePosition;
                    cube.transform.forward = target.forward;

                    // Parent the cube to the grid parent
                    cube.transform.SetParent(gridParent, true);

                }
            }
        }

        private void CreateEdgePositions(Transform parent, Bounds bounds)
        {
            // Create a parent object for the edges
            var edges = new GameObject("EdgePositions").transform;
            edges.SetLocalPositionAndRotation(Vector3.zero, Quaternion.identity);
            edges.localScale = Vector3.one;
            edges.SetParent(parent, true);

            // Debug logs for bounds and parent position
            Debug.Log($"Bounds Size: {bounds.size}");
            Debug.Log($"Parent Local Position: {parent.GetComponent<ChildHolder>().OriginalMesh.transform.localPosition}");

            // List to store edge positions
            List<FloorEdgePosition> edgePositionsList = new List<FloorEdgePosition>();

            // Calculate half sizes for positioning
            float halfWidth = bounds.size.x / 2f;
            float halfDepth = bounds.size.z / 2f;
            Transform targetObject = parent.GetComponent<ChildHolder>().OriginalMesh.transform;
            if (targetObject == null) Debug.LogError("No ChildHolder script or Cannot fild child element");

            // Loop through 4 edges (one for each side)
            for (int i = 0; i < 4; i++)
            {
                // Create a cube for the edge
                var edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                edge.name = $"Edge_{((EdgeType)i).ToString()}";
                edge.transform.SetParent(parent);
                edge.GetComponent<MeshRenderer>().enabled = false;
                edge.layer = _settings.edgeLayer;

                // Calculate position based on edge type
                Vector3 edgePosition = Vector3.zero;
                switch ((EdgeType)i)
                {
                    case EdgeType.Left:
                        edgePosition = new Vector3(targetObject.localPosition.x, 0f, targetObject.localPosition.z + halfDepth);
                        edge.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                        break;
                    case EdgeType.Right:
                        edgePosition = new Vector3(targetObject.localPosition.x, 0f, targetObject.localPosition.z - halfDepth);
                        edge.transform.localRotation = Quaternion.Euler(0f, 90f, 0f);
                        break;
                    case EdgeType.Up:
                        edgePosition = new Vector3(targetObject.localPosition.x - halfWidth, 0f, targetObject.localPosition.z);
                        edge.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                        break;
                    case EdgeType.Down:
                        edgePosition = new Vector3(targetObject.localPosition.x + halfWidth, 0f, targetObject.localPosition.z);
                        edge.transform.localRotation = Quaternion.Euler(0f, 0f, 0f);
                        break;
                }

                // Set edge position
                edge.transform.localPosition = edgePosition;

                // Set edge scale
                edge.transform.localScale = new Vector3(
                    _settings.edgeSize.x,
                    _settings.edgeSize.y,
                    (i < 2) ? bounds.size.z : bounds.size.x // Adjust scale based on edge type
                );

                // Add FloorEdgePosition component and set edge type
                var floorEdgePosition = edge.AddComponent<FloorEdgePosition>();
                floorEdgePosition.SetEdge((EdgeType)i);
                edgePositionsList.Add(floorEdgePosition);
            }

            // Scale adjustment for the parent object (if needed)
            if (parent.GetComponent<MeshFilter>() != null)
            {
                var mesh = parent.GetComponent<MeshFilter>().sharedMesh;
                parent.localScale = new Vector3(
                    _settings.gridCellSize / Mathf.RoundToInt(mesh.bounds.size.x),
                    parent.localScale.y,
                    _settings.gridCellSize / Mathf.RoundToInt(mesh.bounds.size.z)
                );
            }

            // Assign edge positions to the FloorPlacedObject component
            FloorPlacedObject floorPlacedObject = parent.GetComponent<FloorPlacedObject>();
            floorPlacedObject.SetEdgePositions(
                edgePositionsList[0], // Left
                edgePositionsList[2], // Right
                edgePositionsList[1], // Front
                edgePositionsList[3]  // Back
            );
        }

        private void SetLayerRecursively(GameObject obj, int layer)
        {
            obj.layer = layer;
            foreach (Transform child in obj.transform)
                SetLayerRecursively(child.gameObject, layer);
        }
        #endregion

        #region UI Drawing
        private void DrawHeader()
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Harmony Grid Converter", EditorStyles.whiteLargeLabel);
            EditorGUILayout.Space();
        }

        private void DrawMainSettings()
        {
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("objectType"));
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("gridCellSize"));
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("pivotPoint"));

            var generatePrefabProp = _serializedObject.FindProperty("generatePrefab");
            EditorGUILayout.PropertyField(generatePrefabProp);

            if (generatePrefabProp.boolValue)
            {
                EditorGUILayout.PropertyField(_serializedObject.FindProperty("prefabSavePath"));
            }

            EditorGUILayout.PropertyField(_serializedObject.FindProperty("generateScriptableObject"));
            EditorGUILayout.Space();
        }

        private void DrawTypeSpecificSettings()
        {
            EditorGUILayout.LabelField("Type Settings", EditorStyles.boldLabel);

            switch (_settings.objectType)
            {
                case PlacedObjectType.WallObject:
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty("edgeLayer"));
                    break;

                case PlacedObjectType.FloorObject:
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty("edgeSize"));
                    break;

                case PlacedObjectType.LooseObject:
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty("looseObjectLayer"));
                    break;
            }

            EditorGUILayout.Space();

            var generateVisualPrefabProp = _serializedObject.FindProperty("createVisualPrefab");
            EditorGUILayout.PropertyField(generateVisualPrefabProp);

            if (generateVisualPrefabProp.boolValue)
            {
                EditorGUILayout.PropertyField(_serializedObject.FindProperty("visualMaterial"));
            }

            EditorGUILayout.PropertyField(_serializedObject.FindProperty("yOffset"));


            var generatePrefabProp = _serializedObject.FindProperty("createGridVisualization");
            EditorGUILayout.PropertyField(generatePrefabProp);

            if (generatePrefabProp.boolValue)
            {
                EditorGUILayout.PropertyField(_serializedObject.FindProperty("cubePrefab"));
            }

            EditorGUILayout.Space();
        }

        private void DrawValidationSection()
        {
            EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);

            if (Selection.gameObjects.Length == 0)
            {
                EditorGUILayout.HelpBox("Select objects to validate", MessageType.Info);
                return;
            }

            if (_settings.validationResults.errors.Count > 0)
            {
                EditorGUILayout.HelpBox(string.Join("\n", _settings.validationResults.errors),
                    MessageType.Error);
            }

            if (_settings.validationResults.warnings.Count > 0)
            {
                EditorGUILayout.HelpBox(string.Join("\n", _settings.validationResults.warnings),
                    MessageType.Warning);
            }

            if (!string.IsNullOrEmpty(_settings.validationResults.recommendedGridSize))
            {
                EditorGUILayout.HelpBox(_settings.validationResults.recommendedGridSize,
                    MessageType.Info);

                if (GUILayout.Button("Apply Recommended Grid Size"))
                    ApplyRecommendedGridSize();
            }

            EditorGUILayout.Space();
        }

        private void DrawConversionControls()
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Validate Selection", GUILayout.Width(120)))
                ValidateSelectedObjects();

            if (GUILayout.Button("Convert Objects", GUILayout.Width(120)))
                ConvertSelectedObjects();

            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.HelpBox(
                "Select objects in scene or project view to convert them to grid-ready prefabs.\n" +
                "Multiple selection supported for batch processing.",
                MessageType.Info);
        }
        #endregion

        #region Validation System
        private void ValidateSelectedObjects()
        {
            _settings.validationResults = new GridValidationResults();
            if (Selection.gameObjects.Length == 0) return;

            var analysis = new GridObjectAnalysis();
            foreach (var go in Selection.gameObjects)
            {
                try
                {
                    var bounds = GetPrefabBounds(go);
                    analysis.AddObjectBounds(_settings.objectType, bounds.size);
                }
                catch (System.Exception e)
                {
                    _settings.validationResults.errors.Add($"Failed to analyze {go.name}: {e.Message}");
                }
            }

            ProcessAnalysis(analysis);
            Repaint();
        }

        private Bounds GetPrefabBounds(GameObject prefab)
        {
            // Implementation that works with both scene instances and prefab assets
            if (prefab.scene.IsValid()) // Scene object
            {
                return GetRenderBounds(prefab);
            }
            else // Prefab asset
            {
                using (var tempParent = new TemporarySceneParent())
                {
                    var instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
                    instance.transform.SetParent(tempParent.Transform);
                    return GetRenderBounds(instance);
                }
            }
        }


        private void ProcessAnalysis(GridObjectAnalysis analysis)
        {
            var results = _settings.validationResults;
            results.isValid = true;

            // Type-specific validation
            switch (_settings.objectType)
            {
                case PlacedObjectType.WallObject:
                    ValidateWalls(analysis, results);
                    break;
                case PlacedObjectType.FloorObject:
                    ValidateFloors(analysis, results);
                    break;
                default:
                    ValidateGeneric(analysis, results);
                    break;
            }

            // Grid recommendations
            results.recommendedGridSize = analysis.GetRecommendedGridSize(_settings.objectType);
        }

        private void ValidateWalls(GridObjectAnalysis analysis, GridValidationResults results)
        {
            if (analysis.HasWidthVariance())
                results.errors.Add("Walls must have consistent widths!");

            if (analysis.maxDepth > _settings.gridCellSize)
                results.warnings.Add("Some walls are thicker than grid cell size");
        }

        private void ValidateFloors(GridObjectAnalysis analysis, GridValidationResults results)
        {
            if (!Mathf.Approximately(analysis.minWidth, analysis.maxWidth) ||
                !Mathf.Approximately(analysis.minDepth, analysis.maxDepth))
                results.warnings.Add("Floor sizes vary - may cause alignment issues");
        }

        private void ValidateGeneric(GridObjectAnalysis analysis, GridValidationResults results)
        {
            if (analysis.HasWidthVariance())
                results.warnings.Add($"Width variance: {analysis.minWidth:0.0}-{analysis.maxWidth:0.0}");

            if (analysis.HasDepthVariance())
                results.warnings.Add($"Depth variance: {analysis.minDepth:0.0}-{analysis.maxDepth:0.0}");
        }
        #endregion

        #region Utility Methods
        private void LoadOrCreateSettings()
        {
            _settings = AssetDatabase.LoadAssetAtPath<GridObjectConverterSettings>(SETTINGS_PATH) ??
                       CreateInstance<GridObjectConverterSettings>();

            if (!AssetDatabase.Contains(_settings))
            {
                Directory.CreateDirectory(Path.GetDirectoryName(SETTINGS_PATH));
                AssetDatabase.CreateAsset(_settings, SETTINGS_PATH);
                AssetDatabase.SaveAssets();
            }
        }

        private bool ValidateConversion()
        {
            if (Selection.gameObjects.Length == 0)
            {
                EditorUtility.DisplayDialog("Error", "No objects selected!", "OK");
                return false;
            }

            if (_settings.gridCellSize <= 0)
            {
                EditorUtility.DisplayDialog("Error", "Invalid grid cell size!", "OK");
                return false;
            }

            return true;
        }

        private void ApplyRecommendedGridSize()
        {
            if (float.TryParse(_settings.validationResults.recommendedGridSize.Split(':')[1], out float size))
                _settings.gridCellSize = Mathf.RoundToInt(size);
        }
        #endregion

        private class TemporarySceneParent : System.IDisposable
        {
            public Transform Transform { get; }

            public TemporarySceneParent()
            {
                var go = new GameObject("TEMP_CONVERSION_PARENT")
                {
                    hideFlags = HideFlags.HideAndDontSave
                };
                Transform = go.transform;
            }

            public void Dispose()
            {
                if (Transform != null)
                    DestroyImmediate(Transform.gameObject);
            }
        }
    }
}