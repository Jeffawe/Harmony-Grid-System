using UnityEditor;
using UnityEngine;

namespace HarmonyGridSystem.Grid
{
    [CustomEditor(typeof(GridBuildInEditor))]
    public class GridBuilderEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            // Draw the default inspector to show other serialized fields
            DrawDefaultInspector();


            GUILayout.Space(10);

            // Get a reference to the target object (InputController component)
            GridBuildInEditor gridSystem = (GridBuildInEditor)target;

            if (GUILayout.Button("Create Grid"))
            {
                gridSystem.CreateGrid();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Change Current Object"))
            {
                gridSystem.ChangeObject();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Place Object"))
            {
                gridSystem.PlaceObject();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Change Current Grid"))
            {
                gridSystem.ChangeGridSelect();
            }

            GUILayout.Space(5);

            if (GUILayout.Button("Clear Grid"))
            {
                gridSystem.ClearObjects();
            }
        }
    }
}
