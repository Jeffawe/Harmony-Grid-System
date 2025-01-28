using UnityEngine;
using HarmonyGridSystem.Utils;

public class Testing : MonoBehaviour
{
    [SerializeField] GameObject gameObject1;
    [SerializeField] LayerMask mouseMask;
    [SerializeField] GameObject point;
    private void Update()
    {
        Vector3 position = Utilities.GetMouseWorldPosition(mouseMask, Input.mousePosition);

        point.transform.position = new Vector3(position.x, point.transform.position.y, position.z);
    }

    private void ShowDimensions()
    {
        GameObject newObject = Instantiate(gameObject1);
        Mesh mesh = newObject.GetComponent<MeshFilter>().sharedMesh;
        float width = mesh.bounds.size.x;
        float depth = mesh.bounds.size.z;
        float height = mesh.bounds.size.y;

        Debug.Log(width + " " + height + " " + depth);
    }

}
