using UnityEngine;
using HarmonyGridSystem.Objects;

namespace HarmonyGridSystem.Grid
{
    public class GridManager : MonoBehaviour
    {
        public static GridManager Instance;

        [Header("Key Attributes")]
        [Tooltip("Grid Width")]
        [SerializeField] int gridWidth;

        [Tooltip("Grid Height")]
        [SerializeField] int gridHeight;

        [Tooltip("Cell Size of each grid box")]
        [SerializeField] int cellSize;

        [Tooltip("Animation Curve for the Placing Animation")]
        [SerializeField] AnimationCurve animationCurve;

        [Tooltip("The height between two vertical grids")]
        [SerializeField] int gridYSize;

        [Tooltip("Number of Vertical grids to place")]
        [SerializeField] int gridYCount;

        [Header("Other Settings")]
        [Tooltip("The offset on the Y axis for the floor to ensure the floor is above the ground. You can use the y dimension size of your floor mesh to determine this")]
        [SerializeField] float floorYoffset = 0.1f;

        [Space(20)]
        [Tooltip("SO to place in world")]
        public LookUpTable placedObjects;

        public AnimationCurve AnimationCurve => animationCurve;
        public int GridWidth => gridWidth;
        public int GridHeight => gridHeight;
        public int CellSize => cellSize;
        public int GridYSize => gridYSize;
        public int GridYCount => gridYCount;
        public float FloorYOffset => floorYoffset;
        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void SetCellSize(int newCellSize)
        {
            cellSize = newCellSize;
        }

        private int FirstLayerInMask(LayerMask mask)
        {
            return (int)Mathf.Log(mask.value, 2);
        }
    }

    public enum PlacedObjectType
    {
        FloorObject,
        GridObject,
        WallObject,
        LooseObject,
        ZoneObject
    }

    public enum PivotPoint
    {
        Center,
        BottomLeft,
        BottomRight,
        TopLeft,
        TopRight
    }
}