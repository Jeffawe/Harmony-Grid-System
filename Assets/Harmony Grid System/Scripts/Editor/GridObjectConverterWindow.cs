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
            var parent = CreateBaseObject(target, name, new List<System.Type> { typeof(PlacedObject) });
            var bounds = GetRenderBounds(target);

            // Collider setup
            var collider = parent.AddComponent<BoxCollider>();
            collider.center = bounds.center;
            collider.size = bounds.size;

            parent.GetComponent<PlacedObject>().Initialize(
                Mathf.CeilToInt(bounds.size.x / _settings.gridCellSize),
                Mathf.CeilToInt(bounds.size.z / _settings.gridCellSize)
            );
        }

        private void CreateFloorObject(GameObject target, string name)
        {
            var parent = CreateBaseObject(target, name, new List<System.Type> { typeof(FloorPlacedObject) });
            var bounds = GetRenderBounds(target);

            // Edge markers
            CreateEdgePositions(parent.transform, bounds);
        }

        private void CreateWallObject(GameObject target, string name)
        {
            var parent = CreateBaseObject(target, name, new List<System.Type> { typeof(FloorEdgeObject) }, true);
            var bounds = GetRenderBounds(target);
        }

        private void CreateLooseObject(GameObject target, string name)
        {
            var parent = CreateBaseObject(target, name, new List<System.Type> { typeof(PlacedObject) });
            SetLayerRecursively(parent, _settings.looseObjectLayer);
        }

        private GameObject CreateBaseObject(GameObject target, string name, List<System.Type> components, bool isWall = false)
        {
            var parent = new GameObject($"{name}_{_settings.objectType}");
            var bounds = GetRenderBounds(target);
            Debug.Log(bounds.size);
            int width = Mathf.CeilToInt(bounds.size.x);
            int depth = Mathf.CeilToInt(bounds.size.z);
            int gridCellSize = _settings.gridCellSize;
            int occupiedGridCellsX = (Mathf.RoundToInt(width / gridCellSize) == 0) ? 1 : Mathf.RoundToInt(width / gridCellSize);
            int occupiedGridCellsZ = (Mathf.RoundToInt(depth / gridCellSize) == 0) ? 1 : Mathf.RoundToInt(depth / gridCellSize);

            Vector3 offset = new Vector3(parent.transform.localScale.x / 2, 0, parent.transform.localScale.z / 2);

            // Calculate grid-based position
            float positionX = isWall ? 0 : gridCellSize * (occupiedGridCellsX - 1);
            float positionZ = isWall ? 0 : gridCellSize * (occupiedGridCellsZ - 1);

            // Calculate pivot point offset
            Vector3 pivotPoint = SetPivotPoint(_settings.pivotPoint, occupiedGridCellsX, occupiedGridCellsZ, width, depth);

            // Setup child holder
            ChildHolder childHolder = parent.AddComponent<ChildHolder>();
            childHolder.SetChild(target);
            childHolder.SetPlacedObjectType(_settings.objectType);

            target.transform.position = target.transform.position + new Vector3(positionX / 2, 0 + _settings.yOffset, positionZ / 2);
            parent.transform.position = target.transform.position + pivotPoint - offset;
            target.transform.SetParent(parent.transform);

            // Visual part
            if (_settings.createVisualPrefab)
            {
                var visual = Instantiate(target, parent.transform);
                visual.name = "Visual";
                visual.transform.localPosition = -pivotPoint + offset;  // Use same offset as target
                visual.GetComponent<Renderer>().sharedMaterial = _settings.visualMaterial;
            }

            if (_settings.createGridVisualization)
            {
                CreateGridCubes(parent.transform, bounds);
            }

            PlacedObjectSO objectSO = null;
            if (_settings.generateScriptableObject)
            {
                objectSO = GenerateSO(parent, new Vector2Int(occupiedGridCellsX, occupiedGridCellsZ));
            }
            else
            {
                childHolder.SetSOValues(width, depth, target.name, $"{_settings.prefabSavePath}/Visuals", _settings.prefabSavePath, _settings.objectType);
            }

            if (_settings.generatePrefab)
            {
                Utilities.CreatePrefab(parent, _settings.prefabSavePath, parent.name, true);
                if (objectSO) objectSO.prefab = parent.transform;
            }

            if(objectSO) EditorUtility.SetDirty(objectSO);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // Add components
            components.ForEach(c => parent.AddComponent(c));

            return parent;
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

        private PlacedObjectSO GenerateSO(GameObject target, Vector2Int occupiedGridCells)
        {
            PlacedObjectSO building = Utilities.CreateNewScriptableObject<PlacedObjectSO>(target.name);

            building.width = occupiedGridCells.x;
            building.height = occupiedGridCells.y;
            building.nameString = target.name;
            building.PrefabPath = _settings.prefabSavePath;
            building.VisualPath = $"{_settings.prefabSavePath}/Visuals";
            building.placedObjectType = _settings.objectType;
            Utilities.CreatePrefab(target, _settings.prefabSavePath, target.name, true);

            return building;
        }

        private Vector3 SetPivotPoint(PivotPoint pivotPoint, int occupiedGridCellsX, int occupiedGridCellsZ, float width = 0, float depth = 0)
        {
            int gridCellSize = _settings.gridCellSize;
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

        //private void CreateFloorCubeGrids(GameObject target, Transform Parent, int occupiedGridCellsX, int occupiedGridCellsZ)
        //{
        //    List<GameObject> list = new();

        //    //Creates the Ground Grids for the Mesh
        //    for (int x = 0; x < occupiedGridCellsX; x++)
        //    {
        //        for (int z = 0; z < occupiedGridCellsZ; z++)
        //        {
        //            Vector3 cubePosition = new Vector3(x, 0, z) * _settings.gridCellSize;
        //            GameObject cube = Instantiate(_settings.cubePrefab);

        //            cube.transform.localScale = new Vector3(_settings.gridCellSize, 0.1f, _settings.gridCellSize);
        //            cube.transform.position = target.transform.position + cubePosition;
        //            cube.transform.forward = target.transform.forward;
        //            cube.transform.SetParent(Parent.transform);
        //            list.Add(cube);
        //        }
        //    }

        //    ChildHolder childHolder = Parent.GetComponent<ChildHolder>();
        //    childHolder.CreateCube(list);
        //    childHolder.TurnOffCubes();

        //}

        private void CreateGridCubes(Transform parent, Bounds bounds)
        {
            var gridParent = new GameObject("GridCubes").transform;
            gridParent.SetParent(parent);

            int xCells = Mathf.CeilToInt(bounds.size.x / _settings.gridCellSize);
            int zCells = Mathf.CeilToInt(bounds.size.z / _settings.gridCellSize);

            for (int x = 0; x < xCells; x++)
            {
                for (int z = 0; z < zCells; z++)
                {
                    var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    cube.transform.SetParent(gridParent);
                    cube.transform.localPosition = new Vector3(
                        x * _settings.gridCellSize + _settings.gridCellSize / 2,
                        _settings.yOffset,
                        z * _settings.gridCellSize + _settings.gridCellSize / 2
                    );
                    cube.transform.localScale = new Vector3(
                        _settings.gridCellSize - 0.1f,
                        0.1f,
                        _settings.gridCellSize - 0.1f
                    );
                }
            }
        }

        private void CreateEdgePositions(Transform parent, Bounds bounds)
        {
            var edges = new GameObject("EdgePositions").transform;
            List<FloorEdgePosition> edgePositionsList = new List<FloorEdgePosition>();
            edges.SetParent(parent);

            float currentRot = 0;
            float currentX = -bounds.size.x;
            float currentZ = bounds.size.z + 1.5f;

            for (int i = 0; i < 4; i++)
            {
                var edge = GameObject.CreatePrimitive(PrimitiveType.Cube);
                edge.name = $"Edge_{((EdgeType)i).ToString()}";
                edge.GetComponent<MeshRenderer>().enabled = false;
                edge.transform.SetParent(edges);
                edge.transform.rotation = Quaternion.Euler(new Vector3(0, currentRot, 0));
                edge.layer = _settings.edgeLayer;

                // Position calculation
                currentX += bounds.size.x / 2;
                currentZ -= bounds.size.z / 2;
                float x = (currentRot == 0 || currentRot == 180) ? currentX : 0;
                float z = (currentRot == 90 || currentRot == 270) ? currentZ : 0;
                edge.transform.localPosition = new Vector3(x, 0f, z);

                // Scale
                edge.transform.localScale = new Vector3(_settings.edgeSize.x, _settings.edgeSize.y, bounds.size.z);

                // Add component and set edge type
                var floorEdgePosition = edge.AddComponent<FloorEdgePosition>();
                floorEdgePosition.SetEdge((EdgeType)i);
                edgePositionsList.Add(floorEdgePosition);

                currentRot += 90;
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

            FloorPlacedObject floorPlacedObject = parent.GetComponent<FloorPlacedObject>();
            floorPlacedObject.SetEdgePositions(edgePositionsList[0], edgePositionsList[2], edgePositionsList[1], edgePositionsList[3]);
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