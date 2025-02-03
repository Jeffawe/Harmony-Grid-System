using HarmonyGridSystem.Grid;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace HarmonyGridSystem.Utils
{
    [CreateAssetMenu(menuName = "Grid System/Converter Settings")]
    public class GridObjectConverterSettings : ScriptableObject
    {
        [Header("Core Settings")]
        public PlacedObjectType objectType = PlacedObjectType.GridObject;
        public int gridCellSize = 1;
        public PivotPoint pivotPoint = PivotPoint.Center;
        public string prefabSavePath = "Assets/Prefabs/GridObjects";
        public bool generatePrefab;
        public bool generateScriptableObject;
        public bool isMultiple;
        public int gridHeight;

        [Header("Visual Settings")]
        public bool createVisualPrefab = true;
        public Material visualMaterial;
        public bool createGridVisualization = false;
        public float yOffset = 0.1f;
        public GameObject cubePrefab;

        [Header("Type Specific")]
        public int edgeLayer = 6;
        public int looseObjectLayer = 7;
        [Tooltip("The size of the edge place holders of the floor. Makes it easier to place the walls for the floor. Don't set the X layer to above 1 and the y layer to above 2")]
        public Vector3 edgeSize = new Vector3(0.5f, 2, 0);

        [HideInInspector] public GridValidationResults validationResults = new GridValidationResults();

        // Default material for visual prefabs
        private void OnEnable()
        {
            if (visualMaterial == null)
            {
                visualMaterial = new Material(Shader.Find("Standard"));
                visualMaterial.color = new Color(0, 1, 1, 0.3f); // Cyan with transparency
                visualMaterial.renderQueue = 3000; // Transparent render queue
            }
        }
    }

    [System.Serializable]
    public class GridValidationResults
    {
        public bool isValid = true;
        public string recommendedGridSize;
        public List<string> warnings = new List<string>();
        public List<string> errors = new List<string>();

        public void Clear()
        {
            isValid = true;
            recommendedGridSize = string.Empty;
            warnings.Clear();
            errors.Clear();
        }
    }


    public enum PivotPoint
    {
        Center,
        BottomLeft,
        BottomRight,
        TopLeft,
        TopRight
    }

    public class GridObjectAnalysis
    {
        private List<Vector3> _sizes = new List<Vector3>();

        public float minWidth = float.MaxValue;
        public float maxWidth = float.MinValue;
        public float minDepth = float.MaxValue;
        public float maxDepth = float.MinValue;

        public void AddObjectBounds(PlacedObjectType type, Vector3 size)
        {
            _sizes.Add(size);
            minWidth = Mathf.Min(minWidth, size.x);
            maxWidth = Mathf.Max(maxWidth, size.x);
            minDepth = Mathf.Min(minDepth, size.z);
            maxDepth = Mathf.Max(maxDepth, size.z);
        }

        public string GetRecommendedGridSize(PlacedObjectType type)
        {
            return type switch
            {
                PlacedObjectType.WallObject => $"Recommended Grid Size: {Mathf.Ceil(maxWidth)} units (based on wall width)",
                PlacedObjectType.FloorObject => $"Recommended Grid Size: {Mathf.Ceil(Mathf.Max(maxWidth, maxDepth))} units (based on floor dimensions)",
                _ => $"Recommended Grid Size: {Mathf.Ceil(Mathf.Max(maxWidth, maxDepth))} units"
            };
        }

        public bool HasWidthVariance() => Mathf.Abs(maxWidth - minWidth) > 0.01f;
        public bool HasDepthVariance() => Mathf.Abs(maxDepth - minDepth) > 0.01f;

        public bool HasInvalidDimensions(int gridSize)
        {
            return _sizes.Any(size =>
                !IsDivisible(size.x, gridSize) ||
                !IsDivisible(size.z, gridSize));
        }

        private bool IsDivisible(float value, int divisor)
        {
            return Mathf.Approximately(value % divisor, 0);
        }
    }
}