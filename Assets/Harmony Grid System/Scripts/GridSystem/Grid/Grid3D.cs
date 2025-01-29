using System;
using UnityEngine;
using HarmonyGridSystem.Utils;

namespace HarmonyGridSystem.Grid
{
    public class Grid3D<TGridObject>
    {
        public event EventHandler<OnGridValueChangedEventArgs> OnGridValueChanged;

        public class OnGridValueChangedEventArgs : EventArgs
        {
            public int x;
            public int z;
        }

        private int width;
        private int breadth;
        private float cellSize;
        private Vector3 origin;
        private TextMesh[,] debugTextMesh;
        private bool showDebug;

        private TGridObject[,] gridArray;

        public Grid3D(int width, int height, float cellSize, Vector3 origin, DebugOptions debugOptions, Func<Grid3D<TGridObject>, int, int, TGridObject> createGridObject)
        {
            this.width = width;
            this.breadth = height;
            this.cellSize = cellSize;
            this.origin = origin;
            this.showDebug = debugOptions.showDebug;

            gridArray = new TGridObject[width, height];

            for (int x = 0; x < gridArray.GetLength(0); x++)
            {
                for (int z = 0; z < gridArray.GetLength(1); z++)
                {
                    gridArray[x, z] = createGridObject(this, x, z);
                }
            }

            if (debugOptions.showDebug)
            {
                debugTextMesh = new TextMesh[width, height];

                for (int x = 0; x < gridArray.GetLength(0); x++)
                {
                    for (int z = 0; z < gridArray.GetLength(1); z++)
                    {
                        if (debugOptions.showText)
                        {
                            debugTextMesh[x, z] = UtilsClass.CreateWorldText(gridArray[x, z]?.ToString(), null, GetWorldPosition(x, z) + new Vector3(cellSize, cellSize) * .5f, debugOptions.textSize, debugOptions.textColor, TextAnchor.MiddleCenter);
                            debugTextMesh[x, z].transform.SetParent(debugOptions.parent, true);
                        }

                        if (!debugOptions.useLineRenderer)
                        {
                            Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x, z + 1), debugOptions.lineColor, 100f);
                            Debug.DrawLine(GetWorldPosition(x, z), GetWorldPosition(x + 1, z), debugOptions.lineColor, 100f);
                        }
                    }
                }

                if (!debugOptions.useLineRenderer)
                {
                    Debug.DrawLine(GetWorldPosition(0, height), GetWorldPosition(width, height), debugOptions.lineColor, 100f);
                    Debug.DrawLine(GetWorldPosition(width, 0), GetWorldPosition(width, height), debugOptions.lineColor, 100f);
                }
                else
                {
                    SetupGridLineRenderer(debugOptions.lineColor, debugOptions.mat);
                }

