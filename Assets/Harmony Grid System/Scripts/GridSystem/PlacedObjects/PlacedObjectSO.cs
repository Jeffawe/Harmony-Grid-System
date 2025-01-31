using HarmonyGridSystem.Utils;
using HarmonyGridSystem.Grid;
using System.Collections.Generic;
using UnityEngine;

namespace HarmonyGridSystem.Objects
{
    [CreateAssetMenu(fileName = "Placed Object", menuName = "Harmony Grid System/Placed Object")]
    public class PlacedObjectSO : ScriptableObject
    {
        public string nameString;
        public string constraintGroupName;
        public string PrefabPath;
        public string VisualPath;
        public Transform prefab;
        public Transform visual;
        public PlacedObjectType placedObjectType;

        public HashSet<string> allowedAdjacentObjects = new();
        public List<ConstraintRuleSO> constraintRules;

        private int _constraintGroupId = -1;
        public int ConstraintGroupId => _constraintGroupId == -1 ? (_constraintGroupId = ConstraintManager.GetOrCreateGroupId(constraintGroupName)) : _constraintGroupId;

        public bool HasConstraints => constraintRules is { Count: > 0 };

        public bool hasVisual { get; set; }

        public Transform Prefab
        {
            get
            {
                if (prefab == null)
                {
                    GameObject newPrefab = Utilities.GetPrefab(PrefabPath, this.name);
                    if (newPrefab == null) return prefab;
                    else
                        return newPrefab.transform;
                }
                else return prefab;
            }
        }

        public Transform Visual
        {
            get
            {
                if (!hasVisual) return Prefab;
                if (visual == null)
                {
                    GameObject newVisual = Utilities.GetPrefab(VisualPath, $"{this.name}_visual");
                    if (newVisual == null) return Prefab;
                    else return newVisual.transform;
                }
                else return Prefab;
            }
        }

        public int width;
        public int height;


        public List<Vector2Int> GetGridPosition(Vector2Int offset, Dir dir)
        {
            List<Vector2Int> gridPositions = new();

            for (int x = 0; x < width; x++)
            {
                for (int z = 0; z < height; z++)
                {
                    gridPositions.Add(offset + new Vector2Int(x, z));
                }
            }

            switch (dir)
            {
                default:
                case Dir.Down:
                case Dir.Up:
                    for (int x = 0; x < width; x++)
                    {
                        for (int z = 0; z < height; z++)
                        {
                            gridPositions.Add(offset + new Vector2Int(x, z));
                        }
                    }
                    break;

                case Dir.Left:
                case Dir.Right:
                    for (int x = 0; x < height; x++)
                    {
                        for (int z = 0; z < width; z++)
                        {
                            gridPositions.Add(offset + new Vector2Int(x, z));
                        }
                    }
                    break;
            }

            return gridPositions;
        }

        public Dir GetNextDir(Dir dir)
        {
            switch (dir)
            {
                case Dir.Down: return Dir.Left;
                case Dir.Left: return Dir.Up;
                case Dir.Up: return Dir.Right;
                case Dir.Right: return Dir.Down;
                default: return Dir.Down;
            }
        }

        public int GetRotationAngle(Dir dir)
        {
            switch (dir)
            {
                default:
                case Dir.Down: return 0;
                case Dir.Left: return 90;
                case Dir.Up: return 180;
                case Dir.Right: return 270;
            }
        }

        public Vector2Int GetRotationOffset(Dir dir)
        {
            switch (dir)
            {
                default:
                case Dir.Down: return new Vector2Int(0, 0);
                case Dir.Left: return new Vector2Int(0, width);
                case Dir.Up: return new Vector2Int(width, height);
                case Dir.Right: return new Vector2Int(height, 0);
            }
        }

        public enum Dir
        {
            Down,
            Left,
            Up,
            Right
        }
    }


}
