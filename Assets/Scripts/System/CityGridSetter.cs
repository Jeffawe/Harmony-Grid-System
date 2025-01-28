using System.Collections.Generic;
using UnityEngine;
using HarmonyGridSystem.Objects;

namespace HarmonyGridSystem.Builder
{
    public class CityGridSetter : MonoBehaviour
    {
        public int GridSize = 10;

        public int GridWidth;
        public int GridHeight;

        // Pre-allocated grid for efficient placement
        private PlacedObjectSO[,] occupiedGrids;

        // Start is called before the first frame update
        void Start()
        {
            // Initialize occupiedGrids array
            occupiedGrids = new PlacedObjectSO[GridWidth, GridHeight];
        }

        public void ConstructGrid(BuildingBlock[] buildingBlocks)
        {
            foreach (var buildingBlock in buildingBlocks)
            {
                List<Vector2Int> elementGrids = GetElementGrids(buildingBlock.GetPosition(), buildingBlock.GetSO());

                for (int i = 0; i < elementGrids.Count; i++)
                {
                    Vector2Int grid = elementGrids[i];
                    if (occupiedGrids[grid.x, grid.y] != null)
                    {
                        while (occupiedGrids[grid.x, grid.y] != null)
                        {
                            grid.x++;
                        }
                    }

                    occupiedGrids[elementGrids[i].x, elementGrids[i].y] = buildingBlock.GetSO();
                }
            }
        }

        private List<Vector2Int> GetElementGrids(Vector2 paperPosition, PlacedObjectSO objectSO)
        {
            List<Vector2Int> grids = new List<Vector2Int>();
            Vector2Int gridStartingPosition = GetGridPosition(paperPosition);
            Vector2Int ElementWH = GetMeshWidthAndHeight(objectSO);

            for (int x = 0; x < ElementWH.x; x++)
            {
                for (int y = 0; y < ElementWH.y; y++)
                {
                    // Calculate offset within the element's grid position
                    Vector2Int offset = new Vector2Int(x, y);

                    // Calculate absolute grid position
                    Vector2Int absoluteGrid = gridStartingPosition + offset;

                    ExpandGrid(absoluteGrid);

                    grids.Add(absoluteGrid);
                }
            }

            return grids;
        }

        private Vector2Int GetGridPosition(Vector2 paperPosition)
        {
            // Normalize position within the smaller grid
            float normalizedX = paperPosition.x / GridSize;
            float normalizedY = paperPosition.y / GridSize;

            // Calculate grid indices (0-based)
            int gridX = Mathf.FloorToInt(normalizedX);
            int gridY = Mathf.FloorToInt(normalizedY);

            return new Vector2Int(gridX, gridY);
        }

        private Vector2Int GetMeshWidthAndHeight(PlacedObjectSO objectSO)
        {
            return new Vector2Int(objectSO.width, objectSO.height);
        }

        private void ExpandGrid(Vector2Int expandGrid)
        {
            if (expandGrid.x > GridWidth)
            {
                occupiedGrids = new PlacedObjectSO[expandGrid.x, GridHeight];
            }

            if (expandGrid.y > GridWidth)
            {
                occupiedGrids = new PlacedObjectSO[GridWidth, expandGrid.y];
            }

        }
    }

    public class BuildingBlock
    {
        private Vector2Int position;
        private PlacedObjectSO objectSO;

        public BuildingBlock(Vector2Int _position, PlacedObjectSO _SO)
        {
            this.position = _position;
            this.objectSO = _SO;
        }

        public Vector2Int GetPosition()
        {
            return position;
        }

        public PlacedObjectSO GetSO()
        {
            return objectSO;
        }
    }
}
