using UnityEditor;
using UnityEngine;

namespace HarmonyGridSystem.Utils
{
    public static class Utilities
    {
        /// <summary>
        /// Gets the World Position that the mouse is at
        /// </summary>
        /// <param name="mouseMask">The Layer mask for the object the Mouse clicks on</param>
        /// <param name="mousePos">The mouse position</param>
        /// <returns></returns>
        public static Vector3 GetMouseWorldPosition(LayerMask mouseMask, Vector3 mousePos) => GetMouseWorldPosition_Instance(mouseMask, mousePos);

        private static Vector3 GetMouseWorldPosition_Instance(LayerMask mouseMask, Vector3 mousePos)
        {
            Ray ray = Camera.main.ScreenPointToRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit raycastHit, 999f, mouseMask))
            {
                return raycastHit.point;
            }
            else
            {
                return Vector3.zero;
            }
        }

        public static float SquareNumberBy(float number, float numberToSquareBy)
        {
            if (numberToSquareBy == 0) return 0;

            float num = number;

            for (int i = 0; i < numberToSquareBy; i++)
            {
                num *= number;
            }

            return num;
        }

        public static void SaveMesh(GameObject selectedGameObject, string nameOfMesh)
        {
            var mf = selectedGameObject.GetComponent<MeshFilter>();
            if (mf)
            {
                CreateFolder("Mesh");
                string savePath = $"Assets/Mesh/{nameOfMesh}.asset";
                Mesh myMesh = AssetDatabase.LoadAssetAtPath<Mesh>(savePath);

                if (!myMesh) AssetDatabase.CreateAsset(mf.mesh, savePath);
                else selectedGameObject.GetComponent<MeshFilter>().mesh = myMesh;

                AssetDatabase.SaveAssets();
            }
        }

        public static Mesh GetMesh(string nameOfMesh, string folderStructure)
        {
            string savePath = $"Assets/{folderStructure}/{nameOfMesh}.asset";
            Mesh myMesh = AssetDatabase.LoadAssetAtPath<Mesh>(savePath);

            AssetDatabase.SaveAssets();
            return myMesh;
        }

        public static GameObject CreatePrefab(GameObject prefabRoot, string nameOfFolder, string nameOfPrefab, bool deleteGameObject)
        {
            CreateFolder(nameOfFolder);
            string assetPath = $"Assets/{nameOfFolder}/{nameOfPrefab}.prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            if (prefab != null)
            {
                Debug.LogWarning("Prefab already exists at: " + assetPath);
            }

            // Create the prefab from the root GameObject
            prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(prefabRoot, assetPath, InteractionMode.UserAction, out bool prefabSuccess);
            AssetDatabase.SaveAssets();

            // Destroy the temporary root GameObject
            if (deleteGameObject) Object.Destroy(prefabRoot);

            if (prefabSuccess) Debug.Log("Prefab created at Assets/MyPrefab.prefab");
            return prefab;
        }

        public static Material CreateMaterial(Material originalMaterial, Color colorOfMaterial, string folderName, string materialName)
        {
            // Check if the original material is provided
            if (originalMaterial == null)
            {
                Debug.LogWarning("Original material is not provided.");
                return null;
            }

            // Duplicate the original material
            Material newMaterial = new Material(originalMaterial);

            // Change the main color of the new material
            newMaterial.color = colorOfMaterial;

            // Store the duplicated material in another place (e.g., as a new material in the project)
            CreateFolder(folderName);
            string newMaterialPath = $"Assets/{folderName}/{materialName}.mat";
            Material aMaterial = AssetDatabase.LoadAssetAtPath<Material>(newMaterialPath);
            if (aMaterial) return aMaterial;
            AssetDatabase.CreateAsset(newMaterial, newMaterialPath);
            AssetDatabase.SaveAssets();

            return newMaterial;
            
        }

        public static GameObject GetPrefab(string nameOfFolder, string nameOfPrefab)
        {
            // Construct the asset path
            string assetPath = $"Assets/{nameOfFolder}/{nameOfPrefab}.prefab";

            // Check if the folder exists
            if (!AssetDatabase.IsValidFolder($"Assets/{nameOfFolder}"))
            {
                Debug.LogError($"Folder '{nameOfFolder}' not found.");
                return null;
            }

            // Load the prefab
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);

            // Check if the prefab exists
            if (prefab == null)
            {
                Debug.LogError($"Prefab '{nameOfPrefab}' not found in folder '{nameOfFolder}'.");
                return null;
            }

            return prefab;
        }

        private static void CreateFolder(string nameOfFolder)
        {
            string folderPath = "Assets";
            nameOfFolder = nameOfFolder.Trim();
            string[] newString = nameOfFolder.Split("/");

            foreach (string item in newString)
            {
                string newFolderPath = folderPath + "/" + item.Trim();
                if (!AssetDatabase.IsValidFolder(newFolderPath))
                {
                    AssetDatabase.CreateFolder(folderPath, item.Trim());
                    folderPath = newFolderPath;
                }
                else
                {
                    folderPath = newFolderPath;
                    continue;
                }

            }
        }

        /// <summary>
        /// Creates a new ScriptableObject
        /// </summary>
        /// <typeparam name="T">The type of the ScriptableObject</typeparam>
        /// <param name="nameOfObject">The name of the Instance of the Scriptable Object</param>
        /// <returns>The Scriptable Object</returns>
        [ContextMenu("Create New ScriptableObject")]
        public static T CreateNewScriptableObject<T>(string nameOfObject) where T : ScriptableObject
        {
            //AssetDatabase.
            CreateFolder("Data");
            string assetPath = $"Assets/Data/{nameOfObject}.asset";

            T myScriptableObject = AssetDatabase.LoadAssetAtPath<T>(assetPath);
            // Check if the asset already exists at the specified path
            if (myScriptableObject == null)
            {
                // Create a new instance of your ScriptableObject class
                myScriptableObject = ScriptableObject.CreateInstance<T>();

                // You can set the properties of the ScriptableObject here
                myScriptableObject.name = $"{nameOfObject}";

                // Save the ScriptableObject as an asset in the project
                AssetDatabase.CreateAsset(myScriptableObject, assetPath);
                

                // Optional: Refresh the Unity Editor to see the changes immediately
                EditorUtility.SetDirty(myScriptableObject);

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
            else
            {
                Debug.LogWarning("ScriptableObject already exists at path: " + assetPath);
            }

            return myScriptableObject;
        }
    }
}
