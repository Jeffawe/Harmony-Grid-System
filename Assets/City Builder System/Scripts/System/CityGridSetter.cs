using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using HarmonyGridSystem.Objects;

namespace HarmonyGridSystem.Builder
{
    [Serializable]
    public class Position
    {
        public float x;
        public float y;
    }

    [Serializable]
    public class JsonObject
    {
        public string name;
        public string text;
        public Position position;
        public float width;
        public float height;
        public int direction;
    }

    public class GridObj
    {
        public string name;
        public int width;
        public int height;
        public int direction;
        public int x;
        public int y;

        public GridObj(string name, int width, int height, int direction, int x, int y)
        {
            this.name = name;
            this.width = width;
            this.height = height;
            this.direction = direction;
            this.x = x;
            this.y = y;
        }
    }

    public class CityGridSetter : MonoBehaviour
    {
        private HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();
        private int gridWidth;
        private int gridHeight;

        public bool ArrangeGridObj(Vector2Int gridWH, int cellSize, TextAsset jsonFile, LookUpTable lookUpTable, out List<GridObj> results)
        {
            gridWidth = gridWH.x;
            gridHeight = gridWH.y;
            results = new List<GridObj>();
            try
            {
                if (jsonFile != null)
                {
                    results = ProcessLayoutJson(jsonFile.text, cellSize, lookUpTable);
                    return true;
                }
                else
                {
                    Debug.LogError("No JSON file assigned to GridSystem!");
                    return false;
                }
            }
            catch (Exception e)
            {
                Debug.LogError(e);
                return false;
            }

        }

        public List<GridObj> ProcessLayoutJson(string jsonData, float cellSize, LookUpTable lookUpTable)
        {
            JsonObject[] objects = JsonConvert.DeserializeObject<JsonObject[]>(jsonData);

            // Get paper/original dimensions
            JsonObject original = objects[0];
            float paperWidth = original.width;
            float paperHeight = original.height;

            // Calculate grid dimensions based on cell size
            //gridWidth = Mathf.CeilToInt(paperWidth / cellSize);
            //gridHeight = Mathf.CeilToInt(paperHeight / cellSize);

            // Convert remaining objects to GridObj and calculate initial grid positions
            List<GridObj> gridObjects = new List<GridObj>();
            for (int i = 1; i < objects.Length; i++)
            {
                JsonObject obj = objects[i];

                // Convert world position to grid position
                int gridX = Mathf.FloorToInt((obj.position.x / paperWidth) * gridWidth);
                int gridY = Mathf.FloorToInt((obj.position.y / paperHeight) * gridHeight);

                // Get PlacedObjectSO from lookup table
                PlacedObjectSO placedObj = lookUpTable.GetSO(obj.text.ToLower());

                // Default to 1,1 if no object found
                int width = placedObj != null ? placedObj.width : 1;
                int height = placedObj != null ? placedObj.height : 1;

                gridObjects.Add(new GridObj(
                    obj.name,
                    width,
                    height,
                    obj.direction,
                    gridX,
                    gridY
                ));
            }

            return OptimizeLayout(gridObjects);
        }

        private List<GridObj> OptimizeLayout(List<GridObj> objects)
        {
            // Sort objects by area (larger first) and then by original position
            var sortedObjects = objects.OrderByDescending(o => o.width * o.height)
                                     .ThenBy(o => o.y)
                                     .ThenBy(o => o.x)
                                     .ToList();

            occupiedPositions.Clear();
            List<GridObj> finalObjects = new List<GridObj>();

            foreach (var obj in sortedObjects)
            {
                Vector2Int bestPosition = FindBestPosition(obj);

                // Create new GridObj with optimized position
                var optimizedObj = new GridObj(
                    obj.name,
                    obj.width,
                    obj.height,
                    obj.direction,
                    bestPosition.x,
                    bestPosition.y
                );

                // Mark positions as occupied
                for (int x = bestPosition.x; x < bestPosition.x + obj.width; x++)
                {
                    for (int y = bestPosition.y; y < bestPosition.y + obj.height; y++)
                    {
                        occupiedPositions.Add(new Vector2Int(x, y));
                    }
                }

                finalObjects.Add(optimizedObj);
            }

            return finalObjects;
        }

        private Vector2Int FindBestPosition(GridObj obj)
        {
            var originalPos = new Vector2Int(obj.x, obj.y);
            var queue = new PriorityQueue<Vector2Int, float>();
            queue.Enqueue(originalPos, 0);

            var seen = new HashSet<Vector2Int>();

            while (queue.Count > 0)
            {
                Vector2Int currentPos = queue.Dequeue();

                if (seen.Contains(currentPos))
                    continue;

                seen.Add(currentPos);

                if (IsPositionValid(obj, currentPos))
                {
                    return currentPos;
                }

                // Try adjacent positions
                Vector2Int[] directions = new[]
                {
                new Vector2Int(1, 0),
                new Vector2Int(0, 1),
                new Vector2Int(-1, 0),
                new Vector2Int(0, -1)
            };

                foreach (var dir in directions)
                {
                    Vector2Int newPos = currentPos + dir;
                    if (!seen.Contains(newPos))
                    {
                        float priority = Vector2Int.Distance(originalPos, newPos);
                        queue.Enqueue(newPos, priority);
                    }
                }
            }

            return originalPos; // Fallback to original position if no valid position found
        }

        private bool IsPositionValid(GridObj obj, Vector2Int position)
        {
            // Check grid boundaries
            if (position.x < 0 || position.y < 0 ||
                position.x + obj.width > gridWidth ||
                position.y + obj.height > gridHeight)
            {
                return false;
            }

            // Check for overlaps
            for (int x = position.x; x < position.x + obj.width; x++)
            {
                for (int y = position.y; y < position.y + obj.height; y++)
                {
                    if (occupiedPositions.Contains(new Vector2Int(x, y)))
                    {
                        return false;
                    }
                }
            }

            return true;
        }
    }

    // Simple priority queue implementation
    public class PriorityQueue<T, TPriority> where TPriority : IComparable<TPriority>
    {
        private List<(T item, TPriority priority)> elements = new List<(T, TPriority)>();

        public int Count => elements.Count;

        public void Enqueue(T item, TPriority priority)
        {
            elements.Add((item, priority));
            elements.Sort((a, b) => a.priority.CompareTo(b.priority));
        }

        public T Dequeue()
        {
            if (elements.Count == 0)
                throw new InvalidOperationException("Queue is empty");

            T item = elements[0].item;
            elements.RemoveAt(0);
            return item;
        }
    }
}