                OnGridValueChanged += (object sender, OnGridValueChangedEventArgs eventArgs) =>
                {
                    debugTextMesh[eventArgs.x, eventArgs.z].text = gridArray[eventArgs.x, eventArgs.z]?.ToString();
                };
            }
        }


        private void SetupGridLineRenderer(Color lineColor, Material mat)
        {
            GameObject lineRendererObject = new GameObject("GridLineRenderer");
            LineRenderer lineRenderer = lineRendererObject.AddComponent<LineRenderer>();

            lineRenderer.material = mat; // Or any desired shader
            lineRenderer.startColor = lineColor;
            lineRenderer.endColor = lineColor;
            lineRenderer.startWidth = 0.05f;  // Adjust as necessary
            lineRenderer.endWidth = 0.05f;
            lineRenderer.positionCount = (width + 1) * 2 + (breadth + 1) * 2; // Total points for grid lines

            int index = 0;
            Vector3[] positions = new Vector3[lineRenderer.positionCount];

            // Vertical lines
            for (int x = 0; x <= width; x++)
            {
                positions[index++] = GetWorldPosition(x, 0);
                positions[index++] = GetWorldPosition(x, breadth);
            }

            // Horizontal lines
            for (int z = 0; z <= breadth; z++)
            {
                positions[index++] = GetWorldPosition(0, z);
                positions[index++] = GetWorldPosition(width, z);
            }

            lineRenderer.SetPositions(positions);
        }

        public int GetWidth()
        {
            return width;
        }

        public float GetCellSize()
        {
            return cellSize;
        }

        public int GetBreath()
        {
            return breadth;
        }

        /// <summary>
        /// Gets the world position for an object in the grid
        /// </summary>
        /// <param name="x">x parameter</param>
        /// <param name="z">z parameter</param>
        /// <returns></returns>
        public Vector3 GetWorldPosition(int x, int z)
        {
            return new Vector3(x, 0, z) * cellSize + origin;
        }

        public void GetXZ(Vector3 worldPosition, out int x, out int z)
        {
            Vector3 newWorldPos = worldPosition - origin;
            x = Mathf.FloorToInt(newWorldPos.x / cellSize);
            z = Mathf.FloorToInt(newWorldPos.z / cellSize);
        }

        /// <summary>
        /// Updates the value of a row-column specified using index
        /// </summary>
        /// <param name="x">width/column</param>
        /// <param name="z">height/row</param>
        /// <param name="value">value to set</param>
        public void SetGridObject(int x, int z, TGridObject value)
        {
            if (x >= 0 && x < width && z >= 0 && z < breadth)
            {
                gridArray[x, z] = value;
            }

            if (OnGridValueChanged != null) OnGridValueChanged(this, new OnGridValueChangedEventArgs { x = x, z = z });
        }

        /// <summary>
        /// Updates the value of a row-column specified using world Position
        /// </summary>
        /// <param name="value">value to set</param>
        public void SetGridObject(Vector3 worldPosition, TGridObject value)
        {
            int x, z;
            GetXZ(worldPosition, out x, out z);
            SetGridObject(x, z, value);
        }

        /// <summary>
        /// Returns the Object at that point in the Grid
        /// </summary>
        /// <param name="x">The x value</param>
        /// <param name="z">The Z value</param>
        /// <returns></returns>
        public TGridObject GetGridObject(int x, int z)
        {
            if (x >= 0 && x < width && z >= 0 && z < breadth)
            {
                return gridArray[x, z];
            }
            else
            {
                return default(TGridObject);
            }
        }

        public TGridObject GetGridObject(Vector3 worldPosition)
        {
            int x, z;
            GetXZ(worldPosition, out x, out z);
            return GetGridObject(x, z);
        }

        public void TriggerOnGridObjectChanged(int x, int z)
        {
            if (OnGridValueChanged != null) OnGridValueChanged(this, new OnGridValueChangedEventArgs { x = x, z = z });
        }

        /// <summary>
        /// Ensures the Grid Positions actually exists in the grid and alters it if not
        /// </summary>
        /// <param name="gridPosition">The grid position to check on</param>
        /// <returns>The validated Grid positions</returns>
        public Vector2Int ValidateGridPosition(Vector2Int gridPosition)
        {
            return new Vector2Int(
                Mathf.Clamp(gridPosition.x, 0, width - 1),
                Mathf.Clamp(gridPosition.y, 0, breadth - 1)
            );
        }

        public void HideGrid(bool hide)
        {
            showDebug = !hide;

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < breadth; z++)
                {
                    if (debugTextMesh[x, z] != null)
                    {
                        debugTextMesh[x, z].gameObject.SetActive(showDebug);
                    }
                }
            }
        }

        public void ClearGrid()
        {
            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < breadth; z++)
                {
                    gridArray[x, z] = default(TGridObject);

                    if (debugTextMesh[x, z] != null)
                    {
                        GameObject.DestroyImmediate(debugTextMesh[x, z].gameObject);
                        debugTextMesh[x, z] = null;
                    }
                }
            }
        }
    }

    [System.Serializable]
    public struct DebugOptions
    {
        public bool showDebug;
        public bool showText;
        public Color lineColor;
        public Color textColor;
        public int textSize;
        public Transform parent;
        public bool useLineRenderer;
        public Material mat;

        public DebugOptions(bool showDebug, bool showText, Color lineColor, Color textColor, int textSize, Transform parent, bool useLineRenderer, Material mat)
        {
            this.showDebug = showDebug;
            this.showText = showText;   
            this.lineColor = lineColor;
            this.textColor = textColor;
            this.textSize = textSize;
            this.parent = parent;
            this.useLineRenderer = useLineRenderer;
            this.mat = mat;
        }
    }
}
