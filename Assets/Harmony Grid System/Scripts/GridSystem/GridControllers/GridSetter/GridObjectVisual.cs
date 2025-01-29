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
            // Create or get quad mesh
            quadMesh = CreateQuadMesh();

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

        void Update()
        {
            if (!Application.isPlaying)
            {
                DrawGridPreview();
            }
        }

        void DrawGridPreview()
        {
            float gridCellSize = 1f; // Get from your settings

            for (int x = 0; x < placedObject.Width; x++)
            {
                for (int z = 0; z < placedObject.Height; z++)
                {
                    Vector3 position = transform.position +
                        new Vector3(x * gridCellSize, 0.01f, z * gridCellSize);

                    Matrix4x4 matrix = Matrix4x4.TRS(
                        position,
                        Quaternion.Euler(90, 0, 0),
                        new Vector3(gridCellSize, gridCellSize, 1)
                    );

                    Graphics.DrawMesh(
                        quadMesh,
                        matrix,
                        visualMaterial,
                        0, // Layer
                        null, // Camera
                        0, // Submesh index
                        null, // Material properties
                        false, // Cast shadows
                        false // Receive shadows
                    );
                }
            }
        }

        private Mesh CreateQuadMesh()
        {
            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(-0.5f, 0, -0.5f),
                new Vector3(0.5f, 0, -0.5f),
                new Vector3(-0.5f, 0, 0.5f),
                new Vector3(0.5f, 0, 0.5f)
            };

            int[] tris = new int[6] { 0, 2, 1, 2, 3, 1 };

            mesh.vertices = vertices;
            mesh.triangles = tris;
            mesh.RecalculateNormals();

            return mesh;
        }
    }
}

