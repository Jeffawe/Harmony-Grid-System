using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using HarmonyGridSystem.Grid;
using HarmonyGridSystem.Objects;

namespace HarmonyGridSystem.Utils
{
    public class GridObjectConverterWindow : EditorWindow
    {
        private const string SETTINGS_PATH = "Assets/Editor/GridObjectConverterSettings.asset";

        [MenuItem("Tools/Harmony Grid System/Convert Objects")]
        public static void ShowWindow()
        {
            var window = GetWindow<GridObjectConverterWindow>();
            window.titleContent = new GUIContent("Grid Converter");
            window.minSize = new Vector2(400, 500);
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

            // Grid visualization
            if (_settings.createGridVisualization)
            {
                CreateGridCubes(parent.transform, bounds);
            }

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

            // Scaling
            target.transform.localScale = new Vector3(
                _settings.gridCellSize / Mathf.RoundToInt(bounds.size.x),
                1f,
                _settings.gridCellSize / Mathf.RoundToInt(bounds.size.z)
            );

            // Floor specific components
            var floor = parent.GetComponent<FloorPlacedObject>();
            //floor.SetEdgeSize(_settings.edgeSize);
        }

        private void CreateWallObject(GameObject target, string name)
        {
            var parent = CreateBaseObject(target, name, new List<System.Type> { typeof(FloorEdgeObject) });
            var bounds = GetRenderBounds(target);

            // Layer setup
            SetLayerRecursively(parent, _settings.edgeLayer);

            // Wall scaling
            target.transform.localScale = new Vector3(
                _settings.gridCellSize / Mathf.RoundToInt(bounds.size.x),
                1f,
                1f
            );

            // Wall specific components
            var wall = parent.GetComponent<FloorEdgeObject>();
            //wall.SetWallThickness(_settings.wallThickness);
        }

        private void CreateLooseObject(GameObject target, string name)
        {
            var parent = CreateBaseObject(target, name, new List<System.Type> { typeof(PlacedObject) });
            SetLayerRecursively(parent, _settings.looseObjectLayer);
        }

        private GameObject CreateBaseObject(GameObject target, string name, List<System.Type> components)
        {
            var parent = new GameObject($"{name}_{_settings.objectType}");
            parent.transform.position = CalculatePivotPosition(target);

            // Essential part
            var essential = Instantiate(target, parent.transform);
            essential.name = "Essential";
            essential.transform.localPosition = Vector3.zero;

            // Visual part
            if (_settings.createVisualPrefab)
            {
                var visual = Instantiate(target, parent.transform);
                visual.name = "Visual";
                visual.transform.localPosition = Vector3.zero;
                visual.GetComponent<Renderer>().sharedMaterial = _settings.visualMaterial;
            }

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

        private Vector3 CalculatePivotPosition(GameObject target)
        {
            var bounds = GetRenderBounds(target);
            return _settings.pivotPoint switch
            {
                PivotPoint.BottomLeft => new Vector3(bounds.min.x, 0, bounds.min.z),
                PivotPoint.BottomRight => new Vector3(bounds.max.x, 0, bounds.min.z),
                PivotPoint.TopLeft => new Vector3(bounds.min.x, 0, bounds.max.z),
                PivotPoint.TopRight => new Vector3(bounds.max.x, 0, bounds.max.z),
                _ => bounds.center
            };
        }

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
            edges.SetParent(parent);

            float[] rotations = { 0, 90, 180, 270 };
            for (int i = 0; i < 4; i++)
            {
                var edge = new GameObject($"Edge_{((EdgeType)i).ToString()}").transform;
                edge.SetParent(edges);
                edge.localRotation = Quaternion.Euler(0, rotations[i], 0);
                edge.localPosition = Vector3.zero;
                edge.gameObject.AddComponent<FloorEdgePosition>().SetEdge((EdgeType)i);
            }
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
            EditorGUILayout.LabelField("Core Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("objectType"));
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("gridCellSize"));
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("pivotPoint"));
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("prefabSavePath"));
            EditorGUILayout.Space();
        }

        private void DrawTypeSpecificSettings()
        {
            EditorGUILayout.LabelField("Type Settings", EditorStyles.boldLabel);

            switch (_settings.objectType)
            {
                case PlacedObjectType.WallObject:
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty("edgeLayer"));
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty("wallThickness"));
                    break;

                case PlacedObjectType.FloorObject:
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty("edgeSize"));
                    break;

                case PlacedObjectType.LooseObject:
                    EditorGUILayout.PropertyField(_serializedObject.FindProperty("looseObjectLayer"));
                    break;
            }

            EditorGUILayout.Space();
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("createVisualPrefab"));
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("visualMaterial"));
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("createGridVisualization"));
            EditorGUILayout.PropertyField(_serializedObject.FindProperty("yOffset"));
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