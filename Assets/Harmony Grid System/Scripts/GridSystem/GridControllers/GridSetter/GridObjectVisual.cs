using HarmonyGridSystem.Objects;
using UnityEngine;

namespace HarmonyGridSystem.Utils
{

    [ExecuteAlways]
    public class GridObjectVisual : MonoBehaviour
    {
        private PlacedObject placedObject;
        private Material visualMaterial;
        private Mesh quadMesh;
        private int gridCellSize;
        void OnEnable()
        {
            placedObject = GetComponent<PlacedObject>();
            InitializeResources();
        }

        public void Initalize(int gridCellSize)
        {
            this.gridCellSize = gridCellSize;
        }

        void InitializeResources()
        {
            // Create transparent material
            visualMaterial = new Material(Shader.Find("Standard"));
            visualMaterial.color = new Color(0, 1, 1, 0.3f);
            visualMaterial.renderQueue = 3000; // Transparent render queue
        }

        void OnDisable()
        {
            // Cleanup to prevent memory leaks
            if (Application.isEditor && !Application.isPlaying)
            {
                DestroyImmediate(visualMaterial);
                DestroyImmediate(quadMesh);
            }
        }
    }
}